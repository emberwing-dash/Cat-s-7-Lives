using UnityEngine;

namespace CatOnTheSill
{
    /// <summary>What Miso says (in her head) when she meows. Lines get more hopeful as fragments return.</summary>
    public static class CatLines
    {
        static readonly string[][] stages =
        {
            new[] { "Mrrow? Something warm is missing from me...", "Meow. The light knows where my pieces went.", "Mew! Follow the sunbeams, Miso." },
            new[] { "Mrrp! One piece is home. Six to go.", "Meow. Shadows can be pushed around, you know.", "Mrow... Maybe the hour matters. Try the sky windows." },
            new[] { "Prrr! I feel a little more like me.", "Meow. Mirrors send light where light doesn't go.", "Mrrow. Moonlight draws pictures too." },
            new[] { "Prrrow! Nearly whole again.", "Mew. The living-room wall is waking up...", "Meow! Sunrise, every blind open." }
        };
        static int i;

        public static string Next()
        {
            int p = Game.Collected;
            var set = stages[p <= 0 ? 0 : (p <= 2 ? 1 : (p <= 4 ? 2 : 3))];
            return set[i++ % set.Length];
        }
    }
}
