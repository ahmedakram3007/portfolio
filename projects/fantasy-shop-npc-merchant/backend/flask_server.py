from flask import Flask, request, jsonify
from flask_cors import CORS
import cohere
import requests
import os
from collections import deque
from pathlib import Path
import json
import re

app = Flask(__name__)
CORS(app)  # Unity talks to this API


# =========================
# CONFIGURATION
# =========================

# In a real deployment, this should be read from an environment variable.
COHERE_API_KEY = os.getenv("COHERE_API_KEY", "")  # redacted for public repo -- set via environment variable
COHERE_MODEL = "command-r-plus-08-2024"

OLLAMA_ENDPOINT = "http://localhost:11434/api/generate"
OLLAMA_MODEL = os.getenv("OLLAMA_MODEL", "llama3.2:latest")

USE_COHERE = bool(COHERE_API_KEY)
USE_OLLAMA = True

MERCHANT_PROMPT = (
    "Your name is Aldric. Speak in first person as Aldric the merchant in a fantasy RPG.\n"
    "Personality: Helpful, concise, and in-character. Keep responses short (at most 3 sentences).\n"
    "Style: Refer to your wares, prices and shop in first person (e.g. 'I have', 'My wares', 'I can offer').\n"
    "Constraints: Do NOT introduce yourself unless this is the first message of the conversation.\n"
    "Begin directly with the content and, if useful, ask at most one short follow-up question."
)

BASE_DIR = os.path.dirname(__file__)
MEMORY_DIR = os.path.join(BASE_DIR, "memory")

SHORT_TERM_MAX = 8
LONG_TERM_MAX = 200
PROMPT_HISTORY_MAX = 40

conversation_histories = {}  # session_id -> deque[{"role": ..., "text": ...}]
cohere_client = None

CMD_PATTERN = re.compile(r'(?i)(?:^|[^a-z0-9])/(cohere|llama|npc)(?:$|[^a-z0-9])')


# =========================
# BASIC HELPERS
# =========================

def _sanitize_session_id(sid):
    if not sid:
        return "default"
    safe = "".join(c for c in sid if c.isalnum() or c in ("-", "_"))
    return safe or "default"


def _ensure_memory_dir():
    Path(MEMORY_DIR).mkdir(parents=True, exist_ok=True)


# =========================
# MEMORY FUNCTIONS
# =========================

def append_short_history(session_id, role, text):
    """Short-term memory in RAM (recent turns)."""
    sid = _sanitize_session_id(session_id)
    dq = conversation_histories.get(sid)
    if dq is None:
        dq = deque(maxlen=SHORT_TERM_MAX)
        conversation_histories[sid] = dq
    dq.append({"role": role, "text": text})


def get_short_history(session_id, limit=None):
    sid = _sanitize_session_id(session_id)
    dq = conversation_histories.get(sid, deque())
    items = list(dq)
    return items[-limit:] if limit is not None else items


def append_long_history(session_id, role, text):
    """Long-term memory on disk (JSONL), survives restarts."""
    sid = _sanitize_session_id(session_id)
    _ensure_memory_dir()
    path = os.path.join(MEMORY_DIR, f"history_{sid}.jsonl")
    try:
        with open(path, "a", encoding="utf-8") as f:
            f.write(json.dumps({"role": role, "text": text}) + "\n")
    except Exception as e:
        print(f"[MEMORY][ERROR] append_long_history: {e}")


def load_long_history(session_id, limit=None):
    """Load older turns from disk; used so Aldric can remember old events."""
    sid = _sanitize_session_id(session_id)
    path = os.path.join(MEMORY_DIR, f"history_{sid}.jsonl")
    if not os.path.exists(path):
        return []
    items = []
    try:
        with open(path, "r", encoding="utf-8") as f:
            for line in f:
                line = line.strip()
                if not line:
                    continue
                try:
                    items.append(json.loads(line))
                except Exception:
                    items.append({"role": "system", "text": line})
    except Exception as e:
        print(f"[MEMORY][ERROR] load_long_history: {e}")
        return []
    return items[-limit:] if limit is not None else items


def clear_history(session_id):
    """Clear both short- and long-term memory for a session (used by /history/clear)."""
    sid = _sanitize_session_id(session_id)
    conversation_histories.pop(sid, None)
    path = os.path.join(MEMORY_DIR, f"history_{sid}.jsonl")
    if os.path.exists(path):
        try:
            open(path, "w", encoding="utf-8").close()
        except Exception as e:
            print(f"[MEMORY][ERROR] clear_history: {e}")


# =========================
# LLM HELPERS
# =========================

def _history_to_lines(history):
    """Convert memory into 'User:' / 'Assistant:' lines for the prompt."""
    lines = []
    for m in history:
        text = m.get("text", "")
        if not text:
            continue
        role = m.get("role", "user")
        prefix = "User" if role == "user" else "Assistant"
        lines.append(f"{prefix}: {text}")
    return lines


# =========================
# COHERE LLM
# =========================

