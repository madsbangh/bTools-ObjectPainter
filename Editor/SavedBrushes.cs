using System.Collections.Generic;
using UnityEngine;

namespace bTools.ObjectPainter
{
	[System.Serializable]
	public class SavedBrushes : ScriptableObject
	{
		public List<BrushPreset> brushes = new List<BrushPreset>();
	}
}