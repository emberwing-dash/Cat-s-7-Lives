using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace CatOnTheSill
{
    /// <summary>A moon-lily bloom that stays hidden until the true beam finds it (and then forever).</summary>
    public class HiddenBloom : MonoBehaviour
    {
        public LightSensor sensor;
        public Puzzle puzzle;
        public Transform bloom;
        float v;
        void Update()
        {
            bool show = (puzzle != null && puzzle.Solved) || (sensor != null && sensor.Lit);
            v = Mathf.MoveTowards(v, show ? 1f : 0f, Time.deltaTime * (show ? 1.2f : 2f));
            bloom.localScale = Vector3.one * Mathf.SmoothStep(0, 1, v);
            bloom.gameObject.SetActive(v > 0.01f);
            bloom.Rotate(0, 20 * Time.deltaTime, 0, Space.Self);
        }
    }
}
