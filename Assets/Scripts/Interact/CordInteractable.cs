using System.Collections.Generic;
using UnityEngine;

namespace CatOnTheSill
{
    /// <summary>The dangling cord that opens and closes a window cover.</summary>
    public class CordInteractable : Interactable
    {
        public WindowCover cover;
        Transform tassel;
        float pull;

        void Start()
        {
            prompt = cover != null && cover.kind == CoverKind.Blind ? "Pull the blind cord" : (cover != null && cover.kind == CoverKind.Shutters ? "Pull the shutter cord" : "Pull the curtain cord");
            tassel = transform;
        }

        public override void Interact(CatController cat)
        {
            if (cover == null) return;
            cover.Cycle();
            pull = 1f;
            cat?.Paw();
        }

        void Update()
        {
            if (pull > 0f)
            {
                pull = Mathf.MoveTowards(pull, 0f, Time.deltaTime * 2.2f);
                float k = Mathf.Sin(pull * Mathf.PI);
                tassel.localRotation = Quaternion.Euler(k * 18f, 0, k * 12f);
            }
        }
    }
}