def get_cohere_client():
    """Initialise Cohere client once."""
    global cohere_client
    if cohere_client is not None:
        return cohere_client
    if not USE_COHERE:
        print("[COHERE] Disabled (no API key set).")
        return None
    try:
        cohere_client = cohere.Client(COHERE_API_KEY)
        print("[COHERE] Client initialised.")
        return cohere_client
    except Exception as e:
        print(f"[COHERE][ERROR] client init: {type(e).__name__}: {e}")
        cohere_client = None
        return None


def query_cohere(user_message, system_prompt, history):
    """Ask Cohere for a reply using a flattened text prompt."""
    client = get_cohere_client()
    if client is None:
        return None

    lines = [system_prompt, *_history_to_lines(history), f"User: {user_message}", "Assistant:"]
    prompt = "\n".join(lines)

    try:
        resp = client.chat(
            model=COHERE_MODEL,
            message=prompt,
            temperature=0.7,
            max_tokens=80,
        )
        text = getattr(resp, "text", None)
        return text.strip() if isinstance(text, str) and text.strip() else None
    except Exception as e:
        print(f"[COHERE][ERROR] query: {type(e).__name__}: {e}")
        return None


# =========================
# OLLAMA LLM
# =========================

def query_ollama(user_message, system_prompt, history):
    """
    Ask a local Llama model via Ollama.
    Used as a fallback if Cohere fails or is disabled.
    """
    history_lines = _history_to_lines(history)
    guard = "System: Do NOT repeat long greetings. Answer directly and concisely."

    full_prompt = (
        system_prompt
        + "\n\n" + guard + "\n\n"
        + "\n".join(history_lines)
        + f"\nUser: {user_message}\nAssistant:"
    )

    payload = {
        "model": OLLAMA_MODEL,
        "prompt": full_prompt,
        "stream": False,
        "options": {"temperature": 0.7, "num_predict": 150},
    }

    try:
        r = requests.post(OLLAMA_ENDPOINT, json=payload, timeout=60)
        if r.status_code != 200:
            print(f"[OLLAMA][ERROR] HTTP {r.status_code}: {r.text[:200]}")
            return None

        data = r.json()
        for key in ("response", "text", "result"):
            val = data.get(key)
            if isinstance(val, str) and val.strip():
                return val.strip()
        if isinstance(data, str) and data.strip():
            return data.strip()

        print("[OLLAMA][WARN] No usable text in response.")
        return None
    except Exception as e:
        print(f"[OLLAMA][ERROR] query: {type(e).__name__}: {e}")
        return None


# =========================
# POST-PROCESSING
# =========================

def shorten_response(text, max_sentences=3):
    """Keep Aldric concise by limiting the number of sentences."""
    if not text:
        return text
    parts = re.split(r"(?<=[\.\?\!])\s+", text.strip())
    if len(parts) <= max_sentences:
        return text.strip()
    out = " ".join(parts[:max_sentences]).strip()
    if not re.search(r"[\.\?\!]$", out):
        out = out.rstrip(".") + "."
    return out


# =========================
# MAIN CHAT ENDPOINT
# =========================

