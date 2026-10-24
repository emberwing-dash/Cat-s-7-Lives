using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;

namespace CatOnTheSill.EditorTools
{
    /// <summary>
    /// Builds the two real scenes - Assets/Scenes/MainMenu.unity and Assets/Scenes/Game.unity - with the whole house,
    /// the cat and the people placed in the hierarchy, so everything can be previewed before pressing Play.
    /// Run it from the menu: Cat On The Sill / Bake Scenes.
    /// </summary>
    public static class SceneBaker
    {
        const string SceneDir = "Assets/Scenes";
        const string BakedDir = "Assets/Scenes/Baked";

        [MenuItem("Cat On The Sill/Bake All (Menu + Game + Cat)")]
        public static void BakeAll()
        {
            Directory.CreateDirectory(SceneDir);
            WorldBuilder.Spawn = (prefab, parent) => (GameObject)PrefabUtility.InstantiatePrefab(prefab, parent);
            BakeGame();
            BakeMenu();
            EditorBuildSettings.scenes = new[]
            {
                new EditorBuildSettingsScene(SceneDir + "/MainMenu.unity", true),
                new EditorBuildSettingsScene(SceneDir + "/Game.unity", true)
            };
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("Scenes baked: MainMenu + Game");
        }

        // separate entry points: creating a second scene inside one editor session can crash in batch mode
        public static void BakeGameOnly() { Prep(); BakeGame(); Settings(); }
        public static void BakeMenuOnly() { Prep(); BakeMenu(); Settings(); }
        static void Prep() { Directory.CreateDirectory(SceneDir); WorldBuilder.Spawn = (prefab, parent) => (GameObject)PrefabUtility.InstantiatePrefab(prefab, parent); }
        static void Settings()
        {
            EditorBuildSettings.scenes = new[]
            {
                new EditorBuildSettingsScene(SceneDir + "/MainMenu.unity", true),
                new EditorBuildSettingsScene(SceneDir + "/Game.unity", true)
            };
            AssetDatabase.SaveAssets();
        }

        // =================================================================================================== game
        static void BakeGame()
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            Mats.Clear();

            var gm = new GameObject("GameManager").AddComponent<GameManager>();
            var sun = MakeSun();
            var tmGo = new GameObject("TimeManager");
            var tm = tmGo.AddComponent<TimeManager>();

            var wb = new WorldBuilder();
            wb.Build(null);
            var worldRoot = wb.root;
            var refs = worldRoot.GetComponent<WorldRefs>();

            var catGo = new GameObject("Cat");
            catGo.transform.position = wb.CatStart;
            catGo.AddComponent<CharacterController>();
            var cat = catGo.AddComponent<CatController>();
            cat.Setup();
            var inter = catGo.AddComponent<InteractionSystem>(); inter.cat = cat;
            var hints = catGo.AddComponent<HintSystem>();

            var rigGo = new GameObject("CameraRig");
            var rig = rigGo.AddComponent<CameraRig>();
            rig.EnsureCamera();
            rig.cam.GetUniversalAdditionalCameraData();
            rig.orbitLookOffset = Vector3.zero;
            rig.target = cat.transform;

            new GameObject("Beams").AddComponent<BeamSystem>();

            gm.World = refs; gm.Rig = rig; gm.Cat = cat; gm.Times_ = tm; gm.sunLight = sun; gm.Hints = hints;

            ApplyPreviewLighting(sun, TimeOfDay.Morning);
            Finish(scene, SceneDir + "/Game.unity", BakedDir + "/Game");
        }

        // =================================================================================================== menu
        static void BakeMenu()
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var sun = MakeSun();
            var tm = new GameObject("TimeManager").AddComponent<TimeManager>();

