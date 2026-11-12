using UnityEngine;
namespace TwoHandedHero
{
    // Visual-only rig: input, player physics, damage and projectile ownership remain external.
    [DefaultExecutionOrder(450)]
    public sealed class TwoHandedRig : MonoBehaviour
    {
        public Animator bodyAnimator;
        public Transform visualRoot, shoulder, grip, supportHand, frontHand, muzzle;
        public SpriteRenderer weaponRenderer, muzzleFlash;
        public TwoHandedWeapon startingWeapon;
        public float runCycleSeconds = 1f, stopSeconds = .5f, shootSeconds = .333333f;
        public Vector2 carryGrip = new Vector2(.67f, -.14f);
        public Vector2 runGrip = new Vector2(.67f, -.14f);
        public float runBob = .025f;
        [Tooltip("Keep the carry/run body animation while the weapon and hands recoil.")]
        public bool stableBodyWhenFiring;
        [Min(0f)] public float recoilDistanceMultiplier = 1f;
        [Min(0f)] public float recoilRiseDegrees;
        public bool Moving { get; private set; }
        public bool FacingRight { get; private set; } = true;
        public TwoHandedWeapon Weapon { get; private set; }
        public string Action { get; private set; } = "Carry";
        public Vector3 AimOrigin { get; private set; }
        Vector2 aimDirection;
        float landingRemaining;

        public void SetAimDirection(Vector2 direction) { aimDirection = direction.normalized; }
        public void BeginLanding(float duration)
        {
            landingRemaining = Mathf.Max(0, duration);
            if (landingRemaining <= 0) return;
            Moving = false;
            Play("Landing");
        }
        float actionTime, runTime, recoilTime;
        Vector3 rootScale;

        void Awake()
        {
            rootScale = visualRoot.localScale;
            Equip(startingWeapon);
            Play("Carry");
        }

        public void Equip(TwoHandedWeapon weapon)
        {
            Weapon = weapon;
            weaponRenderer.sprite = weapon != null ? weapon.sprite : null;
            recoilTime = 0;
            if (weapon != null)
            {
                supportHand.localPosition = weapon.supportGrip;
                muzzle.localPosition = weapon.muzzle;
            }
            if (muzzleFlash != null) muzzleFlash.enabled = false;
        }

        public void SetFacingRight(bool right)
        {
            FacingRight = right;
            visualRoot.localScale = new Vector3(Mathf.Abs(rootScale.x) * (right ? 1 : -1), rootScale.y, rootScale.z);
        }

        public void SetMoving(bool moving)
        {
            if (Moving == moving) return;
            Moving = moving;
            if (moving) { runTime = 0; Play("Run"); }
            else Play("Stop");
        }

        // Requires Jump/Fall states in the assigned animator controller.
        public void SetLocomotion(bool grounded, float horizontalSpeed, float verticalSpeed,
            bool crouching = false, bool sliding = false, bool dashing = false)
        {
            if (dashing)
            {
                landingRemaining = 0;
                Moving = false;
                if (Action != "Dash") Play("Dash");
                return;
            }
            if (crouching)
            {
                landingRemaining = 0;
                Moving = false;
                string stance = sliding ? "Slide" : Mathf.Abs(horizontalSpeed) > .1f ? "CrouchWalk" : "Crouch";
                if (Action != stance) Play(stance);
                return;
            }
            if (!grounded || verticalSpeed > .1f)
            {
                landingRemaining = 0;
                Moving = false;
                // Keep the tucked jump pose briefly through the apex; walking off
                // a ledge goes straight to Fall rather than playing a takeoff.
                string action = verticalSpeed > .1f ||
                    (Action == "Jump" && verticalSpeed > -.3f) ? "Jump" : "Fall";
                if (Action != action) Play(action);
                return;
            }
            if (landingRemaining > 0)
            {
                Moving = false;
                if (Action != "Landing") Play("Landing");
                return;
            }
            if (Action == "Jump" || Action == "Fall" || Action == "Crouch" || Action == "CrouchWalk" || Action == "Slide" || Action == "Dash" || Action == "Landing") Play("Carry");
            SetMoving(Mathf.Abs(horizontalSpeed) > .1f);
        }

