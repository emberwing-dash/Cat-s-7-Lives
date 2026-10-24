using System;
using System.Collections.Generic;
using UnityEngine;

namespace CatOnTheSill
{
    /// <summary>Serialized index of the baked house (puzzles, people, start point, known solutions).</summary>
    public class WorldRefs : MonoBehaviour
    {
        public List<Puzzle> puzzles = new List<Puzzle>();
        public Vector3 catStart = new Vector3(1.2f, 0.66f, 5f);
        public List<NamedPoint> solutionList = new List<NamedPoint>();

        Dictionary<string, Vector3> map;
        public Dictionary<string, Vector3> Solutions
        {
            get
            {
                if (map == null) { map = new Dictionary<string, Vector3>(); foreach (var s in solutionList) map[s.key] = s.value; }
                return map;
            }
        }
    }
}
