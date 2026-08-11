# from flask import Flask, request, jsonify
# from flask_cors import CORS
# import cohere
# import requests
# import os
# from collections import deque
# from pathlib import Path
# import json

# app = Flask(__name__)
# CORS(app)  # allow Unity to call this API


# # =========================
# # CONFIGURATION
# # =========================
# # I keep all configuration at the top so it is easy to change or explain.

# COHERE_API_KEY = os.getenv("COHERE_API_KEY", "")  # redacted for public repo -- set via environment variable
# COHERE_MODEL = "command-r-plus-08-2024"

# OLLAMA_ENDPOINT = "http://localhost:11434/api/generate"
# OLLAMA_MODEL = os.getenv("OLLAMA_MODEL", "llama3.2:latest")

# USE_COHERE = bool(COHERE_API_KEY)
# USE_OLLAMA = True

# # Default “character” behaviour for the NPC merchant.
# MERCHANT_PROMPT = (
#     "Your name is Aldric. Speak in first person as Aldric the merchant in a fantasy RPG.\n"
#     "Personality: Helpful, concise, and in-character. Keep responses short (at most 3 sentences).\n"
#     "Style: Refer to your wares, prices and shop in first person (e.g. 'I have', 'My wares', 'I can offer').\n"
#     "Constraints: Do NOT introduce yourself unless this is the first message of the conversation.\n"
#     "Begin directly with the content and, if useful, ask at most one short follow-up question."
# )

# # Where I store long-term memory on disk, so conversations survive server restarts.
# BASE_DIR = os.path.dirname(__file__)
# MEMORY_DIR = os.path.join(BASE_DIR, "memory")

# SHORT_TERM_MAX = 8      # how many recent turns to keep in RAM
# LONG_TERM_MAX = 200     # how many past turns to load from disk
# PROMPT_HISTORY_MAX = 40  # how many recent turns to send to the LLM when building the prompt

# # In-memory short-term history: session_id -> deque of {"role": "user"/"assistant", "text": "..."}
# conversation_histories = {}

# cohere_client = None


# # =========================
# # BASIC HELPERS
# # =========================

# def _sanitize_session_id(sid: str) -> str:
#     """Make sure the session id is safe to use as a file name and dict key."""
#     if not sid:
#         return "default"
#     safe = "".join(c for c in sid if c.isalnum() or c in ("-", "_"))
#     return safe or "default"


# def _ensure_memory_dir():
#     """Make sure the memory directory exists before writing files."""
#     Path(MEMORY_DIR).mkdir(parents=True, exist_ok=True)


# # =========================
# # MEMORY FUNCTIONS
# # =========================

# def append_short_history(session_id: str, role: str, text: str):
#     """
#     Short-term memory: I keep the last few turns in RAM.
#     This is used to give the LLM some immediate context.
#     """
#     sid = _sanitize_session_id(session_id)
#     dq = conversation_histories.get(sid)
#     if dq is None:
#         dq = deque(maxlen=SHORT_TERM_MAX)
#         conversation_histories[sid] = dq
#     dq.append({"role": role, "text": text})


# def get_short_history(session_id: str, limit: int | None = None):
#     """Return the most recent messages from short-term memory for a session."""
#     sid = _sanitize_session_id(session_id)
#     dq = conversation_histories.get(sid, deque())
#     items = list(dq)
#     if limit is not None:
#         return items[-limit:]
#     return items


# def append_long_history(session_id: str, role: str, text: str):
#     """
#     Long-term memory: this writes each message to a JSONL file on disk.
#     Because it is on disk, the NPC can remember things even after
#     we close and restart the game or server.
#     """
#     sid = _sanitize_session_id(session_id)
#     _ensure_memory_dir()
#     path = os.path.join(MEMORY_DIR, f"history_{sid}.jsonl")
#     try:
#         with open(path, "a", encoding="utf-8") as f:
#             f.write(json.dumps({"role": role, "text": text}) + "\n")
#     except Exception as e:
#         print(f"[MEMORY][ERROR] append_long_history: {e}")


# def load_long_history(session_id: str, limit: int | None = None):
#     """
#     Load previous conversation turns from disk for this session.
#     This is what lets Aldric remember things like 'you killed the dragon'
#     across different play sessions.
#     """
#     sid = _sanitize_session_id(session_id)
#     path = os.path.join(MEMORY_DIR, f"history_{sid}.jsonl")
#     if not os.path.exists(path):
#         return []
#     items = []
#     try:
#         with open(path, "r", encoding="utf-8") as f:
#             for line in f:
#                 line = line.strip()
#                 if not line:
#                     continue
#                 try:
#                     items.append(json.loads(line))
#                 except Exception:
#                     items.append({"role": "system", "text": line})
#     except Exception as e:
#         print(f"[MEMORY][ERROR] load_long_history: {e}")
#         return []
#     if limit is not None:
#         return items[-limit:]
#     return items