        public bool Fire()
        {
            if (Weapon == null) return false;
            recoilTime = Weapon.recoilSeconds;
            if (!Moving && Action != "Jump" && Action != "Fall" && Action != "Crouch" && Action != "CrouchWalk" && Action != "Slide" && Action != "Dash" && Action != "Landing")
            {
                if (stableBodyWhenFiring)
                {
                    if (Action != "Carry") Play("Carry");
                }
                else Play("Shoot");
            }
            return true;
        }

        void Play(string action)
        {
            Action = action; actionTime = 0;
            if (bodyAnimator != null) bodyAnimator.Play(action, 0, 0);
        }

        void Update()
        {
            actionTime += Time.deltaTime;
            landingRemaining = Mathf.Max(0, landingRemaining - Time.deltaTime);
            if (Moving) runTime += Time.deltaTime;
            if ((Action == "Stop" && actionTime >= stopSeconds) ||
                (Action == "Shoot" && actionTime >= shootSeconds)) Play("Carry");
            recoilTime = Mathf.Max(0, recoilTime - Time.deltaTime);
        }

        void LateUpdate()
        {
            Vector2 position = Moving ? runGrip : carryGrip;
            bool stopping = Action == "Stop";
            // Lower the rifle during braking while keeping the trigger arm connected at the shoulder.
            if (stopping) position = new Vector2(.58f, -.36f);
            frontHand.localRotation = Quaternion.Euler(0, 0, stopping ? -20 : 0);
            supportHand.localRotation = Quaternion.Euler(0, 0, stopping ? 5 : 17);
            if (Moving) position.y += Mathf.Sin(runTime * Mathf.PI * 4 / Mathf.Max(.01f, runCycleSeconds)) * runBob;
            float recoil = Weapon != null && recoilTime > 0
                ? recoilTime / Mathf.Max(.01f, Weapon.recoilSeconds) : 0f;
            // Kick the shared weapon grip, so rifle, both hands and muzzle stay together.
            position.x -= Weapon != null ? Weapon.recoilDistance * recoilDistanceMultiplier * recoil : 0f;
            Vector2 shoulderOffset = Vector2.zero;
            if (Action == "Slide")
            {
                // The seated torso sits behind the feet pivot. Follow its shoulder
                // through the entry frames instead of using the standing X anchor.
                var state = bodyAnimator.GetCurrentAnimatorStateInfo(0);
                float elapsed = state.normalizedTime * state.length;
                float x = elapsed < .0625f ? -.58f : elapsed < .125f ? -.59f : elapsed < .1875f ? -.62f : -.69f;
                shoulderOffset = new Vector2(x, 0f);
            }
            else if (Action == "Crouch" || Action == "CrouchWalk")
                shoulderOffset = new Vector2(-.16f, -.1f);
            else if (Action == "Dash")
            {
                var state = bodyAnimator.GetCurrentAnimatorStateInfo(0);
                float elapsed = state.normalizedTime * state.length;
                shoulderOffset.x = elapsed < .025f ? -.14f : elapsed < .05f ? -.08f : elapsed < .075f ? -.03f : 0;
            }
            else if (Action == "Landing")
            {
                var state = bodyAnimator.GetCurrentAnimatorStateInfo(0);
                shoulderOffset.x = state.normalizedTime * state.length < .09375f ? -.18f : .05f;
            }
            Vector2 origin = (Vector2)shoulder.localPosition + shoulderOffset;
            // Rotate about the shoulder, not the trigger hand. Include mirroring
            // and the flip pivot when converting the requested world aim to local.
            Vector3 localAim = aimDirection.sqrMagnitude > .01f
                ? visualRoot.InverseTransformVector(aimDirection) : Vector3.right;
            float aimAngle = Mathf.Atan2(localAim.y, localAim.x) * Mathf.Rad2Deg;
            Quaternion aim = Quaternion.Euler(0, 0, aimAngle);
            grip.localRotation = Quaternion.Euler(0, 0, aimAngle + recoilRiseDegrees * recoil);
            grip.localPosition = origin + (Vector2)(aim * position);
            AimOrigin = visualRoot.TransformPoint(origin);
            // Both hands share weapon recoil; the support hand follows the equipped weapon's anchor.
            frontHand.localPosition = Vector3.zero;
            if (Weapon != null)
            {
                supportHand.localPosition = Weapon.supportGrip;
                muzzle.localPosition = Weapon.muzzle;
            }
            if (muzzleFlash != null)
                muzzleFlash.enabled = Weapon != null && recoilTime > Weapon.recoilSeconds - .06f;
        }
    }
}
