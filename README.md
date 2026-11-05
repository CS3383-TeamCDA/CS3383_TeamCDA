# CS3383_TeamCDA

## Generated world game demo

Open `Assets/Scenes/GeneratedWorldGameDemo.unity` and press **Play**. This scene is a copy of the existing TL5 `GameDemo`;
the original scene remains unchanged. It keeps the TL6 player, TL1 menu/HUD, TL3 power-up manager, TL4 enemy spawner and attack,
while replacing the old fixed terrain with generated, streamed world streets and the intersection route graph.

Press **Start Game** on the menu to build the generated world and place the player. Use **A/D** to move and **Space** to jump. At a street end, select one of that graph node's onward routes with the graph or
the **up/down** keys, then press **Enter**. Generated enemies and power-ups appear only while their terrain chunks are loaded.
The demo cycles ground, flying, and turret enemies so every type appears, and the camera clamps to the loaded street's full endpoints.
Speed boosts, healing shields, and magnet pickups are connected to the TL6 player's movement, health, and pickup radius by
`GeneratedPlayerPowerUpTarget` in the worldgen integration assembly. No TL3 or TL6 source files are changed for that adapter.

Current TL2 code lives under `Assets/src/TL2_Caleb`. The previous TL2 source tree remains available under
`Assets/_Archive/TL2_Caleb_Legacy`; Unity metadata and existing scene/prefab references were retained.

The integrated scene is the first enabled entry in `ProjectSettings/EditorBuildSettings.asset`, so a player build starts in
the generated world demo. To recreate it in the Unity Editor, use **Escape the 90s > Create Integrated Generated World Demo**;
the command creates a new scene and leaves `Assets/Scenes/TL5_Will/GameDemo.unity` intact.