# def clear_history(session_id: str):
#     """Optional helper to clear both short- and long-term memory for a session."""
#     sid = _sanitize_session_id(session_id)
#     conversation_histories.pop(sid, None)
#     path = os.path.join(MEMORY_DIR, f"history_{sid}.jsonl")
#     if os.path.exists(path):
#         try:
#             open(path, "w", encoding="utf-8").close()
#         except Exception as e:
#             print(f"[MEMORY][ERROR] clear_history: {e}")


# # =========================
# # COHERE LLM
# # =========================

# def get_cohere_client():
#     """
#     Create the Cohere client once and reuse it.
#     """
#     global cohere_client
#     if cohere_client is not None:
#         return cohere_client
#     if not USE_COHERE:
#         print("[COHERE] Disabled (no API key set).")
#         return None
#     try:
#         client = cohere.Client(COHERE_API_KEY)
#         cohere_client = client
#         print("[COHERE] Client initialised.")
#         return cohere_client
#     except Exception as e:
#         print(f"[COHERE][ERROR] client init: {type(e).__name__}: {e}")
#         cohere_client = None
#         return None


# def query_cohere(user_message: str, system_prompt: str, history: list[dict]) -> str | None:
#     """
#     Ask Cohere for a response.
#     To keep it simple, I flatten the history into a single string prompt.
#     """
#     client = get_cohere_client()
#     if client is None:
#         return None

#     lines = [system_prompt]
#     for m in history:
#         role = m.get("role", "user")
#         text = m.get("text", "")
#         if not text:
#             continue
#         prefix = "User" if role == "user" else "Assistant"
#         lines.append(f"{prefix}: {text}")
#     lines.append(f"User: {user_message}")
#     lines.append("Assistant:")

#     prompt = "\n".join(lines)

#     try:
#         resp = client.chat(
#             model=COHERE_MODEL,
#             message=prompt,
#             temperature=0.7,
#             max_tokens=80,
#         )
#         text = getattr(resp, "text", None)
#         if isinstance(text, str) and text.strip():
#             return text.strip()
#         return None
#     except Exception as e:
#         print(f"[COHERE][ERROR] query: {type(e).__name__}: {e}")
#         return None


# # =========================
# # OLLAMA LLM
# # =========================

# def query_ollama(user_message: str, system_prompt: str, history: list[dict]) -> str | None:
#     """
#     Ask a local Llama model via Ollama for a response.
#     This is used as a fallback if Cohere fails.
#     """
#     history_lines = []
#     for m in history:
#         role = m.get("role", "user")
#         text = m.get("text", "")
#         if not text:
#             continue
#         prefix = "User" if role == "user" else "Assistant"
#         history_lines.append(f"{prefix}: {text}")

#     guard = (
#         "System: Do NOT repeat long greetings. Answer directly and concisely."
#     )

#     full_prompt = (
#         system_prompt
#         + "\n\n"
#         + guard
#         + "\n\n"
#         + "\n".join(history_lines)
#         + f"\nUser: {user_message}\nAssistant:"
#     )

#     payload = {
#         "model": OLLAMA_MODEL,
#         "prompt": full_prompt,
#         "stream": False,
#         "options": {"temperature": 0.7, "num_predict": 150},
#     }

#     try:
#         # Allow more time for local Llama models which can be slow on first token
#         r = requests.post(OLLAMA_ENDPOINT, json=payload, timeout=60)
#         if r.status_code != 200:
#             print(f"[OLLAMA][ERROR] HTTP {r.status_code}: {r.text[:200]}")
#             return None

#         data = r.json()
#         for key in ("response", "text", "result"):
#             val = data.get(key)
#             if isinstance(val, str) and val.strip():
#                 return val.strip()

#         if isinstance(data, str) and data.strip():
#             return data.strip()

#         print("[OLLAMA][WARN] No usable text in response.")
#         return None
#     except Exception as e:
#         print(f"[OLLAMA][ERROR] query: {type(e).__name__}: {e}")
#         return None


# # =========================
# # POST-PROCESSING
# # =========================

