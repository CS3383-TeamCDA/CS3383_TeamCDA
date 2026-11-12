using UnityEngine;
namespace TwoHandedHero
{
    [CreateAssetMenu(menuName = "Two Handed Hero/Weapon")]
    public sealed class TwoHandedWeapon : ScriptableObject
    {
        public Sprite sprite;
        [Tooltip("Offsets in weapon-local Unity units; sprite pivot is the trigger-hand grip.")]
        public Vector2 supportGrip = new Vector2(.42f, .02f);
        public Vector2 muzzle = new Vector2(1f, .16f);
        [Min(.01f)] public float recoilDistance = .055f;
        [Min(.01f)] public float recoilSeconds = .16f;
    }
}
