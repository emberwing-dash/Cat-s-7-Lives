using System;
using UnityEngine;

namespace CatOnTheSill
{
    /// <summary>
    /// Picks the UI font. A handwritten-style system font is used only where one is really installed (Windows / Mac
    /// players and the editor). Web builds have no system fonts: asking for one still returns a Font object, but its
    /// line metrics are empty, so every multi-line text collapses onto itself. There the built-in font is used instead.
    /// </summary>
    public static class UiFont
    {
        static readonly string[] Preferred = { "Segoe Print", "Ink Free", "Comic Sans MS", "Chalkboard SE", "Trebuchet MS", "Verdana" };

        public static Font Pick()
        {
#if !UNITY_WEBGL || UNITY_EDITOR
            try
            {
                var installed = Font.GetOSInstalledFontNames();
                if (installed != null && installed.Length > 0)
                    foreach (var want in Preferred)
                        foreach (var have in installed)
                            if (string.Equals(want, have, StringComparison.OrdinalIgnoreCase))
                            {
                                var f = Font.CreateDynamicFontFromOSFont(have, 24);
                                if (f != null) return f;
                            }
            }
            catch (Exception) { }
#endif
            return Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        }
    }
}