# def shorten_response(text: str, max_sentences: int = 3) -> str:
#     """
#     To keep Aldric's answers short and merchant-like,
#     I limit the output to a few sentences.
#     """
#     if not text:
#         return text
#     import re
#     parts = re.split(r"(?<=[\.\?\!])\s+", text.strip())
#     if len(parts) <= max_sentences:
#         return text.strip()
#     out = " ".join(parts[:max_sentences]).strip()
#     if not re.search(r"[\.\?\!]$", out):
#         out = out.rstrip(".") + "."
#     return out


# # =========================
# # MAIN CHAT ENDPOINT
# # =========================

# @app.route("/chat", methods=["POST"])
# def chat():
#     """
#     Main endpoint used by the Unity game.

#     Steps:
#     1. Read JSON from Unity (expects 'message' and optional 'system_prompt' and 'session_id').
#     2. Load past memory for this session (short-term from RAM, long-term from disk).
#     3. Build a system prompt that describes the merchant's personality.
#     4. Try Cohere; if it fails, try Ollama.
#     5. Shorten the answer, store it back into memory, and return JSON.
#     """
#     try:
#         print("\n" + "=" * 60)
#         print("[REQUEST] /chat")

#         data = request.get_json(silent=True) or {}
#         user_message = data.get("message", "")
#         custom_system_prompt = data.get("system_prompt", "")
#         session_id = _sanitize_session_id(data.get("session_id", "default"))

#         if not isinstance(user_message, str) or not user_message.strip():
#             return jsonify({"error": "Field 'message' is required and must be non-empty."}), 400

#         user_message = user_message.strip()

#         # Inline command parsing: allow callers to include /cohere, /llama, or /npc
#         # anywhere in the message to override provider or mode for this request.
#         provider_override = None
#         mode_override = None
#         import re
#         cmd_pat = re.compile(r'(?i)(?:^|[^a-z0-9])/(cohere|llama|npc)(?:$|[^a-z0-9])')
#         found = cmd_pat.findall(user_message)
#         if found:
#             for tok in found:
#                 t = tok.lower()
#                 if t == 'cohere':
#                     provider_override = 'cohere'
#                 elif t == 'llama':
#                     provider_override = 'ollama'
#                 elif t == 'npc':
#                     mode_override = 'npc'
#         # Debug: show what we parsed from the incoming message
#         try:
#             print(f"[PARSE] tokens={found}, provider_override={provider_override}, mode_override={mode_override}")
#         except Exception:
#             pass

#         if provider_override or mode_override:
#             user_message = re.sub(r'(?i)(?:^|[^a-z0-9])/(?:cohere|llama|npc)(?:$|[^a-z0-9])', ' ', user_message)
#             user_message = re.sub(r'\s+', ' ', user_message).strip()

#         # If no inline command, accept a preferred_provider field from JSON payload
#         if provider_override is None:
#             pref = data.get('preferred_provider')
#             if isinstance(pref, str) and pref.strip():
#                 p = pref.strip().lower()
#                 if p in ('llama', 'ollama'):
#                     provider_override = 'ollama'
#                 elif p == 'cohere':
#                     provider_override = 'cohere'
#             try:
#                 print(f"[PREFERRED] preferred_provider={pref} -> provider_override={provider_override}")
#             except Exception:
#                 pass

#         # Build final system prompt (base merchant prompt plus any custom instructions).
#         if isinstance(custom_system_prompt, str) and custom_system_prompt.strip():
#             final_system_prompt = MERCHANT_PROMPT + "\n\n" + custom_system_prompt.strip()
#         else:
#             final_system_prompt = MERCHANT_PROMPT

#         # Load memory: first long-term (from disk), then short-term (from RAM).
#         long_history = load_long_history(session_id, limit=LONG_TERM_MAX)
#         short_history = get_short_history(session_id, limit=SHORT_TERM_MAX)
#         history = long_history + short_history

#         # Use a truncated copy of history when building prompts to keep prompt size
#         # reasonable and avoid long LLM latency that can make Unity client time out.
#         if len(history) > PROMPT_HISTORY_MAX:
#             model_history = history[-PROMPT_HISTORY_MAX:]
#             try:
#                 print(f"[HISTORY] full={len(history)} trimmed_to={len(model_history)} for model prompt")
#             except Exception:
#                 pass
#         else:
#             model_history = history

#         print(f"[REQUEST] session_id={session_id}")
#         print(f"[REQUEST] user_message={user_message}")
#         print("[REQUEST] using history length:", len(history))

#         # Try provider according to inline override (if given), otherwise try Cohere then Ollama.
#         response_text = None
#         provider_used = None

#         if provider_override == 'cohere':
#             if USE_COHERE:
#                 response_text = query_cohere(user_message, final_system_prompt, model_history)
#                 if response_text:
#                     provider_used = 'cohere'
#             else:
#                 print('[REQUEST] Cohere override requested but COHERE is disabled.')