            var root = new GameObject("Diorama").transform;
            var wall = Mats.Lit(new Color(0.96f, 0.88f, 0.76f), 0.05f);
            var wood = Mats.Lit(new Color(0.60f, 0.40f, 0.24f), 0.3f);
            var floorTex = Mats.Textured(Mats.Pattern("wood", new Color(0.62f, 0.42f, 0.25f), new Color(0.78f, 0.55f, 0.34f)), new Vector2(4, 3), Color.white, 0.25f);
            var trim = Mats.Lit(new Color(0.98f, 0.96f, 0.92f), 0.2f);
            B.Quad(root, "Floor", new Vector3(1.5f, 0.002f, 1.0f), Vector3.up, new Vector2(7, 5), floorTex);
            B.BoxMinMax(root, "FloorSlab", new Vector3(-2.2f, -0.4f, -1.2f), new Vector3(5.2f, 0f, 3.4f), Mats.Lit(new Color(0.5f, 0.45f, 0.4f), 0.05f));
            // back wall with a window (wall faces the camera, outside is toward -z, where the low sun is)
            float wx0 = 0.4f, wx1 = 2.6f, wy0 = 0.9f, wy1 = 2.5f;
            B.BoxMinMax(root, "WallL", new Vector3(-2.2f, 0, -1.2f), new Vector3(wx0, 3.2f, -1.0f), wall);
            B.BoxMinMax(root, "WallR", new Vector3(wx1, 0, -1.2f), new Vector3(5.2f, 3.2f, -1.0f), wall);
            B.BoxMinMax(root, "WallBelow", new Vector3(wx0, 0, -1.2f), new Vector3(wx1, wy0, -1.0f), wall);
            B.BoxMinMax(root, "WallAbove", new Vector3(wx0, wy1, -1.2f), new Vector3(wx1, 3.2f, -1.0f), wall);
            B.BoxMinMax(root, "SideWall", new Vector3(-2.2f, 0, -1.2f), new Vector3(-2.0f, 3.2f, 3.4f), wall);
            // frame + sill
            B.BoxMinMax(root, "FrameL", new Vector3(wx0, wy0, -1.12f), new Vector3(wx0 + 0.06f, wy1, -0.98f), trim, false);
            B.BoxMinMax(root, "FrameR", new Vector3(wx1 - 0.06f, wy0, -1.12f), new Vector3(wx1, wy1, -0.98f), trim, false);
            B.BoxMinMax(root, "FrameT", new Vector3(wx0, wy1 - 0.06f, -1.12f), new Vector3(wx1, wy1, -0.98f), trim, false);
            B.BoxMinMax(root, "FrameM", new Vector3(1.47f, wy0, -1.12f), new Vector3(1.53f, wy1, -0.98f), trim, false);
            var sill = B.BoxMinMax(root, "Sill", new Vector3(wx0 - 0.15f, wy0 - 0.06f, -1.0f), new Vector3(wx1 + 0.15f, wy0, -0.62f), wood);
            var glass = B.Quad(root, "Glass", new Vector3(1.5f, 1.7f, -1.1f), Vector3.forward, new Vector2(wx1 - wx0, wy1 - wy0), Mats.Blend(new Color(0.75f, 0.88f, 1f, 0.07f), Mats.Soft, Mats.BlendKind.Alpha));
            var curt = Mats.Textured(Mats.Pattern("stripes", new Color(0.85f, 0.45f, 0.42f), new Color(0.74f, 0.38f, 0.35f), 128), new Vector2(3, 1), Color.white, 0.1f);
            B.BoxMinMax(root, "CurtainL", new Vector3(0.1f, 0.5f, -0.95f), new Vector3(0.5f, 2.8f, -0.85f), curt, false);
            B.BoxMinMax(root, "CurtainR", new Vector3(2.5f, 0.5f, -0.95f), new Vector3(2.9f, 2.8f, -0.85f), curt, false);
            B.Cyl(root, "Rod", new Vector3(1.5f, 2.75f, -0.9f), 0.02f, 3.2f, Mats.Metal(new Color(0.6f, 0.5f, 0.35f), 0.6f), Col.None).transform.rotation = Quaternion.Euler(0, 0, 90);
            // a plant and a rug
            var pot = Mats.Lit(new Color(0.72f, 0.4f, 0.28f), 0.2f);
            var leaf = Mats.Lit(new Color(0.34f, 0.62f, 0.3f), 0.2f);
            B.Cyl(root, "Pot", new Vector3(3.6f, 0.2f, 0.2f), 0.2f, 0.4f, pot, Col.None);
            for (int i = 0; i < 8; i++)
            {
                var l = B.Sphere(root, "Leaf", new Vector3(3.6f + Mathf.Cos(i) * 0.12f, 0.75f, 0.2f + Mathf.Sin(i) * 0.12f), new Vector3(0.1f, 0.6f, 0.2f), leaf, Col.None);
                l.transform.rotation = Quaternion.Euler(20, i * 45f, 25);
            }
            B.Quad(root, "Rug", new Vector3(1.5f, 0.012f, 1.0f), Vector3.up, new Vector2(2.6f, 1.8f), Mats.Textured(Mats.Pattern("rug", new Color(0.55f, 0.72f, 0.78f), new Color(0.95f, 0.88f, 0.7f), 128), Vector2.one, Color.white, 0.05f));
            var lampGo = new GameObject("WarmLight");
            lampGo.transform.SetParent(root, false); lampGo.transform.position = new Vector3(0.2f, 1.8f, 1.2f);
            var wl = lampGo.AddComponent<Light>(); wl.type = LightType.Point; wl.color = new Color(1f, 0.8f, 0.55f); wl.range = 6f; wl.intensity = 1.6f;

