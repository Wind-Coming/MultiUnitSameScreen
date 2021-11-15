
using UnityEngine;
using UnityEditor;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;


namespace nTools.PrefabPainter
{
	//
	// class PrefabPainter
	//

	public class PrefabPainter : EditorWindow
	{
        enum PaintTool
        {
            None         = -1,
            Paint        = 0,
            Erase        = 1,
            Settings     = 2,
        }

        bool        onMouseDown = false;
        bool        onMouseUp = false;
        bool        onDrawing = false;
        EventModifiers eventModifiers = EventModifiers.None;

        bool         onPrecisePlace = false;
        RaycastHitEx preciseHitInfo;
        GameObject   preciseGameObject;
        Vector3      preciseScaleFactor;
        Quaternion   precisePlaceOrienation;


        RaycastHitEx  hitInfo;
        GameObject    hitObject;
        RaycastHitEx  prevHitInfo;
        float         dragDistance;
        Vector3       strokeDirection;
		GameObject    lastObjectInStroke; // last placed object in current paint stroke
		Plane         lastObjectInStrokePlacePlane;

        Vector2     prevMousePos;
        Ray         prevRay;

        GameObject[] placedObjects = new GameObject[4096];  // objects placed in current paint operation, used in ignoge list for PickGameObject()
        int          placedObjectsCount = 0;

        GameObject[] allSceneObjects = null;

        PrefabPainterSceneSettings sceneSettings;
        string                     currentScene;  // scene path
        PrefabPainterSettings settings;     
        SerializedObject setting_sobj;
        SerializedProperty setting_prop_lodBrushSize;

        PaintTool             currentTool = PaintTool.None;
        static string[]       toolNames = { "Paint", "Erase", "Settings" };



        Vector2     scrollPos;
        Vector2     presetsScrollPos;
        Texture2D   logoTexture = null;

        float lastRepaintTime = 0;


		GenericMenu presetContextMenu;
		GenericMenu multiPresetContextMenu;


        // Foldouts
        bool brushSettingsFoldout = true;
        bool positionSettingsFoldout = true;
        bool orientationSettingsFoldout = true;
        bool scaleSettingsFoldout = true;
        bool commonSettingsFoldout = true;



        // Selected objects 

        UnityEngine.Object[]  _selectedObjects = null;
        List<GameObject>      _selectedGameObjects = null;


		static PrefabPainter       activeWindow;


        bool EnumerateChilds(Transform transform, Func<GameObject, bool> func)
        {
            if (transform == null)
                return true;
            
            if (!func(transform.gameObject))
                return false;
            
            for (int i = 0; i < transform.childCount; i++)
            {
                if (!EnumerateChilds(transform.GetChild(i), func))
                    return false;
            }
            return true;
        }


        UnityEngine.Object[]  selectedObjects
        {
            get
            {
                return _selectedObjects;
            }

            set
            {
                _selectedObjects = value;

                if (_selectedObjects == null)
                {
                    _selectedGameObjects = null;
                }
                else
                {
                    _selectedGameObjects = new List<GameObject>(_selectedObjects.Length);

                    foreach(UnityEngine.Object obj in _selectedObjects) {
                        if (obj is GameObject)
                        {
                            EnumerateChilds((obj as GameObject).transform, (gameObject) => {
                                _selectedGameObjects.Add(gameObject);
                                return true;
                            });
                        }
                    }
                }
            }
        }




        static string helpText =             
            "Info:\n" +
            "    Add prefabs - drag it to prefab window\n" +
            "    Shift+drag - relink prefab\n" +
            "\n" +
            "Paint:\n" +
            "    Click \"Paint\" button to begin paint\n" +
            "    Click and drag on surface to place prefabs\n" +
			"    F - Frame camera on brush\n" +
            "\n" +
            "Shortcuts:\n" +
            "    Shift+Click\t- Precise place\n" +
            "    Ctrl+Shift+Click\t- Precise place with angle snap\n" +
            "    []\t\t- Change brush size\n" +
            "    ESC\t\t- Abort paint\n";








		// Unity Editor Menu Item
		[MenuItem ("Window/nTools/Prefab Painter")]
		static void Init ()
		{
			// Get existing open window or if none, make a new one:
			PrefabPainter window = (PrefabPainter)EditorWindow.GetWindow (typeof (PrefabPainter));
            window.ShowUtility(); 
		}




        void LoadSettings()
        {
			const string settingsFolderName = "Settings";
			const string settingsFileName = "PrefabPainterSettings.asset";
			const string defaultSettingsFileName = "DefaultPrefabPainterSettings.asset";


            MonoScript ownerScript;
            string ownerPath;            
			string settingsFolderPath = "Assets/nTools/PrefabPainter/Settings/"; // setup default path to settings file if we can't get it at runtime


			// get path to settings file based on PrefabPainter.cs script path. 
			// get PrefabPainter.cs script 
            if ((ownerScript = MonoScript.FromScriptableObject(this)) != null)
            {                
				// get PrefabPainter.cs script path
                if((ownerPath = AssetDatabase.GetAssetPath(ownerScript)) != null)
                {
                    // get path to PrefabPainter.cs
                    ownerPath = Path.GetDirectoryName(ownerPath);
					// get path to Editor
                    ownerPath = Path.GetDirectoryName(ownerPath);
					// get path to Scripts
                    ownerPath = Path.GetDirectoryName(ownerPath);

					settingsFolderPath = Path.Combine(ownerPath, settingsFolderName);

					if (!AssetDatabase.IsValidFolder(settingsFolderPath))
						AssetDatabase.CreateFolder(ownerPath, settingsFolderName);
                }

            }


			// Try load settings asset
			settings = AssetDatabase.LoadAssetAtPath(Path.Combine(settingsFolderPath, settingsFileName), typeof(PrefabPainterSettings)) as PrefabPainterSettings;
            if (settings == null)
            {
				// if no settings file, try load default settings file
				settings = AssetDatabase.LoadAssetAtPath(Path.Combine(settingsFolderPath, defaultSettingsFileName), typeof(PrefabPainterSettings)) as PrefabPainterSettings;
				if (settings != null)
				{
					// Duplicate
					settings = Instantiate(settings);

					// Save as settingsFileName
					AssetDatabase.CreateAsset(settings, Path.Combine(settingsFolderPath, settingsFileName));
				}
				else
					// if no default settings file - create new instance
				{
					settings = ScriptableObject.CreateInstance<PrefabPainterSettings>();

					// Save as settingsFileName
					AssetDatabase.CreateAsset(settings, Path.Combine(settingsFolderPath, settingsFileName));
				}
            }
            setting_sobj = new SerializedObject(settings);
            setting_prop_lodBrushSize = setting_sobj.FindProperty("LodBrushSize");
        }



        void LoadSceneSettings()
        {
            const string settingsObjectName = "PrefabPainterSceneSettings";

            GameObject gameObject = GameObject.Find(settingsObjectName);
            if (gameObject == null) {
                gameObject = new GameObject(settingsObjectName);
                gameObject.hideFlags = HideFlags.HideInHierarchy|HideFlags.HideInInspector|HideFlags.DontSaveInBuild;
                Utility.MarkActiveSceneDirty();
            }           

            sceneSettings = gameObject.GetComponent<PrefabPainterSceneSettings>();
            if (sceneSettings == null) {
                sceneSettings = gameObject.AddComponent<PrefabPainterSceneSettings>();
				gameObject.hideFlags = HideFlags.HideInHierarchy|HideFlags.HideInInspector|HideFlags.DontSaveInBuild;
                Utility.MarkActiveSceneDirty();
            }
        }




