using System.Collections.Generic;
using UnityEngine;

namespace CatOnTheSill
{
    public class WindowCover : MonoBehaviour, IPersist
    {
        public string id = "cover";
        public CoverKind kind;
        public Vector3 origin, right, up, inward;     // window centre on the inner wall face, unit axes
        public float width, height;
        public int state;
        public int[] cycle = { 2, 0 };                // order the cord steps through (0 closed, 1 gap, 2 open)
        public float gapLeft = -0.2f, gapRight = 0.2f;    // gap extents (metres from window centre along 'right')
        public Material fabric;
        public Color color = new Color(0.82f, 0.45f, 0.40f);

        [SerializeField] Transform panelL, panelR;
        float curA, curB;     // current inner edges of panels (curtain) or blind height
        float padSpan;
        float bunch;
        float top, bottom;
        float depth = 0.07f;
        bool built => panelL != null;

        public string PersistId => "cover:" + id;
        public bool IsOpenEnough => state != 0;

        void OnEnable() { Game.Register(this); }
        void OnDisable() { Game.Unregister(this); }

        void Setup()
        {
            top = height * 0.5f + 0.13f; bottom = -height * 0.5f - (kind == CoverKind.Blind ? 0.02f : 0.32f);
            padSpan = kind == CoverKind.Curtains ? 0.28f : 0.04f;
            bunch = kind == CoverKind.Curtains ? 0.34f : 0.14f;
        }

        void Awake() { Setup(); if (built) SetState(state, true); }

        /// <summary>Creates the panels and rod (done once by the scene baker).</summary>
        public void Build()
        {
            if (built) return;
            Setup();
            var mat = fabric != null ? fabric : Mats.Textured(Mats.Pattern(kind == CoverKind.Shutters ? "wood" : "stripes", color, color * 0.82f, 128), new Vector2(kind == CoverKind.Blind ? 1 : 3, 1), Color.white, 0.1f);
            if (kind == CoverKind.Shutters) mat = Mats.Lit(new Color(0.42f, 0.28f, 0.17f), 0.25f);
            if (kind == CoverKind.Blind) mat = Mats.Textured(Mats.Pattern("stripes", new Color(0.93f, 0.9f, 0.82f), new Color(0.84f, 0.8f, 0.7f), 128), new Vector2(1, 14), Color.white, 0.1f);
            panelL = MakePanel("PanelL", mat);
            if (kind != CoverKind.Blind) panelR = MakePanel("PanelR", mat);
            // rod
            var rodMat = Mats.Metal(new Color(0.6f, 0.5f, 0.35f), 0.6f);
            var rod = B.Cyl(transform, "Rod", origin + up * (top + 0.04f) + inward * 0.1f, 0.018f, width + padSpan * 2f + 0.1f, rodMat, Col.None);
            rod.transform.rotation = Quaternion.FromToRotation(Vector3.up, right);
            rod.GetComponent<Renderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            SetState(state, true);
        }

        Transform MakePanel(string n, Material mat)
        {
            var g = B.Box(transform, n, origin, Vector3.one, mat, true, true);
            g.layer = Layer.Curtain;
            var col = g.GetComponent<BoxCollider>(); col.isTrigger = false;
            return g.transform;
        }

        (float, float) Targets(int s)
        {
            float hw = width * 0.5f;
            if (kind == CoverKind.Blind) return (s == 0 ? 1f : 0.12f, 0);          // blind: fraction of window covered
            if (s == 0) return (0.015f, -0.015f);                                    // curtains meet in the middle
            if (s == 1) return (gapLeft, gapRight);
            return (-hw + (bunch - padSpan), hw - (bunch - padSpan));                 // bunched at the edges
        }

        public void SetState(int s, bool instant)
        {
            state = Mathf.Clamp(s, 0, 2);
            if (!built) return;
            var (a, b) = Targets(state);
            tgtA = a; tgtB = b;
            if (instant) { curA = a; curB = b; Layout(); }
            WorldState.Dirty();
        }

        float tgtA, tgtB;

        public void Cycle()
        {
            // step through the cord's cycle, based on the current state
            int idx = System.Array.IndexOf(cycle, state);
            int next = cycle[(idx + 1) % cycle.Length];
            if (idx < 0) next = cycle[0];
            SetState(next, false);
            AudioManager.I?.Play("curtain", origin, 0.7f, 1f);
            Game.Save();
        }

        void Update()
        {
            if (!built) return;
            bool moving = false;
            float speed = kind == CoverKind.Blind ? 0.9f : 1.3f;
            if (Mathf.Abs(curA - tgtA) > 0.002f) { curA = Mathf.MoveTowards(curA, tgtA, speed * Time.deltaTime); moving = true; }
            if (Mathf.Abs(curB - tgtB) > 0.002f) { curB = Mathf.MoveTowards(curB, tgtB, speed * Time.deltaTime); moving = true; }
            if (moving) { Layout(); Physics.SyncTransforms(); WorldState.Dirty(); }
        }

        void Layout()
        {
            float hw = width * 0.5f;
            float h = top - bottom;
            if (kind == CoverKind.Blind)
            {
                float cover = Mathf.Clamp01(curA);
                float blindH = Mathf.Lerp(0.14f, h, cover);
                Place(panelL, 0f, top - blindH * 0.5f, width + 0.1f, blindH, depth * 0.6f);
                return;
            }
            float leftEdge = -hw - padSpan, rightEdge = hw + padSpan;
            float lw = Mathf.Max(0.04f, curA - leftEdge), rw = Mathf.Max(0.04f, rightEdge - curB);
            Place(panelL, leftEdge + lw * 0.5f, (top + bottom) * 0.5f, lw, h, kind == CoverKind.Shutters ? 0.05f : depth);
            Place(panelR, rightEdge - rw * 0.5f, (top + bottom) * 0.5f, rw, h, kind == CoverKind.Shutters ? 0.05f : depth);
        }

        void Place(Transform t, float u, float v, float w, float h, float d)
        {
            t.position = origin + right * u + up * v + inward * (kind == CoverKind.Shutters ? 0.06f : 0.11f);
            t.rotation = Quaternion.LookRotation(inward, up);   // local +X = right when right = up x inward
            t.localScale = new Vector3(w, h, d);
        }

        public string Capture() => state.ToString();
        public void Restore(string s) { if (int.TryParse(s, out int i)) SetState(i, true); }
    }
}
