using System.Collections.Generic;
using UnityEngine;

namespace CatOnTheSill
{
    /// <summary>Opens the time-of-day picker.</summary>
    public class SkyWindow : Interactable
    {
        public string windowName = "window";
        void Start() { prompt = "Look out the window - change the hour"; range = 1.3f; needsFacing = false; }
        public override void Interact(CatController cat) { UIManager.I?.OpenTimePanel(this); }
    }
}
