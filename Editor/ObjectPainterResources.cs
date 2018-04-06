using System.IO;
using UnityEditor;
using UnityEngine;

namespace bTools.ObjectPainter
{
	public static class ObjectPainterResources
	{
		public static readonly Color BrushColorHidden = new Color32(130, 142, 132, 128);
		public static readonly Color BrushColor = new Color32(0, 255, 127, 255);
		public static readonly Color HeaderSeparatorColor = new Color32(237, 166, 3, 255);

		public static string PathToToolRoot
		{
			get
			{
				string path;

				path = Directory.GetDirectories(Application.dataPath, "bTools", SearchOption.AllDirectories)[0];

				path = path.Replace(Application.dataPath, string.Empty);
				path = path.Replace('\\', '/');
				path = @"Assets" + path + "/ObjectPainter/";

				return path;
			}
		}

		public static SavedBrushes LoadSavedBrushes()
		{
			string[] guids = AssetDatabase.FindAssets(string.Format("t:{0}", typeof(SavedBrushes).ToString().Replace("UnityEngine.", string.Empty)));

			for (int i = 0; i < guids.Length; i++)
			{
				string assetPath = AssetDatabase.GUIDToAssetPath(guids[i]);
				SavedBrushes asset = AssetDatabase.LoadAssetAtPath<SavedBrushes>(assetPath);
				if (asset != null)
				{
					return asset;
				}
			}
			return GenerateSavedBrushesAsset();
		}

		public static SavedBrushes GenerateSavedBrushesAsset()
		{
			SavedBrushes asset = ScriptableObject.CreateInstance<SavedBrushes>();
			var uniquePath = AssetDatabase.GenerateUniqueAssetPath(PathToToolRoot + "ObjectPainter_SavedBrushes.asset");
			AssetDatabase.CreateAsset(asset, uniquePath);
			AssetDatabase.SaveAssets();
			AssetDatabase.Refresh();

			return AssetDatabase.LoadAssetAtPath(uniquePath, typeof(SavedBrushes)) as SavedBrushes;
		}
	}
}