		void OnEnable () 
		{
            hideFlags = HideFlags.HideAndDontSave;
#if (UNITY_5_0)
            title = "Prefab Painter";
#else
            titleContent = new GUIContent("Prefab Painter", "Prefab placement tool");
#endif

			activeWindow = this;

            if (EditorGUIUtility.isProSkin)
                logoTexture = Resources.Load("prefabpainter_logo_bskin") as Texture2D;
            else
                logoTexture = Resources.Load("prefabpainter_logo_wskin") as Texture2D;



			// Init context menus
			presetContextMenu = new GenericMenu();
			presetContextMenu.AddItem(new GUIContent("Reveal in Project"), false, ContextMenuCallback, new Action(RevealPrefabInProject));
			presetContextMenu.AddSeparator ("");
			presetContextMenu.AddItem(new GUIContent("Delete"), false, ContextMenuCallback, new Action(() => settings.DeleteSelectedPresets()));
			presetContextMenu.AddItem(new GUIContent("Duplicate"), false, ContextMenuCallback, new Action(() => settings.DuplicateSelectedPresets()));
			presetContextMenu.AddItem(new GUIContent("Reset"), false, ContextMenuCallback, new Action(() => settings.ResetSelectedPresets()));

			multiPresetContextMenu = new GenericMenu();
			multiPresetContextMenu.AddItem(new GUIContent("Delete"), false, ContextMenuCallback, new Action(() => settings.DeleteSelectedPresets()));
			multiPresetContextMenu.AddItem(new GUIContent("Duplicate"), false, ContextMenuCallback, new Action(() => settings.DuplicateSelectedPresets()));
			multiPresetContextMenu.AddItem(new GUIContent("Reset"), false, ContextMenuCallback, new Action(() => settings.ResetSelectedPresets()));



            // Setup callbacks
			//if(SceneView.duringSceneGui != OnSceneGUI)
				SceneView.duringSceneGui += OnSceneGUI;

            if (EditorApplication.update != EditorApplicationUpdateCallback)
                EditorApplication.update += EditorApplicationUpdateCallback;



#if (UNITY_5_0 || UNITY_5_1 || UNITY_5_2)
            currentScene = EditorApplication.currentScene;
#else
            currentScene = UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene().path;
#endif
            OnSceneChanged();
            

            if (EditorApplication.update != HierarchyWindowChangedCallback)
                EditorApplication.hierarchyChanged += HierarchyWindowChangedCallback;


            LoadSettings();
            LoadSceneSettings();


            brushSettingsFoldout = settings.brushSettingsFoldout;
            positionSettingsFoldout = settings.positionSettingsFoldout;
            orientationSettingsFoldout = settings.orientationSettingsFoldout;
            scaleSettingsFoldout = settings.scaleSettingsFoldout;
            commonSettingsFoldout = settings.commonSettingsFoldout;

            Spenve.MsgSystem.Instance.AddListener<int>("OnLodChanged", OnLodChange);
		}




		void OnDisable () 
		{
			activeWindow = null;

            settings.brushSettingsFoldout = brushSettingsFoldout;
            settings.positionSettingsFoldout = positionSettingsFoldout;
            settings.orientationSettingsFoldout = orientationSettingsFoldout;
            settings.scaleSettingsFoldout = scaleSettingsFoldout;
            settings.commonSettingsFoldout = commonSettingsFoldout;
            
            EditorUtility.SetDirty(settings);
            
			SceneView.duringSceneGui -= OnSceneGUI;
            EditorApplication.update -= EditorApplicationUpdateCallback;
            EditorApplication.hierarchyChanged -= HierarchyWindowChangedCallback;

            Spenve.MsgSystem.Instance.RemoveListener<int>("OnLodChanged", OnLodChange);
		}


        void OnLodChange(int lod)
        {
            settings.lodPresets.Clear();
            for(int i = 0; i < settings.presets.Count; i++)
            {
                if (lod < settings.presets[i].lodRange.x || lod > settings.presets[i].lodRange.y)
                    continue;
                settings.lodPresets.Add(settings.presets[i]);
            }
        }


        void HierarchyWindowChangedCallback()
        {
#if (UNITY_5_0 || UNITY_5_1 || UNITY_5_2)
            if (currentScene != EditorApplication.currentScene)
            {                
                currentScene = EditorApplication.currentScene;
                OnSceneChanged();
            }
#else
            if (currentScene != UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene().path)
            {                
                currentScene = UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene().path;
                OnSceneChanged();
            }
#endif            
        }


        void OnSceneChanged()
        {
            LoadSceneSettings();
        }


        void EditorApplicationUpdateCallback()
        {            
            if (Tools.current != Tool.None && currentTool == PaintTool.Paint)
            {
                currentTool = PaintTool.None;
                Selection.objects = selectedObjects;
                selectedObjects = null;
                Repaint();
            }

            if (Tools.current != Tool.None && currentTool == PaintTool.Erase)
            {
                currentTool = PaintTool.None;
                allSceneObjects = null;
                Repaint();
            }

            if(Time.realtimeSinceStartup - lastRepaintTime > 0.5f)
            {
                lastRepaintTime = Time.realtimeSinceStartup;
                Repaint();
            }
        }







        void HandleEvents()
        {
            Event e = Event.current;

			// Move tool - ESC
			if (e.type == EventType.KeyDown &&
			    e.keyCode == KeyCode.Escape &&
			    currentTool != PaintTool.None)
			{                    
				Tools.current = Tool.Move;
				currentTool = PaintTool.None;
				onMouseDown = false;
				onPrecisePlace = false;
				onDrawing = false;
				e.Use ();
				
				Repaint();
			}



			BrushPreset selectedPreset = settings.GetFirstSelectedPreset ();
			if (selectedPreset != null && !settings.HasMultipleSelectedPresets()) {

				if (currentTool == PaintTool.Paint || currentTool == PaintTool.Erase) {
					// Brush Size
					if (e.type == EventType.KeyDown &&
						e.keyCode == KeyCode.LeftBracket) {
						if (currentTool == PaintTool.Paint)
							selectedPreset.brushSize = Mathf.Min (Mathf.Max (0.05f, selectedPreset.brushSize - selectedPreset.brushSize * 0.1f), settings.brushSizeMax);
						else
							selectedPreset.eraseBrushSize = Mathf.Min (Mathf.Max (0.05f, selectedPreset.eraseBrushSize - selectedPreset.eraseBrushSize * 0.1f), settings.brushSizeMax);

						HandleUtility.Repaint ();
						e.Use ();

					}

					// Brush Size
					if (e.type == EventType.KeyDown &&
						e.keyCode == KeyCode.RightBracket) {
						if (currentTool == PaintTool.Paint)
							selectedPreset.brushSize += selectedPreset.brushSize * 0.1f;
						else
							selectedPreset.eraseBrushSize += selectedPreset.brushSize * 0.1f;

						HandleUtility.Repaint ();
						e.Use ();
					}
				}
			}
        }


		void ContextMenuCallback(object obj)
		{
			if (obj is Action)
				(obj as Action).Invoke();
		}



		void RevealPrefabInProject()
		{
			BrushPreset selectedPreset = settings.GetFirstSelectedPreset ();
			if (selectedPreset != null && selectedPreset.prefab != null)
			{
				EditorGUIUtility.PingObject(selectedPreset.prefab);
			}
		}



