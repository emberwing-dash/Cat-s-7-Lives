using UnityEngine;

namespace CatOnTheSill
{
    /// <summary>A trail of glowing or shadow-dark paw prints that becomes visible under the intended light.</summary>
    public class ClueTrail : MonoBehaviour
    {
        public bool shadowTrail;
        public bool moonOnly;
        public Vector3 surfaceNormal = Vector3.up;
        public Color hiddenColor = new Color(0.25f, 0.16f, 0.08f, 0.12f);
        public Color lightColor = new Color(1f, 0.72f, 0.22f, 0.9f);
        public Color shadowColor = new Color(0.08f, 0.06f, 0.1f, 0.85f);

        Renderer[] marks;
        Material[] materials;
        float visibility;

        void Start()
        {
            marks = GetComponentsInChildren<Renderer>(true);
            materials = new Material[marks.Length];
            for (int i = 0; i < marks.Length; i++) materials[i] = marks[i].material;
        }

        void Update()
        {
            bool rightLight = !moonOnly || SunLight.IsMoon;
            bool lit = SunLight.IsLit(transform.position, surfaceNormal);
            bool reveal = rightLight && (shadowTrail ? !lit : lit);
            visibility = Mathf.MoveTowards(visibility, reveal ? 1f : 0.15f, Time.deltaTime * 2f);
            Color shown = shadowTrail ? shadowColor : lightColor;
            Color c = Color.Lerp(hiddenColor, shown, visibility);
            if (materials == null) return;
            foreach (var material in materials) if (material != null) material.SetColor("_Color", c);
        }
    }
}
