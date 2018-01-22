using UnityEngine;
using UnityEditor;
using UnityEditorInternal;
using UnityEditor.AnimatedValues;
using bTools.CodeExtensions;

namespace bTools.ObjectPainter
{
	public class ObjectPainterWindow : EditorWindow
	{
		// GUI
		private AnimBool panelAnim;
		private Vector2 presetsScroll;
		ReorderableList brushList;

		// Tool
		private RaycastHit brushHit;
		private Vector3 lastHitPoint = Vector3.zero;
		private Collider lastCollider;
		private bool toolEnabled;
		private bool mouseIsDown;
		private double placeTimeStamp;

		// Data
		private SavedBrushes m_savedBrushes;
		public SavedBrushes SavedBrushes
		{
			get
			{
				if ( m_savedBrushes == null )
				{
					var brush = EditorGUIExtensions.LoadAssetsOfType<SavedBrushes>();
					if ( brush.Count == 0 )
					{
						m_savedBrushes = EditorGUIExtensions.InstanciateScriptableObject<SavedBrushes>( Ressources.PathTo_bData );
					}
					else
					{
						m_savedBrushes = brush[0];
					}
				}

				EditorUtility.SetDirty( m_savedBrushes );
				return m_savedBrushes;
			}
		}

		[MenuItem( "bTools/ObjectPainter" )]
		static void Init()
		{
			var window = GetWindow<ObjectPainterWindow>( string.Empty, true );

			window.titleContent = new GUIContent( "Object Painter" );
			window.minSize = new Vector2( 220, 100 );

		}

		private void OnEnable()
		{
			panelAnim = new AnimBool( false, Repaint );
			SceneView.onSceneGUIDelegate += OnSceneGUI;
			Undo.undoRedoPerformed += Repaint;

			// Setup brush list
			if ( brushList == null )
			{
				brushList = new ReorderableList( SavedBrushes.brushes, typeof( BrushPreset ), false, true, true, true );
				brushList.drawHeaderCallback = ( Rect rect ) =>
				{
					EditorGUI.LabelField( rect, "Brushes" );
					rect.x = rect.xMax - 32;
					rect.width = 16;
					if ( GUI.Button( rect, EditorGUIUtility.FindTexture( "d_Toolbar Plus" ), GUIStyle.none ) )
					{
						SavedBrushes.brushes.Add( new BrushPreset() );
						brushList.index = SavedBrushes.brushes.Count - 1;
						brushList.GrabKeyboardFocus();
						presetsScroll.y = float.MaxValue;
					}
					rect.x += 16;

					if ( GUI.Button( rect, EditorGUIUtility.FindTexture( "d_Toolbar Minus" ), GUIStyle.none ) )
					{
						if ( brushList.index >= 0 && brushList.index <= SavedBrushes.brushes.Count - 1 )
						{
							Undo.RecordObject( SavedBrushes, "Removed Brush Preset" );
							SavedBrushes.brushes.RemoveAt( brushList.index );
							brushList.index = Mathf.Max( 0, brushList.index - 1 );
							brushList.GrabKeyboardFocus();
						}
					}
				};

				brushList.drawElementCallback = ( Rect rect, int index, bool isActive, bool isFocused ) =>
				{
					EditorGUI.LabelField( rect, SavedBrushes.brushes[index].brushName );
				};
			}

			if ( SavedBrushes.brushes.Count == 0 )
			{
				SavedBrushes.brushes.Add( new BrushPreset() );
			}

			if ( brushList.index >= SavedBrushes.brushes.Count || brushList.index < 0 )
			{
				brushList.index = 0;
			}
		}

		public void OnDisable()
		{
			SceneView.onSceneGUIDelegate -= OnSceneGUI;
			Undo.undoRedoPerformed -= Repaint;
			Cursor.visible = true;
			Tools.hidden = false;
		}

		public void OnDestroy()
		{
			SceneView.onSceneGUIDelegate -= OnSceneGUI;
			Undo.undoRedoPerformed -= Repaint;
			Cursor.visible = true;
			Tools.hidden = false;
		}

		private void OnGUI()
		{
			var current = Event.current;
			if ( current.type == EventType.KeyDown && current.keyCode == KeyCode.Escape )
			{
				toolEnabled = !toolEnabled;
				Repaint();
			}

			// Rects
			Rect windowPos = position;
			windowPos.x = 0; windowPos.y = 0;

			Rect leftRect = windowPos;
			leftRect.width = Mathf.Ceil( leftRect.width * ( panelAnim.faded * 0.33f ) );
			leftRect.y += EditorGUIUtility.singleLineHeight;

			Rect rightRect = windowPos;
			rightRect.width = Mathf.Ceil( rightRect.width * ( 1 - ( panelAnim.faded * 0.33f ) ) );
			rightRect.x = leftRect.xMax;
			rightRect.y += EditorGUIUtility.singleLineHeight;

			EditorGUI.DrawRect( leftRect, Settings.Get<ToolsSettings_General>().shadedBackgroundColor );
			//EditorGUI.DrawRect( rightRect, Colors.Aero );

			// Topbar
			EditorGUILayout.BeginHorizontal( EditorStyles.toolbar );
			panelAnim.target = GUILayout.Toggle( panelAnim.target, "Brushes", EditorStyles.toolbarButton );
			toolEnabled = GUILayout.Toggle( toolEnabled, "Enable (Esc)", EditorStyles.toolbarButton );
			GUILayout.FlexibleSpace();
			EditorGUILayout.EndHorizontal();

			//GUILayout.BeginArea(leftRect)
			using ( new GUILayout.AreaScope( leftRect ) )
			{
				using ( var scroll = new EditorGUILayout.ScrollViewScope( presetsScroll, GUIStyle.none, GUI.skin.verticalScrollbar ) )
				{
					presetsScroll = scroll.scrollPosition;

					brushList.DoLayoutList();
				}
			}

			using ( new GUILayout.AreaScope( rightRect ) )
			{
				if ( SavedBrushes.brushes.Count == 0 )
				{
					SavedBrushes.brushes.Add( new BrushPreset() );
				}

				if ( brushList.index >= SavedBrushes.brushes.Count || brushList.index < 0 )
				{
					brushList.index = 0;
				}

				GUILayout.Space( 4 );
				SavedBrushes.brushes[brushList.index].OnGUI( this );
			}
		}

		// SCENE GUI //

		void OnSceneGUI( SceneView view )
		{
			Event current = Event.current;
			// Check that the tool is enabled
			if ( current.type == EventType.KeyDown && current.keyCode == KeyCode.Escape )
			{
				toolEnabled = !toolEnabled;
				Repaint();
			}

			if ( !toolEnabled )
			{
				Cursor.visible = true;
				Tools.hidden = false;
				return;
			}

			Vector2 mousePos = current.mousePosition;
			Rect viewRect = view.position;
			viewRect.y += EditorGUIUtility.singleLineHeight;

			// Check that the mouse is in the scene view
			if ( viewRect.Contains( GUIUtility.GUIToScreenPoint( mousePos ) ) && EditorWindow.focusedWindow == SceneView.lastActiveSceneView )
			{
				Cursor.visible = false;
				Tools.hidden = true;
			}
			else
			{
				Cursor.visible = true;
				Tools.hidden = true;
				return;
			}

			// Capture focus
			int ctrlID = GUIUtility.GetControlID( FocusType.Keyboard );
			if ( current.type == EventType.Layout )
			{
				HandleUtility.AddDefaultControl( ctrlID );
			}

			// Input checks
			if ( current.type == EventType.ScrollWheel && current.modifiers == EventModifiers.Alt )
			{
				SavedBrushes.brushes[brushList.index].brushRadius += current.delta.y / 2.5f;

				current.Use();
			}

			// Do Brush
			UpdateBrush( ctrlID, mousePos );
			PaintObjects( current );
		}

		public void ChangeSelected( int index )
		{
			brushList.index = index;
		}

		// BRUSH METHODS //

		private void UpdateBrush( int ctrlID, Vector2 mousePos )
		{
			mousePos.y = Camera.current.pixelHeight - mousePos.y;
			Ray ray = Camera.current.ScreenPointToRay( mousePos );

			if ( Physics.Raycast( ray, out brushHit, Mathf.Infinity, SavedBrushes.brushes[brushList.index].layerMask, QueryTriggerInteraction.Ignore ) )
			{
				Handles.color = Colors.DolphinGray.WithAlpha( 0.5f );
				Handles.CircleHandleCap( ctrlID, brushHit.point + ( brushHit.normal * 0.1f ), Quaternion.LookRotation( brushHit.normal ), SavedBrushes.brushes[brushList.index].brushRadius, EventType.Repaint );
				Handles.DrawLine( brushHit.point, brushHit.point + ( brushHit.normal * 10 ) );

				Handles.color = Colors.GuppieGreen;
				Handles.zTest = UnityEngine.Rendering.CompareFunction.LessEqual;
				Handles.CircleHandleCap( ctrlID, brushHit.point + ( brushHit.normal * 0.1f ), Quaternion.LookRotation( brushHit.normal ), SavedBrushes.brushes[brushList.index].brushRadius, EventType.Repaint );
				Handles.DrawLine( brushHit.point, brushHit.point + ( brushHit.normal * 10 ) );

				Cursor.visible = false;
				SceneView.RepaintAll();
				Handles.zTest = UnityEngine.Rendering.CompareFunction.Always;
			}
			else
			{
				Cursor.visible = true;
			}
		}