		void OnGUI ()
		{            
            Event e = Event.current;

			// Bold foldout style
			GUIStyle boldFoldout = new GUIStyle(EditorStyles.foldout);
			boldFoldout.fontStyle = FontStyle.Bold;



			// Close this window if new one created
			if (activeWindow != null && activeWindow != this)
				this.Close ();




            HandleEvents();




            // Draw Logo
            if (logoTexture != null)
            {
                EditorGUILayout.Space ();
                EditorGUILayout.Space ();

                Rect rect = EditorGUILayout.GetControlRect(GUILayout.Height(logoTexture.height));
                Rect textueRect = rect;
                textueRect.width = logoTexture.width;
                textueRect.height = logoTexture.height; 
                textueRect.center = rect.center;
                GUI.DrawTexture(textueRect, logoTexture);
            }




            EditorGUILayout.Space();



            // Tool select
            EditorGUILayout.BeginVertical ();
            EditorGUILayout.BeginHorizontal ();
            GUILayout.FlexibleSpace ();

            if (Tools.current != Tool.None && (currentTool == PaintTool.Paint || currentTool == PaintTool.Erase)) {
                Selection.objects = selectedObjects;
                selectedObjects = null;
                allSceneObjects = null;
                currentTool = PaintTool.None;
            }

            EditorGUI.BeginChangeCheck ();
            currentTool = (PaintTool)GUI.Toolbar(EditorGUILayout.GetControlRect(GUILayout.Width(200), GUILayout.Height(20)), (int)currentTool, toolNames);
            if (EditorGUI.EndChangeCheck())
            {
                if (currentTool == PaintTool.Paint)
                {                    
                    Tools.current = Tool.None;
                    selectedObjects = Selection.objects;
                    Selection.objects = new UnityEngine.Object[0];
                }
                if (currentTool == PaintTool.Erase)
                {                    
                    Tools.current = Tool.None;
                    selectedObjects = Selection.objects;
                    Selection.objects = new UnityEngine.Object[0];

                    allSceneObjects = GameObject.FindObjectsOfType<GameObject>();
                }
                if (currentTool == PaintTool.Settings)
                {
                    if (selectedObjects != null)
                    {
                        Selection.objects = selectedObjects;
                        selectedObjects = null;
                    }
                    allSceneObjects = null;
                }
            }


            GUILayout.FlexibleSpace ();
            EditorGUILayout.EndHorizontal ();
            EditorGUILayout.EndVertical ();



            
			bool hasSelectedPresets = settings.HasSelectedPresets();
			bool hasMultipleSelectedPresets = settings.HasMultipleSelectedPresets();
			BrushPreset selectedPreset = settings.GetFirstSelectedPreset();




			if (currentTool == PaintTool.None ||
			    currentTool == PaintTool.Paint ||
			    currentTool == PaintTool.Erase)
            {


				// Draw Presets Window
                {
                    Color32 colorBlue = new Color32 (62, 125, 231, 255);

                    GUIStyle iconTextStyle = new GUIStyle(EditorStyles.miniLabel);
                    iconTextStyle.alignment = TextAnchor.LowerCenter;

                    EditorGUILayout.Space(10);
                    settings.openLodInEditor = EditorGUILayout.Toggle ("编辑器开启Lod", settings.openLodInEditor);
                    
                    settings.showAll = EditorGUILayout.Toggle ("显示全部（或者lod）", settings.showAll);

                    int windowBorder = 2;
                    int presetIconWidth = 60;
                    int presetIconHeight = 72;

                    int presetRows = 2;
                    List<BrushPreset> presets = settings.showAll? settings.presets : settings.lodPresets;

                    int presetColumns = Mathf.Max(presets.Count / presetRows + 1, 1);

                    EditorGUILayout.LabelField ("Presets", EditorStyles.boldLabel);


                    Rect realRect = EditorGUILayout.GetControlRect(GUILayout.Height(presetIconHeight * presetRows + windowBorder*2 + 20) );

                    Rect virtualRect = realRect;
                    virtualRect.width = Mathf.Max(presetColumns * presetIconWidth + windowBorder*2, virtualRect.width);
                    virtualRect.height = virtualRect.height - 20;

                    presetsScrollPos = GUI.BeginScrollView(realRect, presetsScrollPos, virtualRect, true, false);

                    // draw presets window background
                    GUI.Label(virtualRect, "", "HelpBox");


					// Empty preset list - Drag&Drop Info
                    if (presets.Count == 0)
                    {
                        GUIStyle labelStyle = new GUIStyle(EditorStyles.boldLabel);
                        labelStyle.fontStyle = FontStyle.Bold;
                        labelStyle.alignment = TextAnchor.MiddleCenter;
                        EditorGUI.LabelField(realRect, "Drag & Drop Prefab Here", labelStyle);
                    }


                    virtualRect.xMax -= windowBorder;
                    virtualRect.xMin += windowBorder;
                    virtualRect.yMax -= windowBorder;
                    virtualRect.yMin += windowBorder;


                    int presetIndex = 0;
					int presetUnderCursor = -1;


                    for (int x = (int)virtualRect.xMin; x < (int)(virtualRect.xMax); x += presetIconWidth)
                    {
                        for (int y = (int)virtualRect.yMin; y < (int)virtualRect.yMax; y += presetIconHeight)
                        {
                            if (presetIndex >= presets.Count)
								break;

                            BrushPreset preset = presets[presetIndex];

                            Rect presetIconRect = new Rect(x, y, presetIconWidth, presetIconHeight);


							if(presetIconRect.Contains(e.mousePosition))
								presetUnderCursor = presetIndex;




                            // Draw selected Prefab preview blue rect
							if(preset.selected)
                                EditorGUI.DrawRect(presetIconRect, colorBlue);
							else 
								EditorGUI.DrawRect(new Rect(presetIconRect.x, presetIconRect.y, 0, 0), colorBlue);


                            Rect iconRect = new Rect(x+1, y+1, presetIconWidth-2, presetIconWidth-2);

                            // Prefab preview 
                            if (preset.prefabPreview != null) {                                
                                GUI.DrawTexture(iconRect, preset.prefabPreview);
                            }

                            if (preset.prefab != null) {
                                // Prefab name
                                EditorGUI.LabelField(presetIconRect, preset.name, iconTextStyle);
                            } else {

                                // Missing prefab

                                GUIStyle labelStyle = new GUIStyle(EditorStyles.boldLabel);
                                labelStyle.normal.textColor = Color.red;
                                labelStyle.alignment = TextAnchor.LowerCenter;
                                EditorGUI.LabelField(presetIconRect, "Missing", labelStyle);

                                labelStyle = new GUIStyle(EditorStyles.miniLabel);
                                labelStyle.alignment = TextAnchor.MiddleCenter;
                                EditorGUI.LabelField(iconRect, "Shift+Drag\nRelink", labelStyle);
                            }

                            presetIndex++;                            
                        }
                    }







					if (e.type == EventType.MouseDown && e.button == 0)
					{
						if(presetUnderCursor != -1)
						{
	#if UNITY_STANDALONE_OSX
							if (e.command)
	#else
							if (e.control)
	#endif
								settings.SelectPresetAdd(presets, presetUnderCursor);
							else if (e.shift) 							
								settings.SelectPresetRange(presets, presetUnderCursor);
							else
								settings.SelectPreset(presets, presetUnderCursor);
						}
						else
						{
							settings.DeselectAllPresets();
						}
						
												
						// Unfocus 'Preset Name' control
						GUI.FocusControl("__none__");

						e.Use();
					}


					if (e.type == EventType.ContextClick && presetUnderCursor != -1)
					{
						if (settings.IsPresetSelected(presetUnderCursor))
						{
							if (hasMultipleSelectedPresets)
								multiPresetContextMenu.ShowAsContext();
							else
								presetContextMenu.ShowAsContext();
						}
						else
						{
							settings.SelectPreset(presets, presetUnderCursor);
						}

						// Unfocus 'Preset Name' control
						GUI.FocusControl("__none__");

						e.Use();
					}



                    // Drag & Drop
					if (e.type == EventType.DragUpdated || e.type == EventType.DragPerform)
					{
						// Relink Prefab
						if (e.shift && presetUnderCursor != -1)
						{
							DragAndDrop.visualMode = DragAndDropVisualMode.Link;
							
							if (e.type == EventType.DragPerform) {
								DragAndDrop.AcceptDrag ();
								
								foreach (UnityEngine.Object draggedObject in DragAndDrop.objectReferences)
								{
									if (draggedObject is GameObject &&
									    PrefabUtility.GetPrefabType(draggedObject as GameObject) != PrefabType.None &&
									    AssetDatabase.Contains(draggedObject))
									{
										Undo.RegisterCompleteObjectUndo(settings, "Relink Prefab");
										settings.presets[presetUnderCursor].AssignPrefab(draggedObject as GameObject, settings);                                                        
									}
								}
							}
							e.Use();
						}
						else if(realRect.Contains (e.mousePosition))
							// Add Prefab
                        {
                            DragAndDrop.visualMode = DragAndDropVisualMode.Copy;

                            if (e.type == EventType.DragPerform) {
                                DragAndDrop.AcceptDrag ();

                                foreach (UnityEngine.Object draggedObject in DragAndDrop.objectReferences)
                                {
                                    if (draggedObject is GameObject && 
                                        PrefabUtility.GetPrefabType(draggedObject as GameObject) != PrefabType.None &&
                                        AssetDatabase.Contains(draggedObject))
                                    {
										Undo.RegisterCompleteObjectUndo(settings, "Add Prefab");
                                        settings.presets.Add(new BrushPreset(draggedObject as GameObject, settings));                                        
                                    }
                                }
                            }
                            e.Use();
                        }                     
                    }



                    GUI.EndScrollView();
                }

               


              
                // Begin Scroll area
                scrollPos = EditorGUILayout.BeginScrollView(scrollPos);



				if (!hasSelectedPresets)
				{
					EditorGUILayout.Space();
					EditorGUILayout.Space();
					EditorGUILayout.HelpBox("Select Preset", MessageType.Info);
					EditorGUILayout.Space();
					EditorGUILayout.Space();
				}
				else if (hasMultipleSelectedPresets)
				{
					EditorGUILayout.Space();
					EditorGUILayout.Space();
					EditorGUILayout.HelpBox("Multiple Selected Presets", MessageType.Info);
					EditorGUILayout.Space();
					EditorGUILayout.Space();
				}
				else if (hasSelectedPresets && selectedPreset != null)
                {
                    EditorGUILayout.Space();

					positionSettingsFoldout = EditorGUILayout.Foldout(positionSettingsFoldout, "Preset Settings", boldFoldout);
					if (positionSettingsFoldout)
					{
	                    ++EditorGUI.indentLevel;
	                    selectedPreset.name = EditorGUILayout.TextField ("Preset name", selectedPreset.name);
						if (currentTool != PaintTool.Erase) {
	                        selectedPreset.brushSize = EditorGUILayout.Slider ("Brush Size", selectedPreset.brushSize, 0.0f, settings.brushSizeMax);
	                        selectedPreset.brushSpacing = EditorGUILayout.Slider ("Brush Spacing", selectedPreset.brushSpacing, 0.01f, settings.brushSpacingMax);
	                    } else {
	                        selectedPreset.eraseBrushSize = EditorGUILayout.Slider ("Brush Size", selectedPreset.eraseBrushSize, 0.0f, settings.brushSizeMax);
	                    }
	                    --EditorGUI.indentLevel;
					}


                    if (currentTool != PaintTool.Erase)
                    {                      


                        positionSettingsFoldout = EditorGUILayout.Foldout(positionSettingsFoldout, "Position", boldFoldout);
                        if (positionSettingsFoldout)
                        {
                            ++EditorGUI.indentLevel;
                            selectedPreset.positionOffset = EditorGUILayout.Vector3Field("Surface Offset", selectedPreset.positionOffset);
                            --EditorGUI.indentLevel;
                        }




                        orientationSettingsFoldout = EditorGUILayout.Foldout(orientationSettingsFoldout, "Orientation", boldFoldout);
                        if (orientationSettingsFoldout)
                        {
                            ++EditorGUI.indentLevel;
                            selectedPreset.orientationTransformMode = (TransformMode) EditorGUILayout.EnumPopup("Transform", selectedPreset.orientationTransformMode);
                            selectedPreset.orientationMode = (OrientationMode) EditorGUILayout.EnumPopup("Direction", selectedPreset.orientationMode);
                            selectedPreset.flipOrientation = EditorGUILayout.Toggle("Flip Direction", selectedPreset.flipOrientation);
                            selectedPreset.rotation = EditorGUILayout.Vector3Field("Aux Rotation", selectedPreset.rotation);
                            selectedPreset.randomizeOrientation.x = EditorGUILayout.Slider ("Randomize X %", selectedPreset.randomizeOrientation.x, 0.0f, 100.0f);
                            selectedPreset.randomizeOrientation.y = EditorGUILayout.Slider ("Randomize Y %", selectedPreset.randomizeOrientation.y, 0.0f, 100.0f);
                            selectedPreset.randomizeOrientation.z = EditorGUILayout.Slider ("Randomize Z %", selectedPreset.randomizeOrientation.z, 0.0f, 100.0f);
                            --EditorGUI.indentLevel;
                        }



                        scaleSettingsFoldout = EditorGUILayout.Foldout(scaleSettingsFoldout, "Scale", boldFoldout);
                        if (scaleSettingsFoldout)
                        {
                            ++EditorGUI.indentLevel;
                            selectedPreset.scaleTransformMode = (TransformMode) EditorGUILayout.EnumPopup("Transform", selectedPreset.scaleTransformMode);
                            selectedPreset.scaleMode = (ScaleMode) EditorGUILayout.EnumPopup("Mode", selectedPreset.scaleMode);
                            if (selectedPreset.scaleMode == ScaleMode.Uniform)
                            {                    
                                selectedPreset.scaleUniformMin = EditorGUILayout.FloatField("Min", selectedPreset.scaleUniformMin);
                                selectedPreset.scaleUniformMax = EditorGUILayout.FloatField("Max", selectedPreset.scaleUniformMax);
                            }
                            else
                            {
                                selectedPreset.scalePerAxisMin = EditorGUILayout.Vector3Field("Min", selectedPreset.scalePerAxisMin);
                                selectedPreset.scalePerAxisMax = EditorGUILayout.Vector3Field("Max", selectedPreset.scalePerAxisMax);
                            }
                            --EditorGUI.indentLevel;
                        }
                    }

                }


				commonSettingsFoldout = EditorGUILayout.Foldout(commonSettingsFoldout, "Common Settings", boldFoldout);
				if (commonSettingsFoldout)
				{
					++EditorGUI.indentLevel;
					settings.paintRandom = EditorGUILayout.Toggle ("Paint Random", settings.paintRandom);
					
					settings.paintOnSelected = EditorGUILayout.Toggle ("Paint On Selected Only", settings.paintOnSelected);
					GUI.enabled = !settings.paintOnSelected;
					settings.paintLayers = LayerMaskField ("Paint On Layers", settings.paintLayers);
					GUI.enabled = true;
					
					sceneSettings.placeUnder = (Placement)EditorGUILayout.EnumPopup ("Place Under", sceneSettings.placeUnder);
					if (sceneSettings.placeUnder == Placement.CustomObject)
					{
						++EditorGUI.indentLevel;
						EditorGUI.BeginChangeCheck();
						sceneSettings.parentForPrefabs = (GameObject)EditorGUILayout.ObjectField("Custom Scene Object", sceneSettings.parentForPrefabs, typeof(GameObject), true);
						if (EditorGUI.EndChangeCheck())
						{
							if (sceneSettings.parentForPrefabs != null && AssetDatabase.Contains(sceneSettings.parentForPrefabs)) {
								sceneSettings.parentForPrefabs = null;                        
							}
							
							Utility.MarkActiveSceneDirty();
						}
						--EditorGUI.indentLevel;
					}
					settings.groupPrefabs = EditorGUILayout.Toggle("Group Prefabs", settings.groupPrefabs);
					
					settings.overwritePrefabLayer = EditorGUILayout.Toggle("Overwrite Prefab Layer", settings.overwritePrefabLayer);
					GUI.enabled = settings.overwritePrefabLayer;
					settings.prefabPlaceLayer = EditorGUILayout.LayerField("Prefab Place Layer", settings.prefabPlaceLayer);
					GUI.enabled = true;
					
					--EditorGUI.indentLevel;
				}

                 EditorGUILayout.EndScrollView();
            }




            if (currentTool == PaintTool.Settings)
            {
                EditorGUILayout.LabelField ("Settings", EditorStyles.boldLabel);
                ++EditorGUI.indentLevel;
                settings.brushSizeMax = EditorGUILayout.FloatField("Max Brush Size", settings.brushSizeMax);
                settings.brushSpacingMax = EditorGUILayout.FloatField("Max Brush Spacing", settings.brushSpacingMax);
                
                GUILayout.Space(20);
                --EditorGUI.indentLevel;
                EditorGUILayout.LabelField ("x:BrushSize y:Spacing z:Scale_Min w:Scale_Max", EditorStyles.boldLabel);

                ++EditorGUI.indentLevel;
                EditorGUILayout.PropertyField(setting_prop_lodBrushSize);
                setting_sobj.ApplyModifiedProperties();

                --EditorGUI.indentLevel;

                EditorGUILayout.LabelField ("Help", EditorStyles.boldLabel);
                ++EditorGUI.indentLevel;
                EditorGUILayout.HelpBox(helpText, MessageType.None, true);
                --EditorGUI.indentLevel;
            }




            EditorUtility.SetDirty(settings);
		}



