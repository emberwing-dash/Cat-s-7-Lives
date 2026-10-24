using System;
using System.Collections.Generic;
using UnityEngine;

namespace CatOnTheSill
{
    /// <summary>Static game progress + persistence (PlayerPrefs JSON). Exposes events the UI / audio listen to.</summary>
    public static class Game
    {
        public const string PrefKey = "cots_save_v1";

        public static SaveData Data = new SaveData();
        public static readonly List<IPersist> Persistents = new List<IPersist>();
        public static bool SaveEnabled = true;
        public static bool Locked;          // true while the world is being (re)built or restored - never save then

        public static event Action<int> FragmentCollected;       // index
        public static event Action<Puzzle> PuzzleSolvedEvent;
        public static event Action<string> Toast;
        public static event Action AllFragmentsCollected;

        public static int Collected => Data.CollectedCount();
        public static bool Has(int i) => Data.fragments[i];

        public static void ResetAll()
        {
            Data = new SaveData();
            Persistents.Clear();
        }

        public static bool HasSave()
        {
            try { return PlayerPrefs.HasKey(PrefKey) && !string.IsNullOrEmpty(PlayerPrefs.GetString(PrefKey)); }
            catch { return false; }
        }

        public static void DeleteSave()
        {
            try { PlayerPrefs.DeleteKey(PrefKey); PlayerPrefs.Save(); } catch { }
        }

        public static bool LoadFromDisk()
        {
            try
            {
                if (!HasSave()) return false;
                var d = JsonUtility.FromJson<SaveData>(PlayerPrefs.GetString(PrefKey));
                if (d == null || d.fragments == null || d.fragments.Length != FragmentInfo.Count) return false;
                Data = d;
                return true;
            }
            catch (Exception e) { Debug.LogWarning("Save load failed: " + e.Message); return false; }
        }

        static void Prune() { Persistents.RemoveAll(p => p == null || (p is UnityEngine.Object o && o == null)); }

        public static void Save()
        {
            Prune();
            if (!SaveEnabled || Locked) return;
            try
            {
                foreach (var p in Persistents)
                {
                    try { Data.Set(p.PersistId, p.Capture()); } catch (Exception e) { Debug.LogWarning("Capture failed for " + p.PersistId + ": " + e.Message); }
                }
                var cat = CatController.I;
                if (cat != null)
                {
                    Data.catPos = new[] { cat.transform.position.x, cat.transform.position.y, cat.transform.position.z };
                    Data.catYaw = cat.transform.eulerAngles.y;
                }
                if (TimeManager.I != null) Data.time = (int)TimeManager.I.Target;
                PlayerPrefs.SetString(PrefKey, JsonUtility.ToJson(Data));
                PlayerPrefs.Save();
            }
            catch (Exception e) { Debug.LogWarning("Save failed: " + e.Message); }
        }

        public static void Register(IPersist p)
        {
            if (!Persistents.Contains(p)) Persistents.Add(p);
        }

        public static void Unregister(IPersist p) { Persistents.Remove(p); }

        public static void RestoreAll()
        {
            Prune();
            foreach (var p in Persistents)
            {
                var s = Data.Get(p.PersistId);
                if (s != null) { try { p.Restore(s); } catch (Exception e) { Debug.LogWarning("Restore failed for " + p.PersistId + ": " + e.Message); } }
            }
        }

        /// <summary>Collecting is idempotent - a fragment can never be counted twice.</summary>
        public static bool Collect(int index)
        {
            if (index < 0 || index >= FragmentInfo.Count) return false;
            if (Data.fragments[index]) return false;
            Data.fragments[index] = true;
            FragmentCollected?.Invoke(index);
            Save();
            if (Collected >= FragmentInfo.Count) AllFragmentsCollected?.Invoke();
            return true;
        }

        public static void RaisePuzzleSolved(Puzzle p) { PuzzleSolvedEvent?.Invoke(p); Save(); }
        public static void ShowToast(string s) { Toast?.Invoke(s); }
    }
}
