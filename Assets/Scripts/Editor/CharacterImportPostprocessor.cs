using UnityEditor;
using UnityEngine;

namespace CatOnTheSill.EditorTools
{
    /// <summary>Makes every imported character humanoid so any idle/walk clip retargets onto any person.</summary>
    public class CharacterImportPostprocessor : AssetPostprocessor
    {
        void OnPreprocessModel()
        {
            string p = assetPath.Replace(System.IO.Path.DirectorySeparatorChar, '/');
            if (!p.Contains("/Resources/Art/Characters/")) return;
            var mi = (ModelImporter)assetImporter;
            bool isAnim = p.Contains("/anim/");
            if (mi.animationType != ModelImporterAnimationType.Human)
            {
                mi.animationType = ModelImporterAnimationType.Human;
                mi.avatarSetup = ModelImporterAvatarSetup.CreateFromThisModel;
            }
            if (isAnim)
            {
                var clips = mi.defaultClipAnimations;
                foreach (var c in clips)
                {
                    c.loopTime = true;
                    c.lockRootRotation = true;
                    c.lockRootHeightY = true;
                    c.lockRootPositionXZ = true;
                    c.keepOriginalOrientation = true;
                    c.keepOriginalPositionY = true;
                    c.keepOriginalPositionXZ = true;
                }
                mi.clipAnimations = clips;
            }
        }
    }
}