        static LayerMask LayerMaskField(string label, LayerMask layerMask)
        {
            List<string> layers = new List<string>(32);
            List<int> layerNumbers = new List<int>(32);

            for (int i = 0; i < 32; i++)
            {
                string layerName = LayerMask.LayerToName(i);
                if (layerName != "") {
                    layers.Add(layerName);
                    layerNumbers.Add(i);
                }
            }
            int maskWithoutEmpty = 0;
            for (int i = 0; i < layerNumbers.Count; i++) {
                if (((1 << layerNumbers[i]) & layerMask.value) > 0)
                    maskWithoutEmpty |= (1 << i);
            }
            maskWithoutEmpty = EditorGUILayout.MaskField(label, maskWithoutEmpty, layers.ToArray());
            int mask = 0;
            for (int i = 0; i < layerNumbers.Count; i++) {
                if ((maskWithoutEmpty & (1 << i)) > 0)
                    mask |= (1 << layerNumbers[i]);
            }
            layerMask.value = mask;
            return layerMask;
        }



        GameObject Raycast(Vector2 mousePosition, out RaycastHitEx raycastHit, int layersMask)
        {
            raycastHit = default(RaycastHitEx);

            // PickGameObject Work only if( Event.current.type == EventType.MouseMove )
#if (UNITY_5_0 || UNITY_5_1 || UNITY_5_2)
			GameObject gameObject = HandleUtility.PickGameObject(mousePosition, false);
#else
			GameObject gameObject = HandleUtility.PickGameObject(mousePosition, false);
#endif
            if (gameObject == null)
                return null;


            if (settings.paintOnSelected)
            {
                if (selectedObjects == null)
                    return null;
                
                if (_selectedGameObjects.Contains(gameObject) == false)
                    return null;
            }
            else
            {
                if(((1 << gameObject.layer) & layersMask) == 0)
                    return null;
            }


            Ray ray = HandleUtility.GUIPointToWorldRay (mousePosition);
            MeshFilter meshFilter = gameObject.GetComponent<MeshFilter>();


            if (meshFilter != null && meshFilter.sharedMesh != null)
            {
				// raycast mesh 

				if(Utility.IntersectRayMesh2 (ray, meshFilter.sharedMesh, meshFilter.transform.localToWorldMatrix, out raycastHit))
                {
					// if we hit backwards - reverse normal
					if (Vector3.Dot (ray.direction, raycastHit.normal) > 0.0f)
					{
						raycastHit.normal = -raycastHit.normal;
						raycastHit.localNormal = -raycastHit.localNormal;
					}

                    return gameObject;
                }
            }
            else
            {
				// if no mesh attached - try raycast using physics colliders
                UnityEngine.RaycastHit unityRaycastHit;
				Collider collider = gameObject.GetComponent<Collider>();

				if (collider != null)
				{
					// TIP: do not use Mathf.Infinity, strange bug
					if(collider.Raycast(ray, out unityRaycastHit, 100000.0f))
					{
						raycastHit.ray = ray;
						raycastHit.isHit = true;
						raycastHit.distance = unityRaycastHit.distance;
						raycastHit.point = unityRaycastHit.point;
						raycastHit.normal = unityRaycastHit.normal;
						
						// if we hit backwards - reverse normal
						if (Vector3.Dot (ray.direction, raycastHit.normal) > 0.0f)
						{
							raycastHit.normal = -raycastHit.normal;
							raycastHit.localNormal = -raycastHit.localNormal;
						}
						
						
						return gameObject;
					}
				}

            }

            return null;
        }





