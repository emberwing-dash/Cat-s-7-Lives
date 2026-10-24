using UnityEngine;

namespace CatOnTheSill
{
    /// <summary>The names and memories of the seven soul fragments.</summary>
    public static class FragmentInfo
    {
        public const int Count = 7;

        public static readonly string[] Titles =
        {
            "The First Ray", "The Garden's Shadow", "The Silver Tray", "Three Shadows",
            "The Moving Moon", "The False Reflection", "The House Remembers"
        };

        public static readonly string[] Rooms =
        {
            "Bedroom", "Living Room", "Kitchen", "Study", "Attic", "Sunroom", "Library"
        };

        public static readonly string[] Memories =
        {
            "A warm paw of sunlight on the pillow. You remember waking, and being loved.",
            "A fern, a stack of crates, and the long orange hour when the house breathed out.",
            "Something silver that laughed light across the kitchen, and a cupboard that kept a secret.",
            "Three small shadows on a desk at night - ink, globe and a little music box.",
            "The moon climbing the rafters, slow as a sleepy cat, until it found the bowl.",
            "A bright path that went nowhere, and a quiet one that led home.",
            "Everything you ever were, curled up and warm. You remember. You are home."
        };

        // pastel / gem colours for the seven fragments
        public static readonly Color[] Colors =
        {
            new Color(1.00f, 0.82f, 0.35f), new Color(0.55f, 0.95f, 0.55f), new Color(0.75f, 0.90f, 1.00f),
            new Color(0.70f, 0.60f, 1.00f), new Color(0.55f, 0.75f, 1.00f), new Color(1.00f, 0.60f, 0.65f),
            new Color(1.00f, 0.95f, 0.85f)
        };
    }
}
