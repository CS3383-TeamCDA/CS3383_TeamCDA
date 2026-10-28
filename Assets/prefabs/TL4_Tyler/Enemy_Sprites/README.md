# TL4 Tyler Prefab Assets

## Overview

`Assets/prefabs/TL4_Tyler/` contains Tyler's enemy prefabs, sprite artwork, animation assets, and supporting scene assets for the escape game. The main enemy assets are organized under `Enemy_Sprites/` into ground, flying, and turret folders. Each enemy folder brings together its prefab, artwork, Animator controller, animation clips, and a README with setup details.

## Asset folders

| Folder | High-level contents |
| --- | --- |
| [Level1_Ground](Level1_Ground/) | Animated ground enemy with idle, walk, run, shoot, melee, hit, and death animations. Includes a projectile prefab and a spawn-point prefab for placing enemy spawn locations. |
| [Level1_Flying](Level1_Flying/) | Flying enemy prefab, sprite sheet, and animations for idle, flight, shooting, and death. |
| [Level1_Turret](Level1_Turret/) | Stationary turret prefab and sprite sheet, with five aiming poses from 0 to 180 degrees and a death animation. |

Within these folders, `Animations/` holds Unity animation clips, and `.controller` files define the animation states. The ground enemy also has a `Sprites/` subfolder containing separate sprite sheets for its character actions. The enemy artwork shares a purple, teal, and red visual style.

## Supporting assets

The parent `TL4_Tyler/` folder also contains `TylerGroundEnemy.prefab`, `GroundEnemyPlaceholder.png`, and `testscene.unity`. These sit alongside the organized enemy assets in `Enemy_Sprites/`.

## How the assets fit together

The prefabs combine enemy visuals with gameplay components, including configurable enemy stats and ranged attacks. Sprite sheets provide the artwork, animation clips define its motion, and Animator controllers select the visual states. The ground enemy spawn-point prefab supports placing spawn markers in room layouts.

These assets provide the enemy building blocks; movement, targeting, projectile collision damage, and other gameplay connections still require integration as described in the individual READMEs. See the [ground enemy guide](Level1_Ground/README.md), [flying enemy guide](Level1_Flying/README.md), and [turret guide](Level1_Turret/README.md) for setup instructions and current limitations.

## Demo

![alt text](<Screen Recording 2026-10-22 at 4.08.19 PM (1).gif>)