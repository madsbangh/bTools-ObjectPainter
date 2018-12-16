using UnityEngine;
using UnityEditor;
using UnityEditor.AnimatedValues;
using UnityEditorInternal;

namespace bTools.ObjectPainter
{
	public class ObjectPainterWindow : EditorWindow
	{
		#region Properties
		// GUI
		private Vector2 presetsScroll;
		private AnimBool brushPanelAnim;
		private ReorderableList brushList;
		public ReorderableList BrushList
		{
			get
			{
				if (brushList == null)
				{
					brushList = new ReorderableList(SavedBrushes.brushes, typeof(BrushPreset), false, true, true, true);
					brushList.drawHeaderCallback = (Rect rect) =>
					{
						EditorGUI.LabelField(rect, "Brushes");
						rect.x = rect.xMax - 32;
						rect.width = 16;
						if (GUI.Button(rect, EditorGUIUtility.FindTexture("d_Toolbar Plus"), GUIStyle.none))
						{
							SavedBrushes.brushes.Add(new BrushPreset());
							brushList.index = SavedBrushes.brushes.Count - 1;
							brushList.GrabKeyboardFocus();
							presetsScroll.y = float.MaxValue;
						}
						rect.x += 16;

						if (GUI.Button(rect, EditorGUIUtility.FindTexture("d_Toolbar Minus"), GUIStyle.none))
						{
							if (brushList.index >= 0 && brushList.index <= SavedBrushes.brushes.Count - 1)
							{
								Undo.RecordObject(SavedBrushes, "Removed Brush Preset");
								SavedBrushes.brushes.RemoveAt(brushList.index);
								brushList.index = Mathf.Max(0, brushList.index - 1);
								brushList.GrabKeyboardFocus();
							}
						}
					};

					brushList.drawElementCallback = (Rect rect, int index, bool isActive, bool isFocused) =>
					{
						EditorGUI.LabelField(rect, SavedBrushes.brushes[index].brushName);
					};
				}

				if (brushList.index >= SavedBrushes.brushes.Count || brushList.index < 0)
				{
					brushList.index = 0;
				}

				return brushList;
			}
		}

		// Tool
		private RaycastHit brushHit;
		private Vector3 lastHitPoint = Vector3.zero;
		private Collider lastCollider;
		private bool toolEnabled;
		private bool mouseIsDown;
		private double placeTimeStamp;

		// Data
		private SavedBrushes m_savedBrushes;
		private Transform parent;

		public SavedBrushes SavedBrushes
		{
			get
			{
				if (m_savedBrushes == null)
				{
					m_savedBrushes = ObjectPainterResources.LoadSavedBrushes();
				}

				if (m_savedBrushes.brushes.Count == 0)
				{
					m_savedBrushes.brushes.Add(new BrushPreset());
				}

				EditorUtility.SetDirty(m_savedBrushes);
				return m_savedBrushes;
			}
		}
		#endregion

		[MenuItem("bTools/ObjectPainter")]
		static void Init()
		{
			var window = GetWindow<ObjectPainterWindow>(string.Empty, true);

			window.titleContent = new GUIContent("ObjectPainter");
			window.minSize = new Vector2(220, 100);
		}

		// INSPECTOR GUI //
		private void OnEnable()
		{
			SceneView.onSceneGUIDelegate += OnSceneGUI;
			Undo.undoRedoPerformed += Repaint;
			brushPanelAnim = new AnimBool(false, Repaint);
		}

		private void OnDisable()
		{
			Cleanup();
		}

		private void OnDestroy()
		{
			Cleanup();
		}

		private void Cleanup()
		{
			SceneView.onSceneGUIDelegate -= OnSceneGUI;
			Undo.undoRedoPerformed -= Repaint;
			Cursor.visible = true;
			Tools.hidden = false;
		}

