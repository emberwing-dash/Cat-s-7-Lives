using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace CatOnTheSill
{
    /// <summary>A puzzle satisfied when light sensors are lit by reflected beams.</summary>
    public class SensorPuzzle : Puzzle
    {
        public List<LightSensor> sensors = new List<LightSensor>();
        public Action<bool> OnSensorState;               // for visuals while the beam is hitting (before the latch)
        bool lastLit;

        protected override bool Evaluate(out float closeness)
        {
            int lit = 0;
            foreach (var s in sensors) if (s != null && s.Lit) lit++;
            closeness = sensors.Count == 0 ? 0 : lit / (float)sensors.Count;
            bool all = sensors.Count > 0 && lit == sensors.Count;
            if (all != lastLit) { lastLit = all; OnSensorState?.Invoke(all); }
            return all;
        }
    }
}
