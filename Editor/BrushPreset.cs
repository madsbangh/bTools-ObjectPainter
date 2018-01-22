using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditorInternal;

namespace bTools.ObjectPainter
{
	[System.Serializable]
	public class BrushPreset
	{
		// Brush Settings.
		[SerializeField] public string brushName = "New brush";
		[SerializeField] public bool colliderStrict = true;
		[SerializeField] public LayerMask layerMask = Physics.AllLayers;
		[SerializeField] public float brushRadius = 5;
		[SerializeField] public float brushRate = 10f;
		[SerializeField] public bool cullEnabled = false;
		[SerializeField] public float cullAngle = 60f;
		[SerializeField] public bool cullInvert = false;
		[SerializeField] public Vector3 cullRef = Vector3.up;

		[SerializeField] public List<BrushPrefabSettings> prefabs = new List<BrushPrefabSettings>();

		// GUI.
		private Vector2 scrollPos;
		private readonly GUIContent colliderStrictContent = new GUIContent( "Collider Strict", "Only places objects on the first collider you clicked on" );
		private readonly GUIContent rateContent = new GUIContent( "Rate", "Objects placed per second" );
		private readonly GUIContent radiusContent = new GUIContent( "Radius", "Alt + MouseWheel to edit in the scene view" );
		private readonly GUIContent layersContent = new GUIContent( "Layers", "Physics Layers to target" );
		private readonly GUIContent cullingContent = new GUIContent( "Culling", "Prevent objects from being placed under certain conditions" );
		private readonly GUIContent cullContent = new GUIContent( "Angle Cull", "Prevent objects from being placed if the angle between the ref vector and the surface normal is too large" );
		private readonly GUIContent cullAngleContent = new GUIContent( "Angle", "Angle between Up and the surface normal" );
		private readonly GUIContent cullInvertContent = new GUIContent( "Invert", "Check that the angle is smaller instead of larger" );
		private readonly GUIContent cullRefContent = new GUIContent( "Ref Vector", "The surface normal is compared to this vector to determine the angle. 0,1,0 is Up, which is what you want most of the time" );

		private ReorderableList prefabsList;

		public void OnGUI( ObjectPainterWindow window )
		{
			//EditorGUIUtility.wideMode = true;
			EditorGUIUtility.labelWidth = 60;
			using ( new EditorGUILayout.VerticalScope( GUI.skin.box ) )
			{
				EditorGUILayout.LabelField( "General - " + brushName, EditorStyles.boldLabel );
				brushName = EditorGUILayout.TextField( "Name ", brushName );

				brushRadius = Mathf.Abs( EditorGUILayout.FloatField( radiusContent, brushRadius ) );
				brushRate = Mathf.Clamp( EditorGUILayout.FloatField( rateContent, brushRate ), 1, 10000 );
			}

			using ( new EditorGUILayout.VerticalScope( GUI.skin.box ) )
			{
				EditorGUILayout.LabelField( "Colliders", EditorStyles.boldLabel );
				layerMask = LayerMaskField( layersContent, layerMask );
				colliderStrict = EditorGUILayout.ToggleLeft( colliderStrictContent, colliderStrict );
			}

			using ( new EditorGUILayout.VerticalScope( GUI.skin.box ) )
			{
				EditorGUILayout.LabelField( cullingContent, EditorStyles.boldLabel );
				cullEnabled = EditorGUILayout.ToggleLeft( cullContent, cullEnabled );
				if ( cullEnabled )
				{
					EditorGUIUtility.labelWidth = 80;
					cullAngle = EditorGUILayout.Slider( cullAngleContent, cullAngle, 0f, 90f );
					cullInvert = EditorGUILayout.ToggleLeft( cullInvertContent, cullInvert );
					cullRef = EditorGUILayout.Vector3Field( cullRefContent, cullRef );
				}
			}

			GUILayout.Space( 8 );

			EditorGUILayout.LabelField( "Prefabs", EditorStyles.boldLabel );

			if ( prefabsList == null )
			{
				prefabsList = new ReorderableList( prefabs, typeof( BrushPrefabSettings ), false, true, true, true );
				prefabsList.drawHeaderCallback = ( Rect rect ) =>
				{
					EditorGUI.LabelField( rect, "Prefabs" );
					rect.x = rect.xMax - 32;
					rect.width = 16;
					if ( GUI.Button( rect, EditorGUIUtility.FindTexture( "d_Toolbar Plus" ), GUIStyle.none ) )
					{
						prefabs.Add( new BrushPrefabSettings() );
						prefabsList.index = prefabs.Count - 1;
						prefabsList.GrabKeyboardFocus();
						scrollPos.y = float.MaxValue;
					}
					rect.x += 16;

					using ( new EditorGUI.DisabledGroupScope( prefabsList.index > 0 ) )
					{
						if ( GUI.Button( rect, EditorGUIUtility.FindTexture( "d_Toolbar Minus" ), GUIStyle.none ) )
						{
							if ( prefabsList.index >= 0 && prefabsList.index <= prefabs.Count - 1 )
							{
								Undo.RecordObject( window.SavedBrushes, "Removed Brush Preset" );
								prefabs.RemoveAt( prefabsList.index );
								prefabsList.index = Mathf.Max( 0, prefabsList.index - 1 );
								prefabsList.GrabKeyboardFocus();
							}
						}
					}
				};

				prefabsList.drawElementCallback = ( Rect rect, int index, bool isActive, bool isFocused ) =>
				{
					prefabs[index].OnGUI( index, rect );
				};

				prefabsList.elementHeightCallback = ( int index ) =>
				{
					return prefabs[index].GetHeight();
				};

				prefabsList.elementHeight = 100;
			}

			// Per-object settings.
			using ( var scroll = new EditorGUILayout.ScrollViewScope( scrollPos ) )
			{
				scrollPos = scroll.scrollPosition;
				prefabsList.DoLayoutList();
			}
		}

		// Unity, why you no have this for editor windows ?
		static List<int> layerNumbers = new List<int>();

		static LayerMask LayerMaskField( GUIContent layersContent, LayerMask layerMask )
		{
			var layers = InternalEditorUtility.layers;

			layerNumbers.Clear();

			for ( int i = 0 ; i < layers.Length ; i++ )
				layerNumbers.Add( LayerMask.NameToLayer( layers[i] ) );

			int maskWithoutEmpty = 0;
			for ( int i = 0 ; i < layerNumbers.Count ; i++ )
			{
				if ( ( ( 1 << layerNumbers[i] ) & layerMask.value ) > 0 )
					maskWithoutEmpty |= ( 1 << i );
			}

			maskWithoutEmpty = UnityEditor.EditorGUILayout.MaskField( layersContent, maskWithoutEmpty, layers );

			int mask = 0;
			for ( int i = 0 ; i < layerNumbers.Count ; i++ )
			{
				if ( ( maskWithoutEmpty & ( 1 << i ) ) > 0 )
					mask |= ( 1 << layerNumbers[i] );
			}
			layerMask.value = mask;

			return layerMask;
		}
	}
}