		private void OnGUI()
		{
			var current = Event.current;
			if (current.type == EventType.KeyDown && current.keyCode == KeyCode.Tab)
			{
				toolEnabled = !toolEnabled;
				if (toolEnabled) SceneView.lastActiveSceneView.Focus();
				Repaint();
			}

			// Setup rects
			Rect windowPos = position;
			windowPos.x = 0;
			windowPos.y = 0;
			windowPos.width -= 4;

			// Brush preset list rect
			Rect leftRect = windowPos;
			leftRect.width = Mathf.Ceil(leftRect.width * (brushPanelAnim.faded * 0.3333f));
			leftRect.xMin += 2;
			leftRect.y += EditorGUIUtility.singleLineHeight + 4;

			// Brush settings list
			Rect rightRect = windowPos;
			rightRect.width = Mathf.Ceil(rightRect.width * (1 - (brushPanelAnim.faded * 0.3333f)));
			rightRect.x = leftRect.xMax + 2;
			rightRect.y += EditorGUIUtility.singleLineHeight;

			// Toolbar
			EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
			brushPanelAnim.target = GUILayout.Toggle(brushPanelAnim.target, "Brushes", EditorStyles.toolbarButton);
			toolEnabled = GUILayout.Toggle(toolEnabled, "Enable (Tab)", EditorStyles.toolbarButton);
			GUILayout.FlexibleSpace();
			EditorGUILayout.EndHorizontal();

			// Brush list
			using (new GUILayout.AreaScope(leftRect))
			{
				using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
				{
					using (var scroll = new EditorGUILayout.ScrollViewScope(presetsScroll, GUIStyle.none, GUI.skin.verticalScrollbar))
					{
						presetsScroll = scroll.scrollPosition;

						BrushList.DoLayoutList();
					}
				}
			}

			// Brush settings
			using (new GUILayout.AreaScope(rightRect))
			{
				GUILayout.Space(4);
				using (new GUILayout.VerticalScope(EditorStyles.helpBox))
				{
					float lastLabelWidth = EditorGUIUtility.labelWidth;
					EditorGUIUtility.labelWidth = 60;

					parent = EditorGUILayout.ObjectField("Parent", parent, typeof(Transform), true) as Transform;

					EditorGUIUtility.labelWidth = lastLabelWidth;

					if (parent != null && parent.gameObject.scene.name == null)
					{
						EditorGUILayout.HelpBox("Only scene objects can be set as parent for newly painted objects", MessageType.Error);
					}
				}

				if (SavedBrushes.brushes.Count == 0)
				{
					SavedBrushes.brushes.Add(new BrushPreset());
				}

				if (BrushList.index >= SavedBrushes.brushes.Count || BrushList.index < 0)
				{
					BrushList.index = 0;
				}

				GUILayout.Space(4);
				SavedBrushes.brushes[BrushList.index].OnGUI(this);
			}
		}

		// SCENE GUI //
		void OnSceneGUI(SceneView view)
		{
			Event current = Event.current;
			if (current.type == EventType.KeyDown && current.keyCode == KeyCode.Tab)
			{
				toolEnabled = !toolEnabled;
				Repaint();
			}

			Tools.hidden = false;
			Cursor.visible = true;

			if (!toolEnabled) return;
			if (EditorApplication.isPlayingOrWillChangePlaymode) return;
			if (GUIUtility.hotControl != 0) return;

			HandleUtility.AddDefaultControl(GUIUtility.GetControlID(GUIUtility.GetControlID("ObjectPainter".GetHashCode(), FocusType.Keyboard), FocusType.Keyboard));
			Tools.hidden = true;

			Vector2 mousePos = current.mousePosition;
			Rect viewRect = view.position;
			viewRect.y += EditorGUIUtility.singleLineHeight;

			// Check that the mouse is in the scene view
			if (viewRect.Contains(GUIUtility.GUIToScreenPoint(mousePos)) && EditorWindow.focusedWindow == SceneView.lastActiveSceneView)
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
			int ctrlID = GUIUtility.GetControlID(FocusType.Keyboard);
			if (current.type == EventType.Layout)
			{
				HandleUtility.AddDefaultControl(ctrlID);
			}

			// Input checks
			if (current.type == EventType.ScrollWheel && current.modifiers == EventModifiers.Alt)
			{
				SavedBrushes.brushes[BrushList.index].brushRadius += current.delta.y / 2.5f;

				current.Use();
			}

			// Do Brush
			DrawBrush(ctrlID, mousePos);
			PaintObjects(current);
		}

