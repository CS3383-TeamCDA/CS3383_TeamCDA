using UnityEngine;
namespace TwoHandedHero
{
    public sealed class TwoHandedPreview : MonoBehaviour
    {
        public TwoHandedRig hero;
        public TwoHandedWeapon[] weapons;
        public bool autoCycle = true;
        public float movementSpeed = 1.8f;
        float sequenceTime, shotTime;
        int sequence = -1;
        bool direction = true;
        void Update()
        {
            if (autoCycle)
            {
                sequenceTime += Time.deltaTime;
                int next = (int)(sequenceTime / 3) % 4;
                if (next != sequence)
                {
                    sequence = next;
                    if (next == 0) hero.SetMoving(false);
                    if (next == 1) hero.SetMoving(true);
                    if (next == 2) hero.SetMoving(false);
                    if (next == 3) hero.Fire();
                }
                if (next == 3 && Time.time >= shotTime) { hero.Fire(); shotTime = Time.time + .65f; }
            }
            if (hero.Moving)
            {
                Vector3 p = hero.transform.position;
                p.x += (direction ? 1 : -1) * movementSpeed * Time.deltaTime;
                if (p.x > 2.8f) { p.x = 2.8f; direction = false; }
                if (p.x < -2.8f) { p.x = -2.8f; direction = true; }
                hero.transform.position = p;
                hero.SetFacingRight(direction);
            }
        }
        void OnGUI()
        {
            float scale = Mathf.Min(Screen.width / 1100f, Screen.height / 700f);
            GUI.matrix = Matrix4x4.Scale(new Vector3(scale, scale, 1));
            GUILayout.BeginArea(new Rect(20, 16, 1060, 180), GUI.skin.box);
            GUILayout.Label("TWO-HANDED HERO — modular weapon prototype");
            GUILayout.Label("Action: " + hero.Action + " | Weapon: " + (hero.Weapon != null ? hero.Weapon.name : "None") +
                " | Carry 8 frames / Run 16 / Stop 6 / Shoot 4");
            GUILayout.BeginHorizontal();
            autoCycle = GUILayout.Toggle(autoCycle, "Auto preview", GUILayout.Width(130));
            if (GUILayout.Button("Run")) { autoCycle = false; hero.SetMoving(true); }
            if (GUILayout.Button("Stop → Carry")) { autoCycle = false; hero.SetMoving(false); }
            if (GUILayout.Button("Fire (also while running)")) { autoCycle = false; hero.Fire(); }
            if (GUILayout.Button("Face left / right")) { direction = !direction; hero.SetFacingRight(direction); }
            foreach (var weapon in weapons) if (GUILayout.Button(weapon.name)) hero.Equip(weapon);
            GUILayout.EndHorizontal();
            GUILayout.Label("Travel speed (visual test only): " + movementSpeed.ToString("F1") + " units/sec");
            movementSpeed = GUILayout.HorizontalSlider(movementSpeed, 0, 4);
            GUILayout.Label("No input-system dependency. Button controls. Visual prototype; no damage or projectile simulation.");
            GUILayout.EndArea();
        }
    }
}