		void OrientObject(Vector3 normal, GameObject gameObject, bool precisePlace, BrushPreset preset)
        {            
            Transform transform = gameObject.transform;
            Quaternion placeOrientation;


            // Place orientation
            {
                Vector3 forward;
                Vector3 upwards;


                switch(preset.orientationMode)
                {
                case OrientationMode.AlongBrushStroke:
                    {
                        if (!precisePlace)
						{							
                            forward = strokeDirection;
						}
                        else
                            forward = Vector3.right;
                        
						upwards = normal;
                        if (preset.flipOrientation)
                            forward = -forward;										

                        placeOrientation = Quaternion.LookRotation(Vector3.Cross(forward, upwards).normalized, upwards);
                    }
                    break;
                case OrientationMode.AlongSurfaceNormal:
                    {
                        Vector3 right;
						upwards = normal;
                        if (preset.flipOrientation)
                            upwards = -upwards;
                        GetRightForward(upwards, out right, out forward);
                        placeOrientation = Quaternion.LookRotation(forward, upwards);
                    }
                    break;
                case OrientationMode.X:
                    {
                        Vector3 right;
                        upwards = new Vector3(1, 0, 0);
                        if (preset.flipOrientation)
                            upwards = -upwards;
                        GetRightForward(upwards, out right, out forward);
                        placeOrientation = Quaternion.LookRotation(forward, upwards);

                    }
                    break;
                case OrientationMode.Y:
                    {
                        Vector3 right;
                        upwards = new Vector3(0, 1, 0);
                        if (preset.flipOrientation)
                            upwards = -upwards;
                        GetRightForward(upwards, out right, out forward);
                        placeOrientation = Quaternion.LookRotation(forward, upwards);

                    }
                    break;
                case OrientationMode.Z:
                    {
                        Vector3 right;
                        upwards = new Vector3(0, 0, 1);
                        if (preset.flipOrientation)
                            upwards = -upwards;
                        GetRightForward(upwards, out right, out forward);
                        placeOrientation = Quaternion.LookRotation(forward, upwards);
                    }
                    break;
                default: case OrientationMode.None:
                    placeOrientation = Quaternion.identity;
                    break;
                }

            }




            // Random rotation
            Vector3 randomVector = UnityEngine.Random.insideUnitSphere * 0.5f;
            Quaternion randomRotation = Quaternion.Euler(new Vector3(preset.randomizeOrientation.x * 3.6f * randomVector.x,
                preset.randomizeOrientation.y * 3.6f * randomVector.y,
                preset.randomizeOrientation.z * 3.6f * randomVector.z));




            // Orient Mode
            switch (preset.orientationTransformMode)
            {
            case TransformMode.Absolute:
                {
                    Vector3 localEulerAngles = preset.rotation;
                    transform.eulerAngles = Vector3.zero;
                    transform.localEulerAngles = Vector3.zero;

                    transform.rotation =  placeOrientation * (randomRotation * Quaternion.Euler(localEulerAngles));
                }
                break;
            default: case TransformMode.Relative:
                {
                    Vector3 localEulerAngles = preset.rotation + transform.localEulerAngles;
                    transform.eulerAngles = Vector3.zero;
                    transform.localEulerAngles = Vector3.zero;

                    transform.rotation = placeOrientation * (randomRotation * Quaternion.Euler(localEulerAngles));
                }
                break;
            }
        }


