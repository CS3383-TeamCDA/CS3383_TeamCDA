# TL2 AI Interaction Log

**AI tool:** OpenAI Codex (GPT-6)  
**Project:** Escape the 90s, generated world demo  
**Date:** November 5, 2026

This log records AI-assisted implementation work. The Unity project owner reviewed the generated changes.

## Interaction record

1. **Playable world generation:** Asked to turn the generated street graph into a playable demo. Codex connected graph routes to generated street rendering and route selection.
2. **Correct intersection choices:** Reported that left and right exits showed the same street, then clarified that choices must correspond to the endpoint the player reached. Codex used graph adjacency for the exact endpoint and route direction.
3. **Terrain streamed enemies:** Reported that enemies spawned before terrain and fell. Codex tied enemy and pickup creation/removal to loaded terrain chunks.
4. **Team feature integration:** Asked for the full demo and powerups without editing teammates’ source. Codex created a separate generated-world scene based on the existing demo, retained the TL1 menu/HUD, TL6 player, TL4 enemy systems, and connected TL3 powerups through a TL2 adapter.
5. **Project organization and progress update:** Asked for a short progress update, an AI usage log, proper TL2 source placement, and an archive that keeps the old demo usable. Codex moved current TL2 code into `Assets/src/TL2_Caleb` and retained the previous source under `Assets/_Archive/TL2_Caleb_Legacy`.
6. **Demo startup, enemy variety, and camera:** Reported that world generation began behind the menu, flying and turret enemies were missing, and camera bounds were wrong. Codex gated generated-world startup on the Playing state, set the demo to include all three enemy kinds, and bounded the camera to the generated street in world coordinates.

## Unity demonstration

- **Scene:** `Assets/Scenes/GeneratedWorldGameDemo.unity`
- **Class:** `EscapeThe90s.WorldGen.StreetView`
- **AI-generated function:** `ShowWorldStreet(WorldStreet street, bool reverse)`
- **Call site:** `WorldNetworkDemo.EnterStreet()`
- **Visible evidence:** Unity Console prints `[AI demo] EscapeThe90s.WorldGen.StreetView.ShowWorldStreet` when the demo displays a generated route.

The function renders the selected graph street using its generated layout and direction. The scene was assembled from the existing project systems and compiled/imported in Unity. A final Play Mode check should be done on the demo machine.

## Files

- `Assets/src/TL2_Caleb/Scripts/WorldGen/Unity/StreetView.cs`
- `Assets/src/TL2_Caleb/Scripts/WorldGen/Demo/WorldNetworkDemo.cs`
- `Assets/src/TL2_Caleb/Scripts/WorldGen/Integration/GeneratedPlayerPowerUpTarget.cs`
