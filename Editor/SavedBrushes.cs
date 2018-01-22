using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace bTools.ObjectPainter
{
    [System.Serializable]
    public class SavedBrushes : ScriptableObject
    {
        public List<BrushPreset> brushes = new List<BrushPreset>();
    }
}