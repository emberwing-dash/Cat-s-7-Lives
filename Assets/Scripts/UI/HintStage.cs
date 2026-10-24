using UnityEngine;
using UnityEngine.UI;

namespace CatOnTheSill
{
    /// <summary>
    /// The picture-hint "GIF": a tiny looping cartoon of the little cat doing the current step to the actual kind of
    /// object (push it to the glowing ring, pull the cord, turn the mirror, pick the hour). All built from UI images.
    /// </summary>
    public class HintStage : MonoBehaviour
    {
        static readonly Color Orange = new Color(0.98f, 0.66f, 0.33f);
        static readonly Color Ink = new Color(0.30f, 0.15f, 0.08f);
        static readonly Color Cream = new Color(1f, 0.93f, 0.78f);
        static readonly Color Gold = new Color(1f, 0.85f, 0.35f);

        const float Loop = 5.4f;
        const float Floor = -120f;

        RectTransform root;
        Font font;
        float t;
        StepKind kind; bool night; string hourName = "";

        // cat
        RectTransform cat, tail, keyBubble;
        // push
        RectTransform obj, goal, bottle; Image objImg, goalImg, sparkle;
        // window (cord + hour)
        RectTransform win, blind, cord, tassel, celestial; Image glass, celestialImg; Text hourLabel;
        // mirror
        RectTransform mirror, sun, beam1, beam2, target; Image targetImg, beam2Img;
        // wait
        RectTransform eye;

