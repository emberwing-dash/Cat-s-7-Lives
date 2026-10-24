using System.Collections.Generic;
using UnityEngine;

namespace CatOnTheSill
{
    /// <summary>A carved sun mark that gives up its fragment when pawed (after it has been lit).</summary>
    public class RevealMark : Interactable
    {
        public Puzzle puzzle;
        public string text = "Touch the glowing mark";
        void Start() { prompt = text; range = 1.1f; needsFacing = false; }
        public override bool CanInteract => base.CanInteract && puzzle != null && puzzle.Solved && puzzle.Fragment == null && !Game.Has(puzzle.fragmentIndex);
        public override void Interact(CatController cat) { cat.Paw(); puzzle.SpawnFragment(); }
    }
}
