# Level 1 Turret

Eight sprites: five discrete aiming poses (0, 45, 90, 135, 180 degrees) and three destruction frames. Six clips: five held aim poses and Death. AimIndex is 0-4, updated by the animation driver after each shot.

The prefab includes the enemy stats component (RangedOnly), Animator, EnemyRangedAttack and EnemyAnimationDriver. Assign the shooter Target to enable firing. Shot events update aim/shoot visuals; defeat triggers Death. Projectile collision damage and movement AI remain separate integration work. A muzzle transform is included and can be adjusted to suit final art.

Artwork was generated with the built-in imagegen tool using the existing ground enemy as the purple/teal/red pixel-art style reference. Prompt brief: stationary armored turret with a fixed base and five cannon angles plus damage/destruction poses, 4x2 transparent atlas.

Imported and validated in Unity 6000.6.0f1. Point filtering, transparent alpha, no texture compression or mipmaps. These are initial AI-generated animation assets and may benefit from art polish during playtesting.