            // outside: grass, hills and trees
            var grass = Mats.Textured(Mats.Pattern("grass", new Color(0.30f, 0.52f, 0.24f), new Color(0.42f, 0.66f, 0.30f)), new Vector2(40, 40), Color.white, 0.05f);
            B.Quad(root, "Ground", new Vector3(1.5f, -0.02f, -60f), Vector3.up, new Vector2(240, 240), grass);
            var hill = Mats.Lit(new Color(0.25f, 0.45f, 0.30f), 0.05f);
            B.Sphere(root, "Hill1", new Vector3(-20f, -12f, -90f), new Vector3(110f, 45f, 70f), hill, Col.None, false);
            B.Sphere(root, "Hill2", new Vector3(40f, -14f, -100f), new Vector3(120f, 55f, 80f), hill, Col.None, false);
            string[] trees = { "Art/Props/Trees/FBX/Birch_1", "Art/Props/Trees/FBX/Tree_1", "Art/Props/Scenery/BroadleafTree_01", "Art/Props/Trees/FBX/Birch_4", "Art/Props/Scenery/Fir_01" };
            Vector3[] tp = { new Vector3(-5, 0, -9), new Vector3(7, 0, -11), new Vector3(1, 0, -16), new Vector3(-11, 0, -18), new Vector3(13, 0, -19) };
            for (int i = 0; i < tp.Length; i++)
            {
                var pf = Resources.Load<GameObject>(trees[i]);
                if (pf == null) continue;
                var t = WorldBuilder.Spawn(pf, root);
                t.transform.position = tp[i]; t.transform.localScale = Vector3.one * 1.1f;
                FixMaterials.Scenery(t, trees[i]);
            }

            // the cat on the sill
            var catRoot = new GameObject("Miso");
            catRoot.transform.position = new Vector3(1.0f, wy0, -0.78f);
            catRoot.transform.rotation = Quaternion.Euler(0, 180f, 0);
            var model = new GameObject("Model");
            model.transform.SetParent(catRoot.transform, false);
            var cm = model.AddComponent<CatModel>(); cm.Build();

            var camGo = new GameObject("Main Camera"); camGo.tag = "MainCamera";
            var cam = camGo.AddComponent<Camera>();
            cam.nearClipPlane = 0.05f; cam.farClipPlane = 900f; cam.fieldOfView = 55f; cam.clearFlags = CameraClearFlags.Skybox;
            camGo.AddComponent<AudioListener>();
            cam.GetUniversalAdditionalCameraData();
            camGo.transform.position = new Vector3(1.4f, 1.5f, 1.8f);
            camGo.transform.rotation = Quaternion.LookRotation(new Vector3(2.4f, 1.1f, -0.8f) - camGo.transform.position);

            var boot = new GameObject("MenuBoot").AddComponent<MenuBoot>();
            Mats.ResetSprites();
            boot.menuUi = MainMenuUI.Bake(Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf"));
            boot.sunLight = sun; boot.times = tm; boot.cat = cm; boot.lookTarget = catRoot.transform;

            ApplyPreviewLighting(sun, TimeOfDay.Sunset);
            Finish(scene, SceneDir + "/MainMenu.unity", BakedDir + "/MainMenu");
        }

        // =================================================================================================== helpers
        static Light MakeSun()
        {
            var g = new GameObject("Sun");
            var l = g.AddComponent<Light>();
            l.type = LightType.Directional; l.shadows = LightShadows.Soft;
            return l;
        }

        /// <summary>So the scenes look right in the editor before Play (the runtime TimeManager takes over once playing).</summary>
        static void ApplyPreviewLighting(Light sun, TimeOfDay t)
        {
            var d = Times.Get(t);
            Vector3 toSun = Times.ToSun(d.az, d.el);
            sun.transform.rotation = Quaternion.LookRotation(-toSun);
            sun.color = d.sunColor; sun.intensity = d.sunIntensity;
            RenderSettings.sun = sun;
            RenderSettings.ambientMode = AmbientMode.Flat;
            RenderSettings.ambientLight = d.ambient;
            Directory.CreateDirectory(BakedDir);
            var sh = Shader.Find("Skybox/Procedural");
            if (sh != null)
            {
                string p = BakedDir + "/Sky_" + t + ".mat";
                var m = AssetDatabase.LoadAssetAtPath<Material>(p);
                if (m == null) { m = new Material(sh); AssetDatabase.CreateAsset(m, p); }
                m.SetColor("_SkyTint", d.skyTint); m.SetColor("_GroundColor", d.ground); m.SetFloat("_Exposure", d.exposure * 0.9f);
                RenderSettings.skybox = m;
            }
        }

