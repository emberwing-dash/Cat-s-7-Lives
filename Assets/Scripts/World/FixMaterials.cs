using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace CatOnTheSill
{
    /// <summary>Makes sure imported materials use a URP-compatible shader (pink-material insurance) and repairs missing textures.</summary>
    public static class FixMaterials
    {
        public static void Apply(GameObject g)
        {
            foreach (var r in g.GetComponentsInChildren<Renderer>(true))
            {
                var mats = r.sharedMaterials;
                bool changed = false;
                for (int i = 0; i < mats.Length; i++)
                {
                    var m = mats[i];
                    if (m == null || m.shader == null) continue;
                    string sn = m.shader.name;
                    if (sn.StartsWith("Universal Render Pipeline") || sn.StartsWith("Shader Graphs") || sn.StartsWith("CatOnTheSill")) continue;
                    var nm = new Material(Mats.LitShader) { name = m.name };
                    Texture tex = m.HasProperty("_MainTex") ? m.GetTexture("_MainTex") : null;
                    if (tex != null) nm.SetTexture("_BaseMap", tex);
                    Color col = m.HasProperty("_Color") ? m.GetColor("_Color") : Color.white;
                    nm.SetColor("_BaseColor", col);
                    mats[i] = nm; changed = true;
                }
                if (changed) r.sharedMaterials = mats;
            }
        }

        /// <summary>Some character files shipped without their textures: give those materials sensible flat colours so nobody is a white ghost.</summary>
        public static void TintByName(GameObject g, Color? hair = null, Color? top = null)
        {
            foreach (var r in g.GetComponentsInChildren<Renderer>(true))
            {
                var mats = r.sharedMaterials;
                for (int i = 0; i < mats.Length; i++)
                {
                    var m = mats[i];
                    if (m == null || !m.HasProperty("_BaseMap")) continue;
                    if (m.GetTexture("_BaseMap") != null) continue;
                    m = new Material(m); mats[i] = m;
                    string n = m.name.ToLowerInvariant();
                    Color c = new Color(0.62f, 0.47f, 0.34f);
                    if (n.Contains("skin")) c = new Color(0.96f, 0.80f, 0.68f);
                    else if (n.Contains("hair")) c = hair ?? new Color(0.22f, 0.14f, 0.10f);
                    else if (n.Contains("iris")) c = new Color(0.30f, 0.50f, 0.75f);
                    else if (n.Contains("eyewhite") || n.Contains("highlight")) c = Color.white;
                    else if (n.Contains("mouth")) c = new Color(0.78f, 0.38f, 0.38f);
                    else if (n.Contains("brow") || n.Contains("lash") || n.Contains("eyeline")) c = new Color(0.12f, 0.08f, 0.06f);
                    else if (n.Contains("tie")) c = new Color(0.70f, 0.15f, 0.20f);
                    else if (n.Contains("shoe")) c = new Color(0.20f, 0.14f, 0.10f);
                    else if (n.Contains("bottom")) c = new Color(0.25f, 0.28f, 0.35f);
                    else if (n.Contains("top") || n.Contains("cloth")) c = top ?? new Color(0.35f, 0.55f, 0.70f);
                    m.SetColor("_BaseColor", c);
                    m.SetFloat("_Smoothness", 0.1f);
                    // these toon-style materials rely on a texture for emission too - without it they glow pure white
                    if (m.HasProperty("_EmissionColor")) m.SetColor("_EmissionColor", Color.black);
                    m.DisableKeyword("_EMISSION");
                }
                r.sharedMaterials = mats;
            }
        }

        static Texture2D Tex(string path) => Resources.Load<Texture2D>(path);

        /// <summary>The imported scenery had lost its textures: rebuild leaf / bark materials from the texture files that ship with the props.</summary>
        public static void Scenery(GameObject g, string prefabPath)
        {
            foreach (var r in g.GetComponentsInChildren<Renderer>(true))
            {
                var mats = r.sharedMaterials;
                for (int i = 0; i < mats.Length; i++)
                {
                    var src = mats[i]; if (src == null) continue;
                    string n = src.name.ToLowerInvariant();
                    Texture2D tex = null; bool leaf = false; Color tint = Color.white;
                    if (n.Contains("birch_bark")) { tex = Tex("Art/Props/Trees/Textures/Birch_Bark"); tint = new Color(0.95f, 0.93f, 0.9f); }
                    else if (n.Contains("birch_leaves")) { tex = Tex("Art/Props/Trees/Textures/Birch_Leaves_Green"); leaf = true; tint = new Color(0.62f, 0.85f, 0.35f); }
                    else if (n.Contains("pine_leaves")) { tex = Tex("Art/Props/Trees/Textures/Pine_Leaves"); leaf = true; tint = new Color(0.45f, 0.75f, 0.45f); }
                    else if (n.Contains("trunk") || n.Contains("bark")) { tex = Tex("Art/Props/textures/Bark_Albedo"); tint = new Color(0.8f, 0.7f, 0.6f); }
                    else if (n.Contains("leaves")) { tex = Tex(prefabPath.Contains("Fir") ? "Art/Props/textures/Fir_Branch" : "Art/Props/textures/BroadleafTree_Leaves"); leaf = true; tint = prefabPath.Contains("Fir") ? new Color(0.4f, 0.7f, 0.45f) : new Color(0.55f, 0.82f, 0.35f); }
                    else if (prefabPath.Contains("Bush")) { tex = Tex("Art/Props/textures/Bush_01_Branch"); leaf = true; tint = new Color(0.5f, 0.78f, 0.4f); }
                    else if (prefabPath.Contains("Flower")) { tex = Tex("Art/Props/textures/FlowerMeadow"); leaf = true; }
                    var nm = new Material(Mats.LitShader) { name = src.name };
                    if (tex != null) { nm.SetTexture("_BaseMap", tex); nm.mainTexture = tex; }
                    else tint = new Color(0.35f, 0.5f, 0.3f);
                    nm.SetColor("_BaseColor", tint);
                    nm.SetFloat("_Smoothness", 0.05f);
                    if (leaf)
                    {
                        nm.SetFloat("_AlphaClip", 1f); nm.SetFloat("_Cutoff", 0.45f); nm.EnableKeyword("_ALPHATEST_ON");
                        nm.SetFloat("_Cull", 0f); nm.renderQueue = 2450;
                    }
                    mats[i] = nm;
                }
                r.sharedMaterials = mats;
            }
        }
    }
}
