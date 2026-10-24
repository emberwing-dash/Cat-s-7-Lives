using UnityEngine;

namespace CatOnTheSill
{
    public partial class WorldBuilder
    {
        void AddLightClue(string name, Vector3 position, Vector3 normal, string message, bool moonOnly = false, bool revealInShadow = false, float size = 0.16f)
        {
            return;   // wall-text clues disabled: hints are pictorial now (remove this line to bring them back)
#pragma warning disable CS0162
            var go = new GameObject(name);
            go.transform.SetParent(root, false);
            go.transform.position = position;
            var clue = go.AddComponent<LightClueMessage>();
            clue.message = message;
            clue.surfaceNormal = normal;
            clue.moonOnly = moonOnly;
            clue.sunOnly = !moonOnly;
            clue.revealInShadow = revealInShadow;
            clue.fontSize = size;
#pragma warning restore CS0162
        }

        void AddPawTrail(string name, Vector3 from, Vector3 to, int count, bool shadowTrail = false, bool moonOnly = false)
        {
            var trail = new GameObject(name);
            trail.transform.SetParent(root, false);
            trail.transform.position = Vector3.Lerp(from, to, 0.5f);
            var clue = trail.AddComponent<ClueTrail>();
            clue.shadowTrail = shadowTrail;
            clue.moonOnly = moonOnly;

            var mat = Mats.Blend(shadowTrail ? new Color(0.08f, 0.06f, 0.1f, 0.15f) : new Color(1f, 0.72f, 0.22f, 0.15f), Mats.Glyph(GlyphKind.Paw), shadowTrail ? Mats.BlendKind.Alpha : Mats.BlendKind.Additive);
            Vector3 direction = to - from;
            for (int i = 0; i < count; i++)
            {
                float t = count == 1 ? 0.5f : i / (float)(count - 1);
                Vector3 p = Vector3.Lerp(from, to, t);
                p.x += (i % 2 == 0 ? -0.12f : 0.12f);
                var mark = B.Quad(trail.transform, "Paw" + i, p - trail.transform.position + Vector3.up * 0.018f, Vector3.up, new Vector2(0.28f, 0.28f), mat);
                mark.transform.Rotate(Vector3.up, -Mathf.Atan2(direction.z, direction.x) * Mathf.Rad2Deg + 90f, Space.World);
            }
        }

    }
}

