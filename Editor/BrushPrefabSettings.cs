using UnityEngine;
using UnityEditor;
using bTools.CodeExtensions;

namespace bTools.ObjectPainter
{
	[System.Serializable]
	public class BrushPrefabSettings
	{
		// Enums.
		public enum AlignMode
		{
			Up,
			Down,
			Left,
			Right,
			Forward,
			Backward
		}

		// Prefab Settings.
		[SerializeField] public GameObject paintObject;
		[SerializeField] public bool alignToPath = false;
		[SerializeField] public Vector2 objectRandomScale = Vector2.one;
		[SerializeField] public Vector2 objectRandomRotation = Vector2.zero;
		[SerializeField] public AlignMode alignMode = AlignMode.Up;
		[SerializeField] public float weight = 1;
		[System.NonSerialized] public bool isExpanded = false;

		// GUI
		private readonly GUIContent alignContent = new GUIContent( "Align to", "Which axis of the prefab to align with the surface normal" );
		private readonly GUIContent weightContent = new GUIContent( "Weight", "How likely this prefab is to be selected instead of others in this brush" );
		private readonly GUIContent alignToPathContent = new GUIContent( "Align to Path", "Align to follow the direction of the brush stroke" );

		public void OnGUI( int index, Rect area )
		{
			area.height = EditorGUIUtility.singleLineHeight;
			area.y += 2;
			isExpanded = EditorGUI.Foldout( area.WithW( 10 ), isExpanded, "" );
			paintObject = EditorGUI.ObjectField( area.WithX( area.x + 15 ).WithW( area.width - 20 ), string.Empty, paintObject, typeof( GameObject ), false ) as GameObject;

			if ( isExpanded )
			{
				area.y += EditorGUIUtility.singleLineHeight + 2;
				EditorGUI.LabelField( area, "Scale", EditorStyles.boldLabel );
				area.y += EditorGUIUtility.singleLineHeight + 2;

				Rect minMaxScale = area;
				EditorGUIUtility.labelWidth = 32;
				minMaxScale.width = Mathf.Ceil( minMaxScale.width / 2 );
				objectRandomScale.x = EditorGUI.FloatField( minMaxScale, "Min", objectRandomScale.x );
				minMaxScale.x = minMaxScale.xMax;
				objectRandomScale.y = EditorGUI.FloatField( minMaxScale, "Max", objectRandomScale.y );

				area.y += EditorGUIUtility.singleLineHeight + 2;
				Rect rotMode = area;
				EditorGUI.LabelField( rotMode, "Rotation - ", EditorStyles.boldLabel );
				rotMode.x += 67;
				EditorGUIUtility.labelWidth = 80;
				alignToPath = EditorGUI.Toggle( rotMode, alignToPathContent, alignToPath );
				EditorGUIUtility.labelWidth = 32;
				area.y += EditorGUIUtility.singleLineHeight + 2;

				Rect minMaxRot = area;
				minMaxRot.width = Mathf.Ceil( minMaxRot.width / 2 );
				objectRandomRotation.x = EditorGUI.FloatField( minMaxRot, "Min", objectRandomRotation.x );
				minMaxRot.x = minMaxRot.xMax;
				objectRandomRotation.y = EditorGUI.FloatField( minMaxRot, "Max", objectRandomRotation.y );

				area.y += EditorGUIUtility.singleLineHeight + 2;
				EditorGUI.LabelField( area, "Other", EditorStyles.boldLabel );
				area.y += EditorGUIUtility.singleLineHeight + 2;

				Rect finalSettings = area;
				EditorGUIUtility.labelWidth = 47;
				finalSettings.width = Mathf.Ceil( finalSettings.width / 2 );
				alignMode = (AlignMode)EditorGUI.EnumPopup( finalSettings, alignContent, alignMode );
				finalSettings.x = finalSettings.xMax;
				weight = EditorGUI.FloatField( finalSettings, weightContent, weight );
			}
		}

		public float GetHeight()
		{
			if ( isExpanded ) return 130;
			return EditorGUIUtility.singleLineHeight + 6;
		}
	}
}