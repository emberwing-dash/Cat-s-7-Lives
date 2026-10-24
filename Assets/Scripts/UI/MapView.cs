using UnityEngine;
using UnityEngine.UI;

namespace CatOnTheSill
{
    /// <summary>
    /// The house map (M): a floor plan of the five areas, the cat's position and heading, and one marker per soul
    /// fragment (hollow ring = still to solve, pulsing star = orb waiting, filled = collected).
    /// </summary>
    public class MapView : MonoBehaviour
    {
        const float S = 52f;              // pixels per metre
        static readonly Vector2 Centre = new Vector2(9f, 6f);

        struct Room { public string name; public float x, z, w, d; public Color color; }

        static readonly Room[] Rooms =
        {
            new Room { name = "Bedroom",     x = 3,  z = 3,  w = 6, d = 6, color = new Color(0.74f, 0.48f, 0.42f) },
            new Room { name = "Study",       x = 3,  z = 9,  w = 6, d = 6, color = new Color(0.40f, 0.55f, 0.52f) },
            new Room { name = "Living Room", x = 9,  z = 4,  w = 6, d = 8, color = new Color(0.78f, 0.58f, 0.40f) },
            new Room { name = "Library",        x = 9,  z = 10, w = 6, d = 4, color = new Color(0.55f, 0.50f, 0.62f) },
            new Room { name = "Kitchen",     x = 15, z = 3,  w = 6, d = 6, color = new Color(0.78f, 0.72f, 0.45f) },
            new Room { name = "Sunroom",     x = 15, z = 9,  w = 6, d = 6, color = new Color(0.50f, 0.70f, 0.48f) },
        };

        Font font;
        RectTransform map, catMarker;
        readonly System.Collections.Generic.List<(Puzzle p, RectTransform rt, Image img)> marks = new System.Collections.Generic.List<(Puzzle, RectTransform, Image)>();

        public static MapView Create(RectTransform parent, Font f)
        {
            var go = new GameObject("MapView", typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var rt = (RectTransform)go.transform;
            rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = new Vector2(0, 10); rt.sizeDelta = new Vector2(18 * S, 12 * S);
            var v = go.AddComponent<MapView>();
            v.font = f; v.map = rt; v.Build();
            return v;
        }

        static Vector2 ToMap(float wx, float wz) => new Vector2((wx - Centre.x) * S, (wz - Centre.y) * S);

        RectTransform Make(string n, Vector2 pos, Vector2 size, Sprite sp, Color c, bool sliced = false)
        {
            var go = new GameObject(n, typeof(RectTransform));
            go.transform.SetParent(map, false);
            var rt = (RectTransform)go.transform;
            rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = pos; rt.sizeDelta = size;
            if (sp != null)
            {
                var im = go.AddComponent<Image>(); im.sprite = sp; im.color = c; im.raycastTarget = false;
                if (sliced) im.type = Image.Type.Sliced;
            }
            return rt;
        }

        void Label(RectTransform parent, string s, int size, Color c, TextAnchor a, Vector2 offsetMin, Vector2 offsetMax)
        {
            var go = new GameObject("T", typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var rt = (RectTransform)go.transform;
            rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one; rt.offsetMin = offsetMin; rt.offsetMax = offsetMax;
            var t = go.AddComponent<Text>();
            t.font = font; t.text = s; t.fontSize = size; t.color = c; t.alignment = a; t.fontStyle = FontStyle.Bold; t.raycastTarget = false;
            t.horizontalOverflow = HorizontalWrapMode.Overflow; t.verticalOverflow = VerticalWrapMode.Overflow;
        }

        void Build()
        {
            foreach (var r in Rooms)
            {
                var rt = Make(r.name, ToMap(r.x, r.z), new Vector2(r.w * S - 6, r.d * S - 6), Mats.RoundedSprite(), new Color(r.color.r, r.color.g, r.color.b, 0.9f), true);
                Label(rt, r.name.ToUpperInvariant(), 22, new Color(0.2f, 0.1f, 0.05f, 0.85f), TextAnchor.UpperLeft, new Vector2(12, 8), new Vector2(-8, -10));
            }
            // the attic is a loft above the north end of the sunroom
            var attic = Make("Attic", ToMap(15f, 10.7f), new Vector2(6f * S - 40, 2.6f * S - 16), Mats.RoundedSprite(), new Color(0.22f, 0.2f, 0.4f, 0.78f), true);
            Label(attic, "ATTIC  (upstairs)", 20, new Color(0.85f, 0.88f, 1f, 0.95f), TextAnchor.LowerRight, new Vector2(8, 6), new Vector2(-12, -6));

            catMarker = Make("Cat", Vector2.zero, new Vector2(46, 46), UIManager.spriteCache(GlyphKind.Arrow), new Color(0.2f, 0.08f, 0.02f));
            var you = Make("You", Vector2.zero, new Vector2(120, 30), null, Color.white);
            you.SetParent(catMarker, false); you.anchoredPosition = new Vector2(0, -34);
            Label(you, "you", 22, new Color(1f, 0.9f, 0.6f), TextAnchor.MiddleCenter, Vector2.zero, Vector2.zero);
            var halo = Make("Halo", Vector2.zero, new Vector2(70, 70), Mats.SoftSprite(), new Color(1f, 0.7f, 0.3f, 0.8f));
            halo.SetParent(catMarker, false); halo.anchoredPosition = Vector2.zero; halo.SetAsFirstSibling();
        }

        void OnEnable() { Refresh(); }
        void Update() { Refresh(); }

        void EnsureMarks()
        {
            if (marks.Count > 0) return;
            foreach (var p in Puzzle.All)
            {
                if (p == null) continue;
                var rt = Make("Fragment" + p.fragmentIndex, Vector2.zero, new Vector2(46, 46), UIManager.spriteCache(GlyphKind.Ring), Color.white);
                marks.Add((p, rt, rt.GetComponent<Image>()));
            }
        }

        public void Refresh()
        {
            if (map == null) return;
            EnsureMarks();
            var cat = CatController.I;
            if (cat != null)
            {
                Vector3 p = cat.transform.position;
                catMarker.anchoredPosition = ToMap(p.x, p.z);
                catMarker.localRotation = Quaternion.Euler(0, 0, -cat.transform.eulerAngles.y);
                var yaw = catMarker.Find("You"); if (yaw != null) yaw.localRotation = Quaternion.Inverse(catMarker.localRotation);
                catMarker.SetAsLastSibling();
            }
            float pulse = 0.5f + 0.5f * Mathf.Sin(Time.unscaledTime * 4f);
            foreach (var (p, rt, img) in marks)
            {
                if (p == null) continue;
                Vector3 w = p.revealPos;
                rt.anchoredPosition = ToMap(w.x, w.z);
                Color c = FragmentInfo.Colors[Mathf.Clamp(p.fragmentIndex, 0, 6)];
                if (Game.Has(p.fragmentIndex))
                {
                    img.sprite = UIManager.spriteCache(GlyphKind.Star); img.color = c; rt.localScale = Vector3.one * 0.8f;
                }
                else if (p.Solved)
                {
                    img.sprite = UIManager.spriteCache(GlyphKind.Star); img.color = Color.Lerp(c, Color.white, pulse); rt.localScale = Vector3.one * (1f + 0.25f * pulse);
                }
                else
                {
                    img.sprite = UIManager.spriteCache(GlyphKind.Ring); img.color = new Color(0.15f, 0.08f, 0.04f, 0.9f); rt.localScale = Vector3.one;
                }
            }
        }
    }
}