		void PositionObject(RaycastHitEx raycastHit, GameObject gameObject, BrushPreset preset)
        {            
            Transform transform = gameObject.transform;

            Vector3 right;
            Vector3 forward;

            GetRightForward(raycastHit.normal, out right, out forward);

            transform.position = raycastHit.point + right * preset.positionOffset.x
                                        + raycastHit.normal * preset.positionOffset.y
                                        + forward * preset.positionOffset.z;

        }


		void ScaleObject(RaycastHitEx raycastHit, GameObject gameObject, BrushPreset preset)
        {            
            Vector3 randomVector = UnityEngine.Random.insideUnitSphere;
            Vector3 scale;

            randomVector = new Vector3(Mathf.Abs(randomVector.x), Mathf.Abs(randomVector.y), Mathf.Abs(randomVector.z));

            if (preset.scaleMode == ScaleMode.Uniform)
            {
                float scaleValue = preset.scaleUniformMin + randomVector.x * (preset.scaleUniformMax - preset.scaleUniformMin);
                scale = new Vector3(scaleValue, scaleValue, scaleValue);
            }
            else
            {                
                scale = new Vector3(preset.scalePerAxisMin.x + randomVector.x * (preset.scalePerAxisMax.x - preset.scalePerAxisMin.x),
                    preset.scalePerAxisMin.y + randomVector.y * (preset.scalePerAxisMax.y - preset.scalePerAxisMin.y),
                    preset.scalePerAxisMin.z + randomVector.z * (preset.scalePerAxisMax.z - preset.scalePerAxisMin.z));
            }

            if (preset.scaleTransformMode == TransformMode.Relative)
                gameObject.transform.localScale = new Vector3(gameObject.transform.localScale.x * scale.x,
                                                              gameObject.transform.localScale.y * scale.y,
                                                              gameObject.transform.localScale.z * scale.z);
            else
                gameObject.transform.localScale = scale;
        }




		GameObject PlaceObject(RaycastHitEx raycastHit, GameObject hitObject, bool precisePlace, BrushPreset preset)
        {                
			GameObject gameObject = PrefabUtility.InstantiatePrefab(preset.prefab) as GameObject;

            if (settings.overwritePrefabLayer)
            {
                gameObject.layer = settings.prefabPlaceLayer;
                EnumerateChilds(gameObject.transform, child => { child.layer = settings.prefabPlaceLayer; return true; });
            }

			OrientObject(raycastHit.normal, gameObject, precisePlace, preset);
			PositionObject(raycastHit, gameObject, preset);
			ScaleObject(raycastHit, gameObject, preset);


            // Register Undo
            Undo.RegisterCreatedObjectUndo(gameObject, "Paint Prefabs");


            Utility.MarkActiveSceneDirty();


            EnumerateChilds(gameObject.transform, (go) =>
            {
                if (placedObjectsCount < placedObjects.Length) {
                    placedObjects[placedObjectsCount++] = go;
                    return true;
                }
                
                return false;
            });
            
			lastObjectInStroke = gameObject;
			lastObjectInStrokePlacePlane = new Plane (raycastHit.normal, raycastHit.point);

            return gameObject;
        }



		#if UNITY_5_4_OR_NEWER
		#else
        static IEnumerable<GameObject> SceneRoots()
        {            
            var prop = new HierarchyProperty(HierarchyType.GameObjects);
            var expanded = new int[0];
            while (prop.Next(expanded)) {
                yield return prop.pptrValue as GameObject;
            }
        }
		#endif

		void ParentObject(GameObject gameObject, BrushPreset preset)
        {
            GameObject parentObject = null;

            if (gameObject != null)
            {
                switch(sceneSettings.placeUnder)
                {
                case Placement.HitObject:
                    parentObject = hitObject;
                    break;
                case Placement.CustomObject:
                    parentObject = sceneSettings.parentForPrefabs;
                    break;
                default: case Placement.World:
                    break;
                }
            }

            // Group Prefabs
            // find group object by name
            if(settings.groupPrefabs)
            {
                Transform group = null;
				string groupName = preset.name + "_group";

                if (parentObject != null)
                    group = parentObject.transform.Find(groupName);
                else {
		#if UNITY_5_4_OR_NEWER
					GameObject[] sceneRoots = UnityEngine.SceneManagement.SceneManager.GetActiveScene().GetRootGameObjects();
					foreach(GameObject root in sceneRoots)
					{
						if(root.name == groupName) {
							group = root.transform;
							break;
						}
					}

		#else
                    foreach(GameObject root in SceneRoots())
					{
                        if(root.name == groupName) {
                            group = root.transform;
                            break;
                        }
                    }
		#endif
                }            
                if (group == null)
                {
                    GameObject childObject = new GameObject(groupName);
                    if (parentObject != null)
                        childObject.transform.parent = parentObject.transform;
                    group = childObject.transform;
                }

                parentObject = group.gameObject;
            }


			if (gameObject != null && parentObject != null && parentObject.transform != null)
                gameObject.transform.parent = parentObject.transform;
        }


        void GetRightForward(Vector3 up, out Vector3 right, out Vector3 forward)
        {
            right = Vector3.Cross(up, Vector3.forward).normalized;
            if (right.magnitude < 0.001f)
                right = Vector3.right;

            forward = Vector3.Cross(right, up).normalized;
        }


