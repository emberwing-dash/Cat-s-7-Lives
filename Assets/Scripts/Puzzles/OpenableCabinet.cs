using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace CatOnTheSill
{
    /// <summary>A cabinet whose doors swing open when its puzzle is solved.</summary>
    public class OpenableCabinet : MonoBehaviour
    {
        public Transform doorL, doorR;
        public float openAngle = 105f;
        float cur, target;
        public void Open(bool animated)
        {
            target = 1f;
            if (!animated) { cur = 1f; Apply(); }
            else AudioManager.I?.Play("open", transform.position, 0.9f, 1f);
        }
        void Update()
        {
            if (Mathf.Abs(cur - target) > 0.001f) { cur = Mathf.MoveTowards(cur, target, Time.deltaTime * 0.7f); Apply(); }
        }
        void Apply()
        {
            if (doorL != null) doorL.localRotation = Quaternion.Euler(0, -openAngle * cur, 0);
            if (doorR != null) doorR.localRotation = Quaternion.Euler(0, openAngle * cur, 0);
        }
    }
}
