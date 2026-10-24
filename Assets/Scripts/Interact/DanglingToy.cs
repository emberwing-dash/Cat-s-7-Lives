using System.Collections.Generic;
using UnityEngine;

namespace CatOnTheSill
{
    /// <summary>A dangling toy to bat about. Pure delight, no puzzle.</summary>
    public class DanglingToy : Interactable
    {
        public Transform swing;       // pivot at the top of the string
        float ang, vel;
        Vector3 axis = Vector3.right;

        void Start() { prompt = "Bat the toy"; needsFacing = false; range = 0.9f; }

        public override void Interact(CatController cat)
        {
            Vector3 dir = (swing.position + Vector3.down - cat.transform.position); dir.y = 0;
            if (dir.sqrMagnitude < 0.001f) dir = cat.transform.forward;
            axis = Vector3.Cross(Vector3.up, dir.normalized);
            vel += 5.5f;
            cat.Paw();
            AudioManager.I?.Play("jingle", swing.position, 0.7f, 1f);
        }

        void Update()
        {
            vel += (-ang * 18f - vel * 0.55f) * Time.deltaTime;
            ang += vel * Time.deltaTime;
            swing.localRotation = Quaternion.AngleAxis(ang * Mathf.Rad2Deg * 0.6f, axis);
        }
    }
}