		// BRUSH METHODS //
		private BrushPrefabSettings ChooseRandomPrefab()
		{
			BrushPrefabSettings currentPick = SavedBrushes.brushes[BrushList.index].prefabs[0];
			float currentWeight = Random.Range(0f, 1f) * currentPick.weight;

			for (int i = 0; i < SavedBrushes.brushes[BrushList.index].prefabs.Count; i++)
			{
				float weight = Random.Range(0f, 1f) * SavedBrushes.brushes[BrushList.index].prefabs[i].weight;
				if (weight > currentWeight)
				{
					currentWeight = weight;
					currentPick = SavedBrushes.brushes[BrushList.index].prefabs[i];
				}
			}

			return currentPick;
		}

		private void DrawBrush(int ctrlID, Vector2 mousePos)
		{
			mousePos.y = Camera.current.pixelHeight - mousePos.y;
			Ray ray = Camera.current.ScreenPointToRay(mousePos);

			if (Physics.Raycast(ray, out brushHit, Mathf.Infinity, SavedBrushes.brushes[BrushList.index].layerMask, QueryTriggerInteraction.Ignore))
			{
				Handles.color = ObjectPainterResources.BrushColorHidden;
				Handles.CircleHandleCap(ctrlID, brushHit.point + (brushHit.normal * 0.1f), Quaternion.LookRotation(brushHit.normal), SavedBrushes.brushes[BrushList.index].brushRadius, EventType.Repaint);
				Handles.DrawLine(brushHit.point, brushHit.point + (brushHit.normal * 10));

				Handles.color = ObjectPainterResources.BrushColor;
				Handles.zTest = UnityEngine.Rendering.CompareFunction.LessEqual;
				Handles.CircleHandleCap(ctrlID, brushHit.point + (brushHit.normal * 0.1f), Quaternion.LookRotation(brushHit.normal), SavedBrushes.brushes[BrushList.index].brushRadius, EventType.Repaint);
				Handles.DrawLine(brushHit.point, brushHit.point + (brushHit.normal * 10));

				Cursor.visible = false;
				SceneView.RepaintAll();
				Handles.zTest = UnityEngine.Rendering.CompareFunction.Always;
			}
			else
			{
				Cursor.visible = true;
			}
		}