@app.route("/chat", methods=["POST"])
def chat():
    """
    Main endpoint used by the Unity game.

    1. Read JSON ('message', optional 'system_prompt', 'session_id', 'preferred_provider').
    2. Load memory for this session (short-term + long-term).
    3. Build system prompt for Aldric.
    4. Try Cohere; if it fails or is overridden, try Ollama.
    5. Shorten and store answer, then return JSON.
    """
    try:
        print("\n" + "=" * 60)
        print("[REQUEST] /chat")

        data = request.get_json(silent=True) or {}
        user_message = data.get("message", "")
        custom_system_prompt = data.get("system_prompt", "")
        session_id = _sanitize_session_id(data.get("session_id", "default"))

        if not isinstance(user_message, str) or not user_message.strip():
            return jsonify({"error": "Field 'message' is required and must be non-empty."}), 400
        user_message = user_message.strip()

        # Inline commands: /cohere, /llama, /npc (npc reserved for future behaviour changes)
        provider_override = None
        mode_override = None
        found = CMD_PATTERN.findall(user_message)
        for tok in found:
            t = tok.lower()
            if t == "cohere":
                provider_override = "cohere"
            elif t == "llama":
                provider_override = "ollama"
            elif t == "npc":
                mode_override = "npc"

        if found:
            user_message = CMD_PATTERN.sub(" ", user_message)
            user_message = re.sub(r"\s+", " ", user_message).strip()

        # Or accept a preferred_provider flag in JSON
        if provider_override is None:
            pref = data.get("preferred_provider")
            if isinstance(pref, str) and pref.strip():
                p = pref.strip().lower()
                if p in ("llama", "ollama"):
                    provider_override = "ollama"
                elif p == "cohere":
                    provider_override = "cohere"
        else:
            pref = None  # just for logging

        print(f"[PARSE] commands={found}, provider_override={provider_override}, mode_override={mode_override}, preferred={pref}")

        # Build final system prompt
        if isinstance(custom_system_prompt, str) and custom_system_prompt.strip():
            final_system_prompt = MERCHANT_PROMPT + "\n\n" + custom_system_prompt.strip()
        else:
            final_system_prompt = MERCHANT_PROMPT

        # Load memory
        long_history = load_long_history(session_id, limit=LONG_TERM_MAX)
        short_history = get_short_history(session_id, limit=SHORT_TERM_MAX)
        history = long_history + short_history

        # Limit what actually goes into the LLM prompt
        model_history = history[-PROMPT_HISTORY_MAX:] if len(history) > PROMPT_HISTORY_MAX else history

        print(f"[REQUEST] session_id={session_id}")
        print(f"[REQUEST] user_message={user_message}")
        print("[REQUEST] history_len:", len(history))

        response_text = None
        provider_used = None

        # Respect explicit provider overrides first
        if provider_override == "cohere":
            if USE_COHERE:
                response_text = query_cohere(user_message, final_system_prompt, model_history)
                provider_used = "cohere" if response_text else None
            else:
                print("[REQUEST] Cohere override requested but COHERE is disabled.")

        elif provider_override == "ollama":
            if USE_OLLAMA:
                response_text = query_ollama(user_message, final_system_prompt, model_history)
                provider_used = "ollama" if response_text else None
                if not response_text and USE_COHERE:
                    print("[REQUEST] Ollama override failed, falling back to Cohere.")
                    response_text = query_cohere(user_message, final_system_prompt, model_history)
                    provider_used = "cohere" if response_text else None
            else:
                print("[REQUEST] Ollama override requested but OLLAMA is disabled.")

        # Default path: try Cohere, then Ollama
        if provider_override is None or provider_used is None:
            if USE_COHERE:
                response_text = query_cohere(user_message, final_system_prompt, model_history)
                provider_used = "cohere" if response_text else None

            if not response_text and USE_OLLAMA:
                print("[FALLBACK] Cohere failed or empty. Trying Ollama.")
                response_text = query_ollama(user_message, final_system_prompt, model_history)
                provider_used = "ollama" if response_text else provider_used

        print(f"[PROVIDER] provider_override={provider_override}, provider_used={provider_used}")

        if not response_text:
            msg = "All LLM providers failed. Check configuration and logs."
            print(f"[CRITICAL] {msg}")
            return jsonify({"error": msg}), 500

        shortened = shorten_response(response_text)

        # Store this turn in memory
        try:
            append_short_history(session_id, "user", user_message)
            append_short_history(session_id, "assistant", shortened)
            append_long_history(session_id, "user", user_message)
            append_long_history(session_id, "assistant", shortened)
        except Exception as e:
            print(f"[MEMORY][ERROR] persist: {e}")

        print(f"[FINAL][{provider_used}] {shortened[:200]}...")
        return jsonify({"response": shortened, "provider": provider_used, "session_id": session_id})

    except Exception as e:
        msg = f"Server error: {type(e).__name__}: {e}"
        print("[CRITICAL]", msg)
        return jsonify({"error": msg}), 500


# =========================
# SIMPLE AUX ENDPOINTS
# =========================

@app.route("/health", methods=["GET"])
def health():
    """Basic health check endpoint."""
    return jsonify({
        "status": "running",
        "cohere_enabled": USE_COHERE,
        "ollama_enabled": USE_OLLAMA,
        "cohere_model": COHERE_MODEL,
        "ollama_model": OLLAMA_MODEL,
        "cohere_api_key_set": bool(COHERE_API_KEY),
    })


@app.route("/history", methods=["GET"])
def history():
    """Inspect stored memory for a given session (mainly for debugging)."""
    session_id = _sanitize_session_id(request.args.get("session_id", "default"))
    return jsonify({
        "session_id": session_id,
        "short": get_short_history(session_id),
        "long": load_long_history(session_id),
    })


@app.route("/history/clear", methods=["POST"])
def history_clear():
    """Reset memory for a session."""
    data = request.get_json(silent=True) or {}
    session_id = _sanitize_session_id(data.get("session_id", "default"))
    clear_history(session_id)
    return jsonify({"status": "cleared", "session_id": session_id})


# =========================
# STARTUP
# =========================

if __name__ == "__main__":
    print("\n" + "=" * 60)
    print("FLASK CHAT SERVER")
    print("=" * 60)
    print(f"Cohere: {'ENABLED' if USE_COHERE else 'DISABLED'} (Model: {COHERE_MODEL})")
    print(f"Ollama: {'ENABLED' if USE_OLLAMA else 'DISABLED'} (Model: {OLLAMA_MODEL})")
    print("=" * 60)
    print("Chat:    POST /chat")
    print("Health:  GET  /health")
    print("History: GET  /history?session_id=...")
    print("History: POST /history/clear")
    print("=" * 60 + "\n")

    app.run(host="0.0.0.0", port=5000, debug=True)