		private void PaintObjects( Event current )
		{
			// Checks
			if ( current.type == EventType.Repaint ) return;
			if ( SavedBrushes.brushes[brushList.index].prefabs.Count == 0 ) return;
			if ( !mouseIsDown && current.type == EventType.MouseDown && current.modifiers == EventModifiers.None && current.button == 0 )
			{
				lastCollider = brushHit.collider;
				mouseIsDown = true;
			}
			if ( mouseIsDown && current.type == EventType.MouseUp )
			{
				lastCollider = null;
				mouseIsDown = false;
			}
			if ( !mouseIsDown || brushHit.collider == null ) return;

			// Exit if delay is not up yet.
			if ( placeTimeStamp >= EditorApplication.timeSinceStartup ) return;
			float rate = SavedBrushes.brushes[brushList.index].brushRate;
			placeTimeStamp = EditorApplication.timeSinceStartup + ( ( 60d / (double)rate ) / 60d );

			// Calculate direction from center to random angle.
			Vector3 dir = ( Quaternion.AngleAxis( Random.Range( 0f, 360f ), brushHit.normal ) * Vector3.Cross( brushHit.normal, Vector3.right ) ).normalized;
			// Calculate random range from center to radius.
			float range = Random.Range( 0, SavedBrushes.brushes[brushList.index].brushRadius );
			// Construct selected location from direction + range.
			Vector3 selectPos = brushHit.point + ( dir * range );

			// Align this point to local surface or abort if nothing was hit.
			Ray upRay = new Ray( selectPos, Vector3.up );
			Ray downRay = new Ray( selectPos, Vector3.down );
			Ray safeRay = new Ray( selectPos + ( Vector3.up * 0.01f ), Vector3.down );
			Ray normalRay = new Ray( selectPos + ( brushHit.normal * 0.01f ), -brushHit.normal );
			RaycastHit hitToSurface;
			bool oldQuery = Physics.queriesHitBackfaces;
			Physics.queriesHitBackfaces = true;

			int layers = SavedBrushes.brushes[brushList.index].layerMask;
			if ( !Physics.Raycast( upRay, out hitToSurface, Mathf.Infinity, layers, QueryTriggerInteraction.Ignore )
			  && !Physics.Raycast( downRay, out hitToSurface, Mathf.Infinity, layers, QueryTriggerInteraction.Ignore )
			  && !Physics.Raycast( safeRay, out hitToSurface, Mathf.Infinity, layers, QueryTriggerInteraction.Ignore )
			  && !Physics.Raycast( normalRay, out hitToSurface, Mathf.Infinity, layers, QueryTriggerInteraction.Ignore ) )
			{
				return;
			}

			if ( SavedBrushes.brushes[brushList.index].colliderStrict && hitToSurface.collider != lastCollider ) return;

			Physics.queriesHitBackfaces = oldQuery;

			if ( SavedBrushes.brushes[brushList.index].cullEnabled )
			{
				float angle = Vector3.Angle( SavedBrushes.brushes[brushList.index].cullRef, hitToSurface.normal );

				if ( !SavedBrushes.brushes[brushList.index].cullInvert && angle > SavedBrushes.brushes[brushList.index].cullAngle )
				{
					return;
				}
				if ( SavedBrushes.brushes[brushList.index].cullInvert && angle < SavedBrushes.brushes[brushList.index].cullAngle )
				{
					return;
				}
			}

			BrushPrefabSettings pick = PickObject();

			if ( pick.paintObject == null )
			{
				Debug.LogWarning( "[ObjectPainter] The Prefab is null !" );
				return;
			}

			// Generate new object.
			float objScale = Random.Range( pick.objectRandomScale.x, pick.objectRandomScale.y );
			float objRot = Random.Range( pick.objectRandomRotation.x, pick.objectRandomRotation.y );

			GameObject newObj = PrefabUtility.InstantiatePrefab( (Object)pick.paintObject ) as GameObject;

			newObj.transform.position = hitToSurface.point;

			switch ( pick.alignMode )
			{
				default:
				case BrushPrefabSettings.AlignMode.Up:
					newObj.transform.up = hitToSurface.normal;
					if ( pick.alignToPath ) newObj.transform.forward = brushHit.point - lastHitPoint;
					break;
				case BrushPrefabSettings.AlignMode.Down:
					newObj.transform.up = -hitToSurface.normal;
					if ( pick.alignToPath ) newObj.transform.forward = brushHit.point - lastHitPoint;
					break;
				case BrushPrefabSettings.AlignMode.Left:
					newObj.transform.right = -hitToSurface.normal;
					if ( pick.alignToPath ) newObj.transform.forward = brushHit.point - lastHitPoint;
					break;
				case BrushPrefabSettings.AlignMode.Right:
					newObj.transform.right = hitToSurface.normal;
					if ( pick.alignToPath ) newObj.transform.forward = brushHit.point - lastHitPoint;
					break;
				case BrushPrefabSettings.AlignMode.Forward:
					newObj.transform.forward = hitToSurface.normal;
					if ( pick.alignToPath ) newObj.transform.up = brushHit.point - lastHitPoint;
					break;
				case BrushPrefabSettings.AlignMode.Backward:
					newObj.transform.forward = -hitToSurface.normal;
					if ( pick.alignToPath ) newObj.transform.up = brushHit.point - lastHitPoint;
					break;
			}

			newObj.transform.localScale = new Vector3( objScale, objScale, objScale );
			newObj.transform.RotateAround( newObj.transform.position, hitToSurface.normal, objRot );
			lastHitPoint = brushHit.point;

			Undo.RegisterCreatedObjectUndo( newObj, "Object Painter" );

			//current.Use();
		}

		private BrushPrefabSettings PickObject()
		{
			BrushPrefabSettings currentPick = SavedBrushes.brushes[brushList.index].prefabs[0];
			float currentWeight = Random.Range( 0f, 1f ) * currentPick.weight;

			for ( int i = 0 ; i < SavedBrushes.brushes[brushList.index].prefabs.Count ; i++ )
			{
				float weight = Random.Range( 0f, 1f ) * SavedBrushes.brushes[brushList.index].prefabs[i].weight;
				if ( weight > currentWeight )
				{
					currentWeight = weight;
					currentPick = SavedBrushes.brushes[brushList.index].prefabs[i];
				}
			}

			return currentPick;
		}
	}
}