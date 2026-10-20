using UnityEngine;

namespace HeyHeyThere.PixelUISciFiFree
{
    /// <summary>
    /// One theme's prefabs by name ("Button", "Window", "Slider" ...), for code that builds UI or swaps
    /// themes; <see cref="iconColor"/> is what the theme tints the white icons with, and
    /// <see cref="GetColor"/> any colour of the theme by its "Type/name".
    /// </summary>
    public class PixelTheme : ScriptableObject
    {
        public string[] names;

        /// <summary>The theme's name as the kit calls it ("Mansion"), without its asset's type prefix.</summary>
        public string Title => name.StartsWith("") ? name.Substring("".Length) : name;
        public GameObject[] prefabs;
        public Color iconColor = Color.white;
        public string[] colorNames = { };
        public Color[] colors = { };

        public Color GetColor(string name, Color fallback)
        {
            int i = System.Array.IndexOf(colorNames, name);
            return i < 0 ? fallback : colors[i];
        }

        public GameObject Prefab(string name)
        {
            int i = System.Array.IndexOf(names, name);
            return i < 0 ? null : prefabs[i];
        }

        public bool Has(string name) => Prefab(name) != null;

        public GameObject Make(string name, Transform parent)
        {
            var prefab = Prefab(name);
            if (prefab == null)
                throw new System.ArgumentException($"{this.name} has no {name}");
            var go = Instantiate(prefab, parent, false);
            go.name = name;
            return go;
        }

        public T Make<T>(string name, Transform parent) where T : Component => Make(name, parent).GetComponent<T>();
    }
}
