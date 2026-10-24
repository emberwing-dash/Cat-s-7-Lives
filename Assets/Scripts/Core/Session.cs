using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;

namespace CatOnTheSill
{
    /// <summary>Which scene to start and whether the Game scene should restore a save.</summary>
    public static class Session
    {
        public static bool Continue;
        public const string MenuScene = "MainMenu";
        public const string GameScene = "Game";
    }
}
