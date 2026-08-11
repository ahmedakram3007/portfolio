# Fantasy Shop — AI-Driven NPC Merchant

A real-time game system where an NPC merchant's dialogue and behaviour are driven by an external machine learning service, built as an MSc individual project.

## What it does
Connects a Unity game interface to an external ML service, with fallback logic so the NPC keeps behaving sensibly if the service is slow or unavailable.

## Tools
Unity (C#), Python

## Files
- [`scripts/`](scripts) — the Unity C# scripts (`NPCController.cs`, `LLMAPIManager.cs`, `ChatManager.cs`, `ShopBGMManager.cs`, `ShopSFXManager.cs`, and others) — extracted from the full Unity project, which also depends on third-party asset packs not included here
- [`diagrams/`](diagrams) — architecture diagrams (`.drawio`, open at [app.diagrams.net](https://app.diagrams.net))
- [`gptgamemodel.ipynb`](gptgamemodel.ipynb) — the external ML service side (Python)

_TODO (Ahmed): a demo GIF/video would help a lot here — game/AI projects benefit a lot from being seen in action._

## Notes
_TODO (Ahmed): add notes on the architecture choices (memory management, fallback mechanisms) and what you'd improve with more time._
