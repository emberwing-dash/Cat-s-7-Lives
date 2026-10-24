using System;
using System.Collections.Generic;
using UnityEngine;

namespace CatOnTheSill
{
    /// <summary>Anything whose state must survive room changes and save/load implements this.</summary>
    public interface IPersist
    {
        string PersistId { get; }
        string Capture();
        void Restore(string state);
    }
}
