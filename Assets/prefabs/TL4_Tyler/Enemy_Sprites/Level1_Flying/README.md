# Level 1 Flying

Sixteen sprites: four frames each of Idle, Fly, Shoot and Death. Idle loops at 8 sprite FPS; Fly at 12. Shoot plays at 12 and Death at 8. Call EnemyAnimationDriver.SetMoving(bool) from movement logic to select Idle/Fly.

The prefab includes the enemy stats component (RangedOnly), Animator, EnemyRangedAttack and EnemyAnimationDriver. Assign the shooter Target to enable firing. Shot events update aim/shoot visuals; defeat triggers Death. Projectile collision damage and movement AI remain separate integration work. A muzzle transform is included and can be adjusted to suit final art.

Artwork was generated with the built-in imagegen tool using the existing ground enemy as the purple/teal/red pixel-art style reference. Prompt brief: right-facing purple alien drone with teal armor and thrusters; hover, forward flight, shooting and death sequences in a 4x4 transparent atlas; corrected spacing to keep effects inside frames.

Imported and validated in Unity 6000.6.0f1. Point filtering, transparent alpha, no texture compression or mipmaps. These are initial AI-generated animation assets and may benefit from art polish during playtesting.