        public static HintStage Create(RectTransform parent, Font f)
        {
            var go = new GameObject("HintStage", typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var rt = (RectTransform)go.transform;
            rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = new Vector2(0, 40); rt.sizeDelta = new Vector2(980, 400);
            var s = go.AddComponent<HintStage>();
            s.font = f; s.root = rt; s.Build();
            return s;
        }

        // ------------------------------------------------------------------------------------------ construction
        RectTransform Make(string n, Transform parent, Vector2 pos, Vector2 size, Sprite sp, Color c, bool sliced = false, Vector2? pivot = null)
        {
            var go = new GameObject(n, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var rt = (RectTransform)go.transform;
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = pivot ?? new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = pos; rt.sizeDelta = size;
            if (sp != null)
            {
                var im = go.AddComponent<Image>(); im.sprite = sp; im.color = c; im.raycastTarget = false;
                if (sliced) im.type = Image.Type.Sliced;
            }
            return rt;
        }

        static Image Im(RectTransform r) => r.GetComponent<Image>();
        static Sprite G(GlyphKind k) => UIManager.spriteCache(k);

        void Build()
        {
            var bg = Make("Bg", root, Vector2.zero, root.sizeDelta, Mats.RoundedSprite(), new Color(0.1f, 0.07f, 0.12f, 0.92f), true);
            Make("Floor", root, new Vector2(0, Floor - 36), new Vector2(940, 12), Mats.RoundedSprite(), new Color(0.45f, 0.28f, 0.16f), true);

            // ---- push props
            goal = Make("Goal", root, new Vector2(230, Floor + 40), new Vector2(120, 120), G(GlyphKind.Ring), Gold);
            goalImg = Im(goal);
            obj = Make("Obj", root, new Vector2(-150, Floor + 36), new Vector2(92, 92), G(GlyphKind.Square), Orange);
            objImg = Im(obj);
            bottle = Make("Bottle", obj, Vector2.zero, new Vector2(10, 10), null, Color.white);
            Make("BBody", bottle, new Vector2(0, -8), new Vector2(46, 62), Mats.RoundedSprite(), new Color(0.96f, 0.97f, 1f), true);
            Make("BNeck", bottle, new Vector2(0, 34), new Vector2(24, 26), Mats.RoundedSprite(), new Color(0.96f, 0.97f, 1f), true);
            Make("BCap", bottle, new Vector2(0, 50), new Vector2(28, 10), Mats.RoundedSprite(), new Color(0.85f, 0.6f, 0.25f), true);
            Make("BLabel", bottle, new Vector2(0, -8), new Vector2(34, 22), Mats.RoundedSprite(), new Color(0.55f, 0.75f, 0.95f), true);
            sparkle = Im(Make("Sparkle", root, new Vector2(230, Floor + 40), new Vector2(150, 150), G(GlyphKind.Star), Gold));

            // ---- window (cord + hour)
            win = Make("Win", root, new Vector2(120, 40), new Vector2(210, 230), null, Color.white);
            Make("Frame", win, Vector2.zero, new Vector2(226, 246), Mats.RoundedSprite(), new Color(0.55f, 0.36f, 0.2f), true);
            glass = Im(Make("Glass", win, Vector2.zero, new Vector2(198, 218), Mats.RoundedSprite(), new Color(0.55f, 0.75f, 0.95f), true));
            celestial = Make("Celestial", win, new Vector2(0, 0), new Vector2(110, 110), G(GlyphKind.Sun), Gold);
            celestialImg = Im(celestial);
            blind = Make("Blind", win, new Vector2(0, 109), new Vector2(198, 218), Mats.RoundedSprite(), new Color(0.93f, 0.9f, 0.82f), true, new Vector2(0.5f, 1f));
            cord = Make("Cord", root, new Vector2(262, 70), new Vector2(5, 150), Mats.RoundedSprite(), new Color(0.9f, 0.8f, 0.6f), true, new Vector2(0.5f, 1f));
            tassel = Make("Tassel", cord, new Vector2(0, -75), new Vector2(26, 38), Mats.RoundedSprite(), new Color(0.85f, 0.45f, 0.4f), true);
            var hl = Make("HourLabel", root, new Vector2(120, -98), new Vector2(360, 50), null, Color.white);
            hourLabel = hl.gameObject.AddComponent<Text>();
            hourLabel.font = font; hourLabel.fontSize = 34; hourLabel.fontStyle = FontStyle.Bold; hourLabel.alignment = TextAnchor.MiddleCenter;
            hourLabel.color = Cream; hourLabel.raycastTarget = false;

            // ---- mirror puzzle
            sun = Make("SunSrc", root, new Vector2(-400, 0), new Vector2(110, 110), G(GlyphKind.Sun), Gold);
            beam1 = Make("Beam1", root, new Vector2(-350, 0), new Vector2(450, 10), Mats.RoundedSprite(), new Color(1f, 0.9f, 0.4f, 0.9f), true, new Vector2(0, 0.5f));
            mirror = Make("Mirror", root, new Vector2(100, 0), new Vector2(18, 120), Mats.RoundedSprite(), new Color(0.75f, 0.9f, 1f), true);
            beam2 = Make("Beam2", root, new Vector2(100, 0), new Vector2(170, 10), Mats.RoundedSprite(), new Color(1f, 0.9f, 0.4f, 0.9f), true, new Vector2(0, 0.5f));
            beam2Img = Im(beam2);
            target = Make("Target", root, new Vector2(100, 170), new Vector2(86, 86), G(GlyphKind.Star), new Color(0.6f, 0.6f, 0.65f));
            targetImg = Im(target);

            // ---- wait
            eye = Make("Eye", root, new Vector2(200, 30), new Vector2(150, 150), G(GlyphKind.Eye), Orange);

            // ---- the cat (faces right)
            cat = Make("Cat", root, new Vector2(-300, Floor + 34), new Vector2(10, 10), null, Color.white);
            tail = Make("Tail", cat, new Vector2(-62, -6), new Vector2(14, 64), Mats.RoundedSprite(), Orange, true, new Vector2(0.5f, 0f));
            Make("Body", cat, Vector2.zero, new Vector2(132, 64), Mats.RoundedSprite(), Orange, true);
            Make("Stripe1", cat, new Vector2(-18, 14), new Vector2(10, 30), Mats.RoundedSprite(), new Color(0.85f, 0.5f, 0.2f), true);
            Make("Stripe2", cat, new Vector2(4, 14), new Vector2(10, 30), Mats.RoundedSprite(), new Color(0.85f, 0.5f, 0.2f), true);
            Make("LegBack", cat, new Vector2(-34, -34), new Vector2(22, 26), Mats.RoundedSprite(), Orange, true);
            Make("LegFront", cat, new Vector2(38, -34), new Vector2(22, 26), Mats.RoundedSprite(), Orange, true);
            Make("EarL", cat, new Vector2(54, 62), new Vector2(26, 30), G(GlyphKind.Ear), Orange);
            Make("EarR", cat, new Vector2(88, 62), new Vector2(26, 30), G(GlyphKind.Ear), Orange);
            Make("Head", cat, new Vector2(70, 34), new Vector2(66, 56), Mats.RoundedSprite(), Orange, true);
            Make("EyeA", cat, new Vector2(58, 40), new Vector2(9, 11), Mats.RoundedSprite(), Ink, true);
            Make("EyeB", cat, new Vector2(84, 40), new Vector2(9, 11), Mats.RoundedSprite(), Ink, true);
            Make("Nose", cat, new Vector2(72, 28), new Vector2(10, 7), Mats.RoundedSprite(), new Color(0.95f, 0.5f, 0.55f), true);

            keyBubble = Make("Key", root, Vector2.zero, new Vector2(70, 70), null, Color.white);
            Make("Sh", keyBubble, new Vector2(0, -5), new Vector2(70, 70), Mats.RoundedSprite(), new Color(0.45f, 0.25f, 0.12f), true);
            Make("Top", keyBubble, Vector2.zero, new Vector2(70, 70), Mats.RoundedSprite(), Cream, true);
            var kt = Make("L", keyBubble, Vector2.zero, new Vector2(70, 70), null, Color.white);
            var txt = kt.gameObject.AddComponent<Text>();
            txt.font = font; txt.text = "E"; txt.fontSize = 42; txt.fontStyle = FontStyle.Bold; txt.alignment = TextAnchor.MiddleCenter; txt.color = Ink; txt.raycastTarget = false;
        }

        // ------------------------------------------------------------------------------------------ playback
        public void Play(StepKind k, GlyphKind objGlyph, bool isNight, string hour)
        {
            kind = k; night = isNight; hourName = hour ?? ""; t = 0f;
            objImg.sprite = G(objGlyph);
            bool isBottle = objGlyph == GlyphKind.Triangle;
            objImg.enabled = !isBottle; bottle.gameObject.SetActive(isBottle);
            foreach (Transform c in root) c.gameObject.SetActive(true);
            bool push = k == StepKind.Push, cordK = k == StepKind.Cord, turn = k == StepKind.Turn, hourK = k == StepKind.Hour;
            goal.gameObject.SetActive(push); obj.gameObject.SetActive(push); sparkle.gameObject.SetActive(push);
            win.gameObject.SetActive(cordK || hourK); cord.gameObject.SetActive(cordK); hourLabel.gameObject.SetActive(hourK);
            sun.gameObject.SetActive(turn); beam1.gameObject.SetActive(turn); mirror.gameObject.SetActive(turn); beam2.gameObject.SetActive(turn); target.gameObject.SetActive(turn);
            eye.gameObject.SetActive(k == StepKind.Wait);
            blind.gameObject.SetActive(cordK);
            Tick(0f);
        }

        void Update() { Tick(Time.unscaledDeltaTime); }

        static float Ease(float x) { x = Mathf.Clamp01(x); return x * x * (3f - 2f * x); }
        static float Seg(float t, float a, float b) => Mathf.Clamp01((t - a) / (b - a));

        void Tick(float dt)
        {
            t = (t + dt) % Loop;
            switch (kind)
            {
                case StepKind.Push: DoPush(); break;
                case StepKind.Cord: DoCord(); break;
                case StepKind.Turn: DoTurn(); break;
                case StepKind.Hour: DoHour(); break;
                default: DoWait(); break;
            }
        }

        void SetCat(float x, float y = 0f, float tilt = 0f, float tailSpeed = 3f)
        {
            cat.anchoredPosition = new Vector2(x, Floor + 34 + y);
            cat.localRotation = Quaternion.Euler(0, 0, tilt);
            tail.localRotation = Quaternion.Euler(0, 0, 20f + Mathf.Sin(Time.unscaledTime * tailSpeed) * 22f);
        }

        void Key(bool show, Vector2 pos, float pop)
        {
            keyBubble.gameObject.SetActive(show);
            if (!show) return;
            keyBubble.anchoredPosition = pos + new Vector2(0, Mathf.Sin(Time.unscaledTime * 8f) * 4f);
            keyBubble.localScale = Vector3.one * (0.9f + pop * 0.25f);
        }

        // push the object onto the glowing ring
        void DoPush()
        {
            float walk = Ease(Seg(t, 0f, 1.1f));
            float push = Ease(Seg(t, 1.2f, 3.5f));
            float ox = Mathf.Lerp(-150f, 230f, push);
            float cx = t < 1.2f ? Mathf.Lerp(-420f, -260f, walk) : ox - 108f;
            bool moving = t > 1.2f && t < 3.5f;
            SetCat(cx, moving ? Mathf.Abs(Mathf.Sin(t * 9f)) * 6f : 0f, 0f, moving ? 8f : 3f);
            obj.anchoredPosition = new Vector2(ox, Floor + 36);
            float pulse = 0.5f + 0.5f * Mathf.Sin(Time.unscaledTime * 4f);
            goalImg.color = new Color(1f, 0.85f, 0.35f, 0.55f + 0.45f * pulse);
            goal.localScale = Vector3.one * (1f + 0.06f * pulse);
            bool done = t > 3.5f;
            sparkle.gameObject.SetActive(done);
            if (done) { float s = Seg(t, 3.5f, 4.4f); sparkle.rectTransform.localScale = Vector3.one * (0.5f + s); sparkle.color = new Color(1f, 0.9f, 0.4f, 1f - s * 0.6f); sparkle.rectTransform.localRotation = Quaternion.Euler(0, 0, t * 90f); }
            Key(false, Vector2.zero, 0);
        }

        // pull the cord, the blind lifts, the room fills with light
        void DoCord()
        {
            float walk = Ease(Seg(t, 0f, 1.6f));
            float reach = Ease(Seg(t, 1.7f, 2.1f));
            float pull = Mathf.Sin(Mathf.Clamp01(Seg(t, 2.2f, 2.9f)) * Mathf.PI);
            float lift = Ease(Seg(t, 2.5f, 3.9f));
            float cx = Mathf.Lerp(-420f, 150f, walk);
            SetCat(cx, reach * 30f, reach * 24f, 3f);
            cord.sizeDelta = new Vector2(5, 150 + pull * 36f);
            tassel.anchoredPosition = new Vector2(0, -cord.sizeDelta.y * 0.5f);
            blind.sizeDelta = new Vector2(198, Mathf.Lerp(218f, 8f, lift));
            glass.color = Color.Lerp(new Color(0.25f, 0.3f, 0.4f), new Color(0.6f, 0.82f, 1f), lift);
            celestial.gameObject.SetActive(true);
            celestialImg.sprite = G(GlyphKind.Sun);
            celestialImg.color = new Color(1f, 0.85f, 0.35f, lift);
            Key(t > 1.8f && t < 3.2f, new Vector2(cx + 70, Floor + 150), pull);
        }

        // press E until the mirror throws the beam onto the star
        void DoTurn()
        {
            float walk = Ease(Seg(t, 0f, 1.2f));
            float cx = Mathf.Lerp(-420f, -20f, walk);
            float step = Mathf.Floor(Seg(t, 1.6f, 3.6f) * 3.999f);          // 0..3 presses
            float into = Seg(t, 1.6f, 3.6f) * 4f - step;
            float ang = -(step + Ease(Mathf.Clamp01(into * 3f))) * 15f;      // 0 -> -45 deg
            if (t < 1.6f) ang = 0f; else if (t > 3.6f) ang = -45f;
            SetCat(cx, 0f, 0f, 3f);
            mirror.localRotation = Quaternion.Euler(0, 0, ang);
            float rad = ang * Mathf.Deg2Rad;
            Vector2 r = new Vector2(-Mathf.Cos(2f * rad), -Mathf.Sin(2f * rad));
            beam2.localRotation = Quaternion.Euler(0, 0, Mathf.Atan2(r.y, r.x) * Mathf.Rad2Deg);
            bool hit = Mathf.Abs(ang + 45f) < 1.5f;
            beam2Img.color = new Color(1f, 0.9f, 0.4f, 0.9f);
            targetImg.color = hit ? new Color(1f, 0.9f, 0.4f) : new Color(0.55f, 0.55f, 0.62f);
            target.localScale = Vector3.one * (hit ? 1.15f + 0.1f * Mathf.Sin(Time.unscaledTime * 8f) : 1f);
            bool pressing = t > 1.6f && t < 3.6f && into < 0.45f;
            Key(t > 1.3f && t < 3.9f, new Vector2(cx + 70, Floor + 150), pressing ? 1f : 0f);
        }

        // paw the window and pick the hour
        void DoHour()
        {
            float walk = Ease(Seg(t, 0f, 1.4f));
            float reach = Ease(Seg(t, 1.5f, 1.9f));
            float cx = Mathf.Lerp(-420f, -70f, walk);
            SetCat(cx, reach * 30f, reach * 24f, 3f);
            float change = Ease(Seg(t, 2.4f, 3.4f));
            Color dayGlass = new Color(0.6f, 0.82f, 1f), nightGlass = new Color(0.08f, 0.1f, 0.25f);
            Color from = night ? dayGlass : nightGlass, to = night ? nightGlass : dayGlass;
            glass.color = Color.Lerp(from, to, change);
            celestialImg.sprite = G((change < 0.5f ? !night : night) ? GlyphKind.Sun : GlyphKind.Moon);
            celestialImg.color = (change < 0.5f ? !night : night) ? new Color(1f, 0.85f, 0.35f) : new Color(0.75f, 0.88f, 1f);
            celestial.localScale = Vector3.one * (1f + Mathf.Sin(change * Mathf.PI) * 0.0f);
            hourLabel.text = hourName.ToUpperInvariant();
            hourLabel.color = change > 0.5f ? Gold : Cream;
            Key(t > 1.6f && t < 2.9f, new Vector2(cx + 70, Floor + 150), Seg(t, 1.6f, 2.2f));
        }

        // nothing to do right now: the cat sits and watches the light
        void DoWait()
        {
            float walk = Ease(Seg(t, 0f, 1.2f));
            SetCat(Mathf.Lerp(-420f, -120f, walk), 0f, 0f, 2f);
            float blink = Mathf.Abs(Mathf.Sin(t * 1.3f));
            eye.localScale = new Vector3(1f, Mathf.Lerp(0.15f, 1f, Mathf.Clamp01(blink * 3f)), 1f);
            Key(false, Vector2.zero, 0);
        }
    }
}
