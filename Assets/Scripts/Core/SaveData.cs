using System;
using System.Collections.Generic;
using UnityEngine;

namespace CatOnTheSill
{
    [Serializable]
    public class SaveData
    {
        public int version = 1;
        public bool[] fragments = new bool[FragmentInfo.Count];
        public int time = (int)TimeOfDay.Morning;
        public float[] catPos = null;
        public float catYaw;
        public bool ending;
        public bool introSeen;
        public bool tutorialDone;
        public float playSeconds;
        public List<KV> kv = new List<KV>();

        public string Get(string key)
        {
            for (int i = 0; i < kv.Count; i++) if (kv[i].k == key) return kv[i].v;
            return null;
        }

        public void Set(string key, string value)
        {
            for (int i = 0; i < kv.Count; i++) if (kv[i].k == key) { kv[i].v = value; return; }
            kv.Add(new KV { k = key, v = value });
        }

        public int CollectedCount() { int n = 0; foreach (var f in fragments) if (f) n++; return n; }
    }
}