#         elif provider_override == 'ollama':
#             if USE_OLLAMA:
#                 response_text = query_ollama(user_message, final_system_prompt, model_history)
#                 if response_text:
#                     provider_used = 'ollama'
#                 else:
#                     print('[REQUEST] Ollama override requested but Ollama returned no response or timed out.')
#                     # If Ollama fails for an explicit override, try Cohere as a fallback
#                     if USE_COHERE:
#                         print('[FALLBACK] Ollama failed. Trying Cohere as a fallback.')
#                         response_text = query_cohere(user_message, final_system_prompt, history)
#                         if response_text:
#                             provider_used = 'cohere'
#             else:
#                 print('[REQUEST] Ollama override requested but OLLAMA is disabled.')

#         else:
#             # Default flow: try Cohere then Ollama
#             if USE_COHERE:
#                 response_text = query_cohere(user_message, final_system_prompt, model_history)
#                 if response_text:
#                     provider_used = 'cohere'

#             if not response_text and USE_OLLAMA:
#                 print('[FALLBACK] Cohere failed or empty. Trying Ollama.')
#                 response_text = query_ollama(user_message, final_system_prompt, model_history)
#                 if response_text:
#                     provider_used = 'ollama'

#         # Debug: report provider resolution
#         try:
#             print(f"[PROVIDER RESOLVE] provider_override={provider_override}, provider_used={provider_used}")
#         except Exception:
#             pass

#         if not response_text:
#             msg = "All LLM providers failed. Check configuration and logs."
#             print(f"[CRITICAL] {msg}")
#             return jsonify({"error": msg}), 500

#         shortened = shorten_response(response_text)

#         # Store this turn in both short-term and long-term memory.
#         try:
#             append_short_history(session_id, "user", user_message)
#             append_short_history(session_id, "assistant", shortened)
#             append_long_history(session_id, "user", user_message)
#             append_long_history(session_id, "assistant", shortened)
#         except Exception as e:
#             print(f"[MEMORY][ERROR] persist: {e}")

#         print(f"[FINAL][{provider_used}] {shortened[:200]}...")
#         return jsonify({
#             "response": shortened,
#             "provider": provider_used,
#             "session_id": session_id
#         })

#     except Exception as e:
#         msg = f"Server error: {type(e).__name__}: {e}"
#         print("[CRITICAL]", msg)
#         return jsonify({"error": msg}), 500


# # =========================
# # SIMPLE AUX ENDPOINTS
# # =========================

# @app.route("/health", methods=["GET"])
# def health():
#     """Simple health check endpoint used for debugging."""
#     return jsonify({
#         "status": "running",
#         "cohere_enabled": USE_COHERE,
#         "ollama_enabled": USE_OLLAMA,
#         "cohere_model": COHERE_MODEL,
#         "ollama_model": OLLAMA_MODEL,
#         "cohere_api_key_set": bool(COHERE_API_KEY),
#     })


# @app.route("/history", methods=["GET"])
# def history():
#     """
#     Debug endpoint to inspect stored memory for a given session.
#     This is useful during development but not required for gameplay.
#     """
#     session_id = _sanitize_session_id(request.args.get("session_id", "default"))
#     return jsonify({
#         "session_id": session_id,
#         "short": get_short_history(session_id),
#         "long": load_long_history(session_id),
#     })


# @app.route("/history/clear", methods=["POST"])
# def history_clear():
#     """
#     Optional endpoint to reset memory for a session.
#     This can be called from a debug menu or during testing.
#     """
#     data = request.get_json(silent=True) or {}
#     session_id = _sanitize_session_id(data.get("session_id", "default"))
#     clear_history(session_id)
#     return jsonify({"status": "cleared", "session_id": session_id})


# # =========================
# # STARTUP
# # =========================

# if __name__ == "__main__":
#     print("\n" + "=" * 60)
#     print("FLASK CHAT SERVER")
#     print("=" * 60)
#     print(f"Cohere: {'ENABLED' if USE_COHERE else 'DISABLED'} (Model: {COHERE_MODEL})")
#     print(f"Ollama: {'ENABLED' if USE_OLLAMA else 'DISABLED'} (Model: {OLLAMA_MODEL})")
#     print("=" * 60)
#     print("Chat:   POST /chat")
#     print("Health: GET  /health")
#     print("History: GET /history?session_id=...")
#     print("=" * 60 + "\n")

#     app.run(host="0.0.0.0", port=5000, debug=True)