		private void PaintObjects(Event current)
		{
			// Checks
			if (current.type == EventType.Repaint) return;
			if (SavedBrushes.brushes[BrushList.index].prefabs.Count == 0) return;
			if (!mouseIsDown && current.type == EventType.MouseDown && current.modifiers == EventModifiers.None && current.button == 0)
			{
				lastCollider = brushHit.collider;
				mouseIsDown = true;
			}
			if (mouseIsDown && current.type == EventType.MouseUp)
			{
				lastCollider = null;
				mouseIsDown = false;
			}
			if (!mouseIsDown || brushHit.collider == null) return;

			// Exit if delay is not up yet.
			if (placeTimeStamp >= EditorApplication.timeSinceStartup) return;

			// Calculate direction from center to random angle.
			Vector3 dir = (Quaternion.AngleAxis(Random.Range(0f, 360f), brushHit.normal) * Vector3.Cross(brushHit.normal, Vector3.right)).normalized;
			// Calculate random range from center to radius.
			float range = Random.Range(0, SavedBrushes.brushes[BrushList.index].brushRadius);
			// Construct selected location from direction + range.
			Vector3 selectPos = brushHit.point + (dir * range);

			// Align this point to local surface or abort if nothing was hit.
			Ray upRay = new Ray(selectPos, Vector3.up);
			Ray downRay = new Ray(selectPos, Vector3.down);
			Ray safeRay = new Ray(selectPos + (Vector3.up * 0.01f), Vector3.down);
			Ray normalRay = new Ray(selectPos + (brushHit.normal * 0.01f), -brushHit.normal);
			RaycastHit hitToSurface;
			bool oldQuery = Physics.queriesHitBackfaces;
			Physics.queriesHitBackfaces = true;

			int layers = SavedBrushes.brushes[BrushList.index].layerMask;
			if (!Physics.Raycast(upRay, out hitToSurface, Mathf.Infinity, layers, QueryTriggerInteraction.Ignore)
			  && !Physics.Raycast(downRay, out hitToSurface, Mathf.Infinity, layers, QueryTriggerInteraction.Ignore)
			  && !Physics.Raycast(safeRay, out hitToSurface, Mathf.Infinity, layers, QueryTriggerInteraction.Ignore)
			  && !Physics.Raycast(normalRay, out hitToSurface, Mathf.Infinity, layers, QueryTriggerInteraction.Ignore))
			{
				return;
			}

			if (SavedBrushes.brushes[BrushList.index].colliderStrict && hitToSurface.collider != lastCollider) return;

			Physics.queriesHitBackfaces = oldQuery;

			if (SavedBrushes.brushes[BrushList.index].cullEnabled)
			{
				float angle = Vector3.Angle(SavedBrushes.brushes[BrushList.index].cullRef, hitToSurface.normal);

				if (!SavedBrushes.brushes[BrushList.index].cullInvert && angle > SavedBrushes.brushes[BrushList.index].cullAngle)
				{
					return;
				}
				if (SavedBrushes.brushes[BrushList.index].cullInvert && angle < SavedBrushes.brushes[BrushList.index].cullAngle)
				{
					return;
				}
			}

			BrushPrefabSettings pick = ChooseRandomPrefab();

			if (pick.paintObject == null)
			{
				Debug.LogWarning("[ObjectPainter] The Prefab is null !");
				return;
			}

			// Generate new object.
			float objScale = Random.Range(pick.objectRandomScale.x, pick.objectRandomScale.y);
			float objRot = Random.Range(pick.objectRandomRotation.x, pick.objectRandomRotation.y);

			GameObject newObj = PrefabUtility.InstantiatePrefab(pick.paintObject) as GameObject;

			// Set parent
			if (parent)
			{
				newObj.transform.parent = parent;
			}

			newObj.transform.position = hitToSurface.point;

			switch (pick.alignMode)
			{
				default:
				case BrushPrefabSettings.AlignMode.Up:
					newObj.transform.up = hitToSurface.normal;
					if (pick.alignToPath) newObj.transform.forward = brushHit.point - lastHitPoint;
					break;
				case BrushPrefabSettings.AlignMode.Down:
					newObj.transform.up = -hitToSurface.normal;
					if (pick.alignToPath) newObj.transform.forward = brushHit.point - lastHitPoint;
					break;
				case BrushPrefabSettings.AlignMode.Left:
					newObj.transform.right = -hitToSurface.normal;
					if (pick.alignToPath) newObj.transform.forward = brushHit.point - lastHitPoint;
					break;
				case BrushPrefabSettings.AlignMode.Right:
					newObj.transform.right = hitToSurface.normal;
					if (pick.alignToPath) newObj.transform.forward = brushHit.point - lastHitPoint;
					break;
				case BrushPrefabSettings.AlignMode.Forward:
					newObj.transform.forward = hitToSurface.normal;
					if (pick.alignToPath) newObj.transform.up = brushHit.point - lastHitPoint;
					break;
				case BrushPrefabSettings.AlignMode.Backward:
					newObj.transform.forward = -hitToSurface.normal;
					if (pick.alignToPath) newObj.transform.up = brushHit.point - lastHitPoint;
					break;
				case BrushPrefabSettings.AlignMode.None:
					if (pick.alignToPath) newObj.transform.forward = brushHit.point - lastHitPoint;
					break;
			}

			newObj.transform.localScale = new Vector3(objScale, objScale, objScale);
			newObj.transform.RotateAround(newObj.transform.position, hitToSurface.normal, objRot);
			lastHitPoint = brushHit.point;

			Undo.RegisterCreatedObjectUndo(newObj, "Object Painter");
			float rate = SavedBrushes.brushes[BrushList.index].brushRate;
			placeTimeStamp = EditorApplication.timeSinceStartup + ((60d / rate) / 60d);
		}
	}
}