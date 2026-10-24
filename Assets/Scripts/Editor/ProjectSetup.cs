using System.Linq;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;
using UnityEngine.Rendering;

namespace CatOnTheSill.EditorTools
{
    /// <summary>Makes sure every runtime-found shader ends up in builds, and offers a one-click Windows build.</summary>
    [InitializeOnLoad]
    public static class ProjectSetup
    {
        static readonly string[] RequiredShaders =
        {
            "Skybox/Procedural", "Universal Render Pipeline/Lit", "Universal Render Pipeline/Unlit",
            "Universal Render Pipeline/Simple Lit", "CatOnTheSill/Particle"
        };

        static ProjectSetup() { EditorApplication.delayCall += EnsureShaders; }

        public static void EnsureShaders()
        {
            var gs = AssetDatabase.LoadAssetAtPath<Object>("ProjectSettings/GraphicsSettings.asset");
            if (gs == null) return;
            var so = new SerializedObject(gs);
            var arr = so.FindProperty("m_AlwaysIncludedShaders");
            if (arr == null) return;
            bool changed = false;
            foreach (var name in RequiredShaders)
            {
                var sh = Shader.Find(name);
                if (sh == null) continue;
                bool has = false;
                for (int i = 0; i < arr.arraySize; i++) if (arr.GetArrayElementAtIndex(i).objectReferenceValue == sh) { has = true; break; }
                if (has) continue;
                arr.arraySize++;
                arr.GetArrayElementAtIndex(arr.arraySize - 1).objectReferenceValue = sh;
                changed = true;
            }
            if (changed) { so.ApplyModifiedPropertiesWithoutUndo(); AssetDatabase.SaveAssets(); }
        }

        public static void BuildWindows()
        {
            EnsureShaders();
            PlayerSettings.productName = "Cat on the Sill";
            PlayerSettings.defaultScreenWidth = 1600; PlayerSettings.defaultScreenHeight = 900;
            PlayerSettings.fullScreenMode = FullScreenMode.Windowed;
            PlayerSettings.runInBackground = true;
            var opts = new BuildPlayerOptions
            {
                scenes = EditorBuildSettings.scenes.Where(s => s.enabled).Select(s => s.path).ToArray(),
                locationPathName = "Builds/Windows/CatOnTheSill.exe",
                target = BuildTarget.StandaloneWindows64,
                options = BuildOptions.None
            };
            var report = BuildPipeline.BuildPlayer(opts);
            Debug.Log("Build result: " + report.summary.result + " size=" + report.summary.totalSize);
            if (report.summary.result != BuildResult.Succeeded) EditorApplication.Exit(1);
        }
    }
}
