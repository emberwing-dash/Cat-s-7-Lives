using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace CatOnTheSill
{
    /// <summary>A flat reflective plate. The plate's local +Z (forward) is the shiny face.</summary>
    public class Reflector : MonoBehaviour
    {
        public Transform plate;
        public Collider plateCollider;
        public Vector3 Normal => plate != null ? plate.forward : transform.forward;
    }
}
