
using UnityEngine;
using TwoHandedHero;

public class PlayerVisualController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private TwoHandedRig rig;
    [SerializeField] private PlayerControllerTL6 controller;
    [SerializeField] private Rigidbody2D rb;
    [SerializeField] private PlayerCombatInputHandler combatInput;
    private Camera playerCamera;
    private Vector2 _aimDirection = Vector2.right;

    public Vector2 AimDirection => _aimDirection;
    public Vector2 MuzzlePosition =>
        (Vector2)rig.muzzle.position;

    private void LateUpdate()
    {
        if (rig == null || controller == null || rb == null)
            return;

        UpdateAiming();
        UpdateFacing();
        UpdateLocomotion();
    }

    private void UpdateFacing()
    {
        // Horizontal aiming determines which way the player faces.
        if (Mathf.Abs(_aimDirection.x) > 0.1f)
        {
            rig.SetFacingRight(_aimDirection.x > 0f);
        }
    }

    private void UpdateLocomotion()
    {
        Vector2 velocity = rb.linearVelocity;

        rig.SetLocomotion(
            grounded: controller.IsGrounded(),
            horizontalSpeed: velocity.x,
            verticalSpeed: velocity.y,
            crouching: controller.IsCrouching,
            sliding: false,
            dashing: controller.IsDashing
        );
    }

    private void UpdateAiming()
    {
        if (playerCamera == null)
            playerCamera = Camera.main;

        if (playerCamera == null || combatInput == null)
            return;

        Vector2 screenPosition = combatInput.AimScreenPosition;

        Vector3 mouseWorld = playerCamera.ScreenToWorldPoint(
            new Vector3(
                screenPosition.x,
                screenPosition.y,
                -playerCamera.transform.position.z
            )
        );

        Vector2 rawDirection =
            (Vector2)mouseWorld - (Vector2)transform.position;

        Vector2 aimDirection = EightDirectionAim.Snap(rawDirection);

        if (aimDirection == Vector2.zero)
            return;

        _aimDirection = aimDirection;
        rig.SetAimDirection(_aimDirection);
    }

    public bool TryPlayFireAnimation()
    {
        return rig != null && rig.Fire();
    }
}
