using UnityEngine;

namespace CatOnTheSill
{
    /// <summary>Simple puzzle solved by placing two objects on clearly marked floor spots.</summary>
    public class PlacementPuzzle : Puzzle
    {
        public MovableObject first;
        public MovableObject second;
        public Vector3 firstPosition;
        public Vector3 secondPosition;
        public float radius = 0.75f;

        protected override bool Evaluate(out float closeness)
        {
            float a = Distance(first, firstPosition);
            float b = second != null ? Distance(second, secondPosition) : 0f;   // single-object puzzles leave 'second' empty
            bool firstPlaced = first != null && a <= radius;
            bool secondPlaced = second == null || b <= radius;
            closeness = second != null ? (Mathf.Clamp01(1f - a / 3f) + Mathf.Clamp01(1f - b / 3f)) * 0.5f : Mathf.Clamp01(1f - a / 3f);
            return firstPlaced && secondPlaced;
        }

        static float Distance(MovableObject item, Vector3 target)
        {
            if (item == null) return 99f;
            Vector3 p = item.transform.position;
            return Vector2.Distance(new Vector2(p.x, p.z), new Vector2(target.x, target.z));
        }
    }
}
