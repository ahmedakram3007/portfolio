# Fantasy Shop — AI-Driven NPC Merchant

A real-time game system where an NPC merchant's dialogue and behaviour are driven by an external machine learning service, built as an MSc individual project.

## What it does
Connects a Unity game interface to an external ML service, with fallback logic so the NPC keeps behaving sensibly if the service is slow or unavailable.

## Tools
Unity (C#), Python

## Files
- [`AI_Merchant_Final_Report.docx`](AI_Merchant_Final_Report.docx) — the project report
- [`scripts/`](scripts) — the Unity C# scripts (`NPCController.cs`, `LLMAPIManager.cs`, `ChatManager.cs`, `ShopBGMManager.cs`, `ShopSFXManager.cs`, and others) — extracted from the full Unity project, which also depends on third-party asset packs not included here
- [`diagrams/`](diagrams) — architecture diagrams (`.drawio`, open at [app.diagrams.net](https://app.diagrams.net))
- [`gptgamemodel.ipynb`](gptgamemodel.ipynb) — the external ML service side (Python)

The NPC ("Aldric") is served through a Python Flask backend that routes chat requests to either Cohere (cloud, primary) or a locally hosted Llama model via Ollama (fallback), with runtime slash commands (`/llama`, `/cohere`, `/status`) for switching providers and checking connection state.

_TODO (Ahmed): a demo GIF/video would help a lot here — game/AI projects benefit a lot from being seen in action._

## Notes
Key challenges: Llama worked in the Unity editor but not in builds, due to networking differences — resolved with logging, certificate handling, and `link.xml` configuration. Provider switching was initially unstable because Flask responses overwrote Unity's preference values — fixed with a `preferred_provider` field in the JSON payload. NPC persona consistency was improved through prompt engineering and first-person voice rules.

Response time typically 2–5 seconds per message; the Cohere/Llama fallback logic kept the system available in the large majority of test sessions.