        void DrawEraseHandles(RaycastHitEx hit, BrushPreset preset)
        {
            Handles.color = Color.red;
            Handles.CircleCap (1, hit.point, Quaternion.LookRotation (hit.normal), preset.eraseBrushSize);

            Vector3 forward, right;
            GetRightForward(hit.normal, out right, out forward);

            float size = Mathf.Max(preset.eraseBrushSize * 0.1f, 0.2f);

            Handles.color = Color.green;
            Handles.DrawLine(hit.point + hit.normal * size, hit.point + hit.normal * -size);
            Handles.color = Color.red;
            Handles.DrawLine(hit.point + right * size, hit.point + right * -size);
            Handles.color = Color.blue;
            Handles.DrawLine(hit.point + forward * size, hit.point + forward * -size);
        }


        void DrawHandles(RaycastHitEx hit, BrushPreset preset)
        {
            Handles.color = Color.red;
            Handles.CircleCap (1, hit.point, Quaternion.LookRotation (hit.normal), preset.brushSize);

            Vector3 forward, right;
            GetRightForward(hit.normal, out right, out forward);

            float size = Mathf.Max(preset.brushSize * 0.1f, 0.2f);

            Handles.color = Color.green;
            Handles.DrawLine(hit.point + hit.normal * size, hit.point + hit.normal * -size);
            Handles.color = Color.red;
            Handles.DrawLine(hit.point + right * size, hit.point + right * -size);
            Handles.color = Color.blue;
            Handles.DrawLine(hit.point + forward * size, hit.point + forward * -size);
        }




        void DrawPrecisePlaceHandles(RaycastHitEx hit, float radius, float angle, Vector3 point)
        {
            Handles.color = Color.red;
            Handles.CircleCap (1, hit.point, Quaternion.LookRotation (hit.normal), radius);

			GUIStyle style = new GUIStyle(EditorStyles.largeLabel);
			style.normal.textColor = Color.red;
			style.fontStyle = FontStyle.Bold;


            Vector3 forward, right;
            GetRightForward(hit.normal, out right, out forward);

			Handles.color = Color.red;
			Handles.DrawDottedLine(hit.point, point, 4.0f);
			Handles.DrawDottedLine(hit.point, hit.point + right * radius, 4.0f);


            
			Handles.color = new Color(0, 0, 0, 0.2f);
			Handles.DrawSolidArc(hit.point, hit.normal, right, angle, radius);


			Handles.color = Color.red;
			Handles.Label(point, "    A: " + (angle).ToString("F2"), style);
			Handles.Label(point, "\n    R: " + (radius).ToString("F2"), style);



			float handleSize = HandleUtility.GetHandleSize (hit.point) * 0.5f;

            Handles.color = Color.green;
			Handles.DrawLine(hit.point + hit.normal * handleSize, hit.point + hit.normal * -handleSize);
            Handles.color = Color.red;
			Handles.DrawLine(hit.point + right * handleSize, hit.point + right * -handleSize);
            Handles.color = Color.blue;
			Handles.DrawLine(hit.point + forward * handleSize, hit.point + forward * -handleSize);
        }




        Bounds GetObjectBounds(GameObject gameObject)
        {
            Bounds bounds = new Bounds();
            bool found = false;

            EnumerateChilds(gameObject.transform, (go) =>
                {
                    Renderer renderer = go.GetComponent<Renderer>();
                    if (renderer != null) {
                        if (!found) {
                            bounds = renderer.bounds;
                            found = true;
                        } else {
                            bounds.Encapsulate(renderer.bounds);
                        }
                    } else {
                        Collider collider = go.GetComponent<Collider>();
                        if (collider != null) {
                            if (!found) {
                                bounds = collider.bounds;
                                found = true;
                            } else {
                                bounds.Encapsulate(collider.bounds);
                            }
                        } else {
                         
                        }
                    }
                    return true;

                });

            if (!found)
                return new Bounds(gameObject.transform.position, gameObject.transform.lossyScale);

            return bounds;
        }


        Vector3 GetObjectScaleFactor(GameObject gameObject)
        {
            Bounds bounds = GetObjectBounds(gameObject);
            Vector3 localScale = gameObject.transform.localScale;

            float size = Mathf.Max(bounds.size.x, bounds.size.y, bounds.size.z);

            if (size != 0.0f)
                size = 1.0f / size;
            else
                size = 1.0f;

            return new Vector3(localScale.x * size, localScale.y * size, localScale.z * size);
        }