        static void Finish(Scene scene, string path, string assetFolder)
        {
            Directory.CreateDirectory(assetFolder);
            PersistAssets(scene, assetFolder);
            PersistSprites(scene, assetFolder);
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene, path);
        }

        static string Safe(string n)
        {
            foreach (var c in Path.GetInvalidFileNameChars()) n = n.Replace(c, '_');
            return n.Length > 40 ? n.Substring(0, 40) : n;
        }

        static bool IsAsset(Object o) => o == null || !string.IsNullOrEmpty(AssetDatabase.GetAssetPath(o));

        /// <summary>The menu's script-made UI sprites (rounded buttons, glyph icons) are saved as PNG sprite assets so the scene keeps them.</summary>
        static void PersistSprites(Scene scene, string folder)
        {
            var map = new System.Collections.Generic.Dictionary<Texture2D, Sprite>();
            foreach (var root in scene.GetRootGameObjects())
                foreach (var im in root.GetComponentsInChildren<UnityEngine.UI.Image>(true))
                {
                    var sp = im.sprite;
                    if (sp == null || IsAsset(sp)) continue;
                    var tex = sp.texture;
                    if (!map.TryGetValue(tex, out var asset))
                    {
                        string nm = string.IsNullOrEmpty(tex.name) ? (sp.border != Vector4.zero ? "rounded" : "soft") : tex.name;
                        string path = $"{folder}/UI_{Safe(nm)}_{map.Count}.png";
                        var readable = tex;
                        if (!tex.isReadable)
                        {
                            var rt = RenderTexture.GetTemporary(tex.width, tex.height, 0, RenderTextureFormat.ARGB32, RenderTextureReadWrite.sRGB);
                            Graphics.Blit(tex, rt);
                            var prev = RenderTexture.active; RenderTexture.active = rt;
                            readable = new Texture2D(tex.width, tex.height, TextureFormat.RGBA32, false);
                            readable.ReadPixels(new Rect(0, 0, tex.width, tex.height), 0, 0); readable.Apply();
                            RenderTexture.active = prev; RenderTexture.ReleaseTemporary(rt);
                        }
                        File.WriteAllBytes(path, readable.EncodeToPNG());
                        AssetDatabase.ImportAsset(path);
                        var ti = (TextureImporter)AssetImporter.GetAtPath(path);
                        ti.textureType = TextureImporterType.Sprite; ti.spriteImportMode = SpriteImportMode.Single;
                        ti.spritePixelsPerUnit = 100; ti.mipmapEnabled = false; ti.alphaIsTransparency = true;
                        ti.wrapMode = TextureWrapMode.Clamp; ti.spriteBorder = sp.border;
                        ti.SaveAndReimport();
                        asset = AssetDatabase.LoadAssetAtPath<Sprite>(path);
                        map[tex] = asset;
                    }
                    im.sprite = asset;
                }
            AssetDatabase.SaveAssets();
        }

        /// <summary>Script-made materials, textures and meshes only exist in memory; save them as assets so the scene keeps them.</summary>
        static void PersistAssets(Scene scene, string folder)
        {
            int n = 0;
            string Next(string name, string ext) => AssetDatabase.GenerateUniqueAssetPath($"{folder}/{Safe(name)}_{n++}.{ext}");
            foreach (var root in scene.GetRootGameObjects())
            {
                foreach (var r in root.GetComponentsInChildren<Renderer>(true))
                {
                    foreach (var m in r.sharedMaterials)
                    {
                        if (IsAsset(m)) continue;
                        foreach (var prop in m.GetTexturePropertyNames())
                        {
                            var tex = m.GetTexture(prop);
                            if (!IsAsset(tex)) AssetDatabase.CreateAsset(tex, Next(tex.name, "asset"));
                        }
                        AssetDatabase.CreateAsset(m, Next(m.name, "mat"));
                    }
                }
                foreach (var mf in root.GetComponentsInChildren<MeshFilter>(true))
                    if (!IsAsset(mf.sharedMesh)) AssetDatabase.CreateAsset(mf.sharedMesh, Next(mf.sharedMesh.name, "asset"));
            }
            AssetDatabase.SaveAssets();
        }
    }
}
