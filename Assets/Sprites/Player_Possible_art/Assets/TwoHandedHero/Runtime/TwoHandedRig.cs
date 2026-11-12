using UnityEngine;
namespace TwoHandedHero
{
    // Visual-only rig: input, player physics, damage and projectile ownership remain external.
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
        public bool Moving { get; private set; }
        public bool FacingRight { get; private set; } = true;
        public TwoHandedWeapon Weapon { get; private set; }
        public string Action { get; private set; } = "Carry";
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

        public bool Fire()
        {
            if (Weapon == null) return false;
            recoilTime = Weapon.recoilSeconds;
            if (!Moving) Play("Shoot");
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
            if (Weapon != null && recoilTime > 0)
                position.x -= Weapon.recoilDistance * recoilTime / Weapon.recoilSeconds;
            grip.localPosition = (Vector2)shoulder.localPosition + position;
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