		void OnSceneGUI(SceneView sceneView)
		{
			Event e = Event.current;


            HandleEvents();

            if(settings.openLodInEditor)
                Spenve.MsgSystem.Instance.PostMessage("OnZoomChanged", (int)sceneView.camera.transform.position.y);

            // if any object selected - abort paint
            if (Selection.objects.Length > 0 && (currentTool == PaintTool.Paint || currentTool == PaintTool.Erase))
            {
                currentTool = PaintTool.None;
                selectedObjects = null;
                allSceneObjects = null;
                Repaint();
            }
            
            if (currentTool != PaintTool.Paint && currentTool != PaintTool.Erase)
            {
                if (onDrawing)
                {
                    for (int i = 0; i < placedObjectsCount; i++)
                        placedObjects[i] = null;
                    placedObjectsCount = 0;

					onMouseDown = false;
					onPrecisePlace = false;
                    onDrawing = false;
                    Repaint ();
                }

                return;
            }

			// Unity Issue 799561
			// https://issuetracker.unity3d.com/issues/osx-event-dot-current-dot-shift-returns-true-only-when-moving-the-mouse
			// Temp fix
			if (e.type != EventType.Layout && e.type != EventType.Repaint)
			{
				eventModifiers = e.modifiers;
			}

			Vector2 mousePosition = prevMousePos;

            switch (e.type)
            {            
            case EventType.MouseDown:
                if ((e.modifiers & EventModifiers.Alt) == 0 && e.button == 0)
                {
                    onMouseDown = true;

					lastObjectInStroke = null;

                    if ((e.modifiers & EventModifiers.Shift) != 0)
                        onPrecisePlace = true;

                    e.Use();
                }
                break;
			case EventType.MouseUp:
				onMouseDown = false;
				onMouseUp = true;
				break;	
			case EventType.MouseMove:				
				mousePosition = new Vector2 (Mathf.Round (e.mousePosition.x), Mathf.Round (e.mousePosition.y));
				e.Use();
				break;
            case EventType.MouseDrag:
                if ((e.modifiers & EventModifiers.Alt) == 0 && e.button == 0)
                {
					mousePosition = new Vector2 (Mathf.Round (e.mousePosition.x), Mathf.Round (e.mousePosition.y));
                    e.Use();
                }
                break;            
            case EventType.Layout:
                HandleUtility.AddDefaultControl (GUIUtility.GetControlID (FocusType.Passive));
				break;
			case EventType.Repaint:				
				break;
            }



            Ray ray;
            int layersMask = currentTool == PaintTool.Erase ? ~0 : settings.paintLayers;            
            bool isMouseChangePosition = Utility.CompareVector2 (mousePosition, prevMousePos) != true;

			if (isMouseChangePosition)
            {
                ray = HandleUtility.GUIPointToWorldRay (mousePosition);
                hitObject = Raycast(mousePosition, out hitInfo, layersMask);
            }
            else {                
                ray = prevRay;
            }





			bool hasMultipleSelectedPresets = settings.HasMultipleSelectedPresets();
			BrushPreset preset = settings.GetFirstSelectedPreset();


			if (preset != null && preset.prefab != null && !hasMultipleSelectedPresets)
            {    

				// Frame camera on brush hit point
				if (hitInfo.isHit &&
				    e.type == EventType.KeyDown &&
				    e.keyCode == KeyCode.F &&
				    e.modifiers == 0)
				{
					SceneView.lastActiveSceneView.LookAt(hitInfo.point);
					e.Use();
				}



                if (currentTool == PaintTool.Erase) 
                {
                    // Erase objects

                    if (hitInfo.isHit) {
                        DrawEraseHandles(hitInfo, preset);
                    }

                    if (onMouseDown)
                    {
                        onDrawing = true;
                    }

                    if (onDrawing)
                    {
                        if(hitObject != null){
                            if (PrefabUtility.GetCorrespondingObjectFromSource(hitObject) == preset.prefab)
                                Undo.DestroyObjectImmediate(hitObject);
                        }
                        
                        foreach (GameObject gameObject in allSceneObjects)
                        {
                            if(gameObject == null)
                            {
                                continue;
                            }
                            if(PrefabUtility.GetCorrespondingObjectFromSource(gameObject) == preset.prefab)
                            {
                                if(Vector3.Distance(gameObject.transform.position, hitInfo.point) < preset.eraseBrushSize)                        
                                    Undo.DestroyObjectImmediate(gameObject);
                            }
                        }
                    }
                }
                else
                if (onPrecisePlace)
                {
                    // Precise Place

                    if (onMouseDown)
                    {
                        onDrawing = true;
                        dragDistance = 0;

                        if (hitInfo.isHit)
                        {
							preciseGameObject = PlaceObject(hitInfo, hitObject, true, preset);
                            precisePlaceOrienation = preciseGameObject.transform.rotation;
                            preciseScaleFactor = GetObjectScaleFactor(preciseGameObject);
                        }
                        preciseHitInfo = hitInfo;
                    }

                    if (preciseHitInfo.isHit && preciseGameObject != null)
                    {
                        float rayDistance;
                        Plane plane = new Plane(preciseHitInfo.normal, preciseHitInfo.point);

                        if(plane.Raycast(ray, out rayDistance))
                        {
                            Vector3 point = ray.GetPoint(rayDistance);
                            Vector3 vector = point - preciseHitInfo.point;
                            float vectorLength = vector.magnitude;

                            if (vectorLength < 0.01f)
                            {
                                vector = Vector3.up * 0.01f;
                                vectorLength = 0.01f;
                            }

                            float scale = Mathf.Max(vectorLength * 2, 0.01f);

                            preciseGameObject.transform.localScale = new Vector3(scale * preciseScaleFactor.x, scale * preciseScaleFactor.y, scale * preciseScaleFactor.z);

                            Vector3 forward, right;
                            GetRightForward(preciseHitInfo.normal, out right, out forward);

                            float angle = Vector3.Angle(right.normalized, vector.normalized);
                            if (Vector3.Dot(vector.normalized, forward) > 0.0f)
                                angle = -angle;
								
                            // Hold Contol to snap angle
								if ((eventModifiers & EventModifiers.Control) != 0)
                                angle = (float)((int)(Mathf.Round(angle / 15.0f) * 15.0f));
                            
                            preciseGameObject.transform.eulerAngles = Vector3.zero;
                            preciseGameObject.transform.localEulerAngles = Vector3.zero;
                            preciseGameObject.transform.rotation =  Quaternion.AngleAxis(angle, preciseHitInfo.normal) * precisePlaceOrienation;


                            DrawPrecisePlaceHandles(preciseHitInfo, scale * 0.5f, angle, point);
                        }
                    }

                    if (onMouseUp) {
						ParentObject(preciseGameObject, preset);
                        onPrecisePlace = false;
                    }
                }
                else
                {
                    // Place objects

                    float spacing = Mathf.Max (0.01f, preset.brushSpacing);

                    if (hitInfo.isHit) {
                        DrawHandles(hitInfo, preset);
                    }

                    if (onMouseDown)
                    {
                        onDrawing = true;
                        dragDistance = 0;
                        strokeDirection = new Vector3(1, 0, 0);

                        if (hitInfo.isHit)
                        {                           
							ParentObject(PlaceObject(hitInfo, hitObject, false, preset), preset);
                        }

                        prevHitInfo = hitInfo;
                    }



                    if (onDrawing && isMouseChangePosition && (hitInfo.isHit || prevHitInfo.isHit))
                    {
                        Vector3 hitPoint = hitInfo.point;
                        Vector3 lastHitPoint = prevHitInfo.point;
                        bool isTwoPoints = true;

                        if (!hitInfo.isHit)
                        {
                            float rayDistance;
                            Plane trianglePlane = new Plane (prevHitInfo.normal, prevHitInfo.point);
                            if (trianglePlane.Raycast (ray, out rayDistance))
                                hitPoint = ray.GetPoint (rayDistance);
                            else
                                isTwoPoints = false;
                        }

                        if (!prevHitInfo.isHit)
                        {
                            float rayDistance;
                            Plane trianglePlane = new Plane (hitInfo.normal, hitInfo.point);
                            if (trianglePlane.Raycast (prevHitInfo.ray, out rayDistance))
                                lastHitPoint = prevHitInfo.ray.GetPoint (rayDistance);
                            else
                                isTwoPoints = false;
                        }


						// re-orient object along stroke
						if (preset.orientationMode == OrientationMode.AlongBrushStroke &&
						    lastObjectInStroke != null)
						{
							float d;
							lastObjectInStrokePlacePlane.Raycast(ray, out d);
							Vector3 point = ray.GetPoint(d);
							strokeDirection = (point - lastObjectInStroke.transform.position).normalized;
							lastObjectInStroke.transform.eulerAngles = Vector3.zero;
							OrientObject(lastObjectInStrokePlacePlane.normal, lastObjectInStroke, false, preset);
						}



                        if (isTwoPoints)
                        {
                            Vector3 moveVector = (hitPoint - lastHitPoint);
                            float moveLenght = moveVector.magnitude;
                            Vector3 moveDirection = moveVector.normalized;

                            


                            if (dragDistance + moveLenght >= spacing)
                            {
                                float d = spacing - dragDistance;
                                Vector3 drawPoint = lastHitPoint + moveDirection * d;
                                dragDistance = 0;
                                moveLenght -= d;

                                RaycastHitEx tempHitInfo = default(RaycastHitEx);
                                hitObject = Raycast (HandleUtility.WorldToGUIPoint(drawPoint + (UnityEngine.Random.onUnitSphere * preset.brushSize * 0.5f)), out tempHitInfo, layersMask);

                                if (tempHitInfo.isHit) {
									ParentObject(PlaceObject(tempHitInfo, hitObject, false, preset), preset);
                                }

                                while (moveLenght >= spacing)
                                {
                                    moveLenght -= spacing;

                                    drawPoint += moveDirection * spacing;

                                    hitObject = Raycast (HandleUtility.WorldToGUIPoint(drawPoint + (UnityEngine.Random.onUnitSphere * preset.brushSize * 0.5f)), out tempHitInfo, layersMask);
                                    if (tempHitInfo.isHit) {
										ParentObject(PlaceObject(tempHitInfo, hitObject, false, preset), preset);
                                    }
                                }
                            }

                            dragDistance += moveLenght;
                        }

                    }

                }
            }




            if (onMouseUp && onDrawing)
            {
                for (int i = 0; i < placedObjectsCount; i++)
                    placedObjects[i] = null;
                placedObjectsCount = 0;

				onMouseDown = false;
				onPrecisePlace = false;
                onDrawing = false;
                Repaint();
            }


            onMouseDown = false;
            onMouseUp = false;


            prevHitInfo = hitInfo;
            prevMousePos = mousePosition;
            prevRay = ray;
		}
	}

}


