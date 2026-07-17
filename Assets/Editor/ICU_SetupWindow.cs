using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEditor.PackageManager;
using UnityEditor.PackageManager.Requests;
using System;
using System.IO;

namespace IcuXr.EditorTooling
{
    public class IcuSetupWindow : EditorWindow
    {
        private Vector2 _scrollPosition;
        private bool _isNewtonsoftInstalled;
        private bool _isTmpInstalled;
        private bool _doesScenarioExist;
        private static AddRequest _packageRequest;

        [MenuItem("ICU XR/Setup Wizard")]
        public static void ShowWindow()
        {
            var window = GetWindow<IcuSetupWindow>("ICU XR Setup");
            window.minSize = new Vector2(400, 550);
            window.Show();
        }

        private void OnEnable()
        {
            CheckProjectDependencies();
        }

        private void OnGUI()
        {
            _scrollPosition = EditorGUILayout.BeginScrollView(_scrollPosition);

            DrawHeader();
            DrawSeparator();
            
            DrawDependencySection();
            DrawSeparator();

            DrawSceneSetupSection();
            DrawSeparator();

            DrawUtilitySection();
            DrawSeparator();

            DrawDocumentationSection();

            EditorGUILayout.EndScrollView();
        }

        #region UI Drawing Methods

        private void DrawHeader()
        {
            EditorGUILayout.Space(10);
            var titleStyle = new GUIStyle(EditorStyles.boldLabel)
            {
                fontSize = 16,
                alignment = TextAnchor.MiddleLeft
            };
            EditorGUILayout.LabelField("ICU XR Simulation - Project Setup", titleStyle, GUILayout.Height(25));
            EditorGUILayout.LabelField("Automated environment and dependency configuration utility.", EditorStyles.miniLabel);
            EditorGUILayout.Space(5);
        }

        private void DrawDependencySection()
        {
            EditorGUILayout.LabelField("Dependency Diagnostics", EditorStyles.boldLabel);
            EditorGUILayout.Space(5);

            // Newtonsoft.Json Row
            DrawDependencyRow("Newtonsoft.Json", _isNewtonsoftInstalled, "Install via UPM", () =>
            {
                _packageRequest = Client.Add("com.unity.nuget.newtonsoft-json");
                EditorApplication.update += MonitorPackageInstallation;
            });

            // TextMeshPro Row
            DrawDependencyRow("TextMesh Pro", _isTmpInstalled, "Open Package Manager", () =>
            {
                UnityEditor.PackageManager.UI.Window.Open("com.unity.textmeshpro");
            });

            // Scenario JSON Row
            DrawDependencyRow("Scenario Config (Hypoxia)", _doesScenarioExist, null, null);

            EditorGUILayout.Space(5);
            if (GUILayout.Button("Run Diagnostics Update", GUILayout.Height(25)))
            {
                CheckProjectDependencies();
            }
        }

        private void DrawDependencyRow(string label, bool isInstalled, string actionLabel, Action onActionClick)
        {
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField(label, GUILayout.Width(180));

            if (isInstalled)
            {
                var greenStyle = new GUIStyle(EditorStyles.label);
                greenStyle.normal.textColor = Color.green;
                EditorGUILayout.LabelField("Detected", greenStyle, GUILayout.Width(100));
            }
            else
            {
                var redStyle = new GUIStyle(EditorStyles.label);
                redStyle.normal.textColor = Color.red;
                EditorGUILayout.LabelField("Missing", redStyle, GUILayout.Width(100));

                if (!string.IsNullOrEmpty(actionLabel) && onActionClick != null)
                {
                    if (GUILayout.Button(actionLabel, GUILayout.Width(150)))
                    {
                        onActionClick.Invoke();
                    }
                }
            }
            EditorGUILayout.EndHorizontal();
        }

        private void DrawSceneSetupSection()
        {
            EditorGUILayout.LabelField("Environment Generation & Staging", EditorStyles.boldLabel);
            EditorGUILayout.Space(5);

            EditorGUILayout.HelpBox(
                "Quick generation tools for prototyping the ICU room. Ensure you refer to SETUP_GUIDE.md for detailed manual spatial staging adjustments.",
                MessageType.Info
            );

            EditorGUILayout.Space(5);

            if (GUILayout.Button("Generate Prototype ICU Room Structure", GUILayout.Height(28)))
            {
                GenerateIcuRoomGeometry();
            }

            if (GUILayout.Button("Generate Standard UI Canvas", GUILayout.Height(28)))
            {
                GenerateStandardCanvas();
            }

            if (GUILayout.Button("Initialize Scene Managers", GUILayout.Height(28)))
            {
                SpawnManagerHierarchy();
            }
        }

        private void DrawUtilitySection()
        {
            EditorGUILayout.LabelField("Developer Utilities", EditorStyles.boldLabel);
            EditorGUILayout.Space(5);

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Validate Scenario JSON", GUILayout.Height(25)))
            {
                ValidateScenarioLoad();
            }
            if (GUILayout.Button("Open Scenarios Folder", GUILayout.Height(25)))
            {
                string path = Path.Combine(Application.dataPath, "Resources", "Scenarios");
                if (Directory.Exists(path)) EditorUtility.RevealInFinder(path);
                else EditorUtility.DisplayDialog("Error", "Scenarios directory does not exist yet.", "OK");
            }
            EditorGUILayout.EndHorizontal();

            if (GUILayout.Button("Locate Persistent Log Directory", GUILayout.Height(25)))
            {
                EditorUtility.RevealInFinder(Application.persistentDataPath);
            }
        }

        private void DrawDocumentationSection()
        {
            EditorGUILayout.LabelField("Technical Resources", EditorStyles.boldLabel);
            EditorGUILayout.Space(5);

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("README", GUILayout.Height(25))) OpenDocFile("README.md");
            if (GUILayout.Button("Setup Guide", GUILayout.Height(25))) OpenDocFile("SETUP_GUIDE.md");
            if (GUILayout.Button("Technical Spec", GUILayout.Height(25))) OpenDocFile("TECHNICAL_DOCUMENTATION.md");
            EditorGUILayout.EndHorizontal();
        }

        private void DrawSeparator()
        {
            EditorGUILayout.Space(8);
            var rect = EditorGUILayout.GetControlRect(false, 1);
            rect.height = 1;
            EditorGUI.DrawRect(rect, new Color(0.3f, 0.3f, 0.3f, 0.5f));
            EditorGUILayout.Space(8);
        }

        #endregion

        #region Dependency & Package Management Logic

        private void CheckProjectDependencies()
        {
            _isNewtonsoftInstalled = Type.GetType("Newtonsoft.Json.JsonConvert, Newtonsoft.Json") != null;
            _isTmpInstalled = Type.GetType("TMPro.TextMeshProUGUI, Unity.TextMeshPro") != null;

            string scenarioPath = Path.Combine(Application.dataPath, "Resources", "Scenarios", "icu_scenario_hypoxia_v1.json");
            _doesScenarioExist = File.Exists(scenarioPath);

            Repaint();
        }

        private void MonitorPackageInstallation()
        {
            if (_packageRequest == null || !_packageRequest.IsCompleted) return;

            if (_packageRequest.Status == StatusCode.Success)
            {
                Debug.Log("ICU XR Setup: Newtonsoft.Json integrated successfully.");
                CheckProjectDependencies();
            }
            else if (_packageRequest.Status >= StatusCode.Failure)
            {
                Debug.LogError($"ICU XR Setup: Package installation failed: {_packageRequest.Error.message}");
            }

            EditorApplication.update -= MonitorPackageInstallation;
            _packageRequest = null;
        }

        #endregion

        #region Scene Generation Logic

        private void GenerateIcuRoomGeometry()
        {
            if (!EditorUtility.DisplayDialog("Generate Environment", 
                "This action will generate primitive static colliders representing the floor, ceiling, and walls. Proceed?", 
                "Generate", "Cancel"))
            {
                return;
            }

            var roomRoot = new GameObject("ICU_Room_Environment");
            Undo.RegisterCreatedObjectUndo(roomRoot, "Generate ICU Room");

            // Floor
            var floor = GameObject.CreatePrimitive(PrimitiveType.Plane);
            floor.name = "Floor";
            floor.transform.SetParent(roomRoot.transform);
            floor.transform.localPosition = Vector3.zero;
            floor.transform.localScale = new Vector3(2f, 1f, 2f);
            Undo.RegisterCreatedObjectUndo(floor, "Generate ICU Room - Floor");

            // Walls
            GenerateWall("Wall_Back", roomRoot.transform, new Vector3(0f, 1.5f, 10f), new Vector3(20f, 3f, 0.2f));
            GenerateWall("Wall_Left", roomRoot.transform, new Vector3(-10f, 1.5f, 0f), new Vector3(0.2f, 3f, 20f));
            GenerateWall("Wall_Right", roomRoot.transform, new Vector3(10f, 1.5f, 0f), new Vector3(0.2f, 3f, 20f));

            // Ceiling
            var ceiling = GameObject.CreatePrimitive(PrimitiveType.Plane);
            ceiling.name = "Ceiling";
            ceiling.transform.SetParent(roomRoot.transform);
            ceiling.transform.localPosition = new Vector3(0f, 3f, 0f);
            ceiling.transform.localRotation = Quaternion.Euler(180f, 0f, 0f);
            ceiling.transform.localScale = new Vector3(2f, 1f, 2f);
            Undo.RegisterCreatedObjectUndo(ceiling, "Generate ICU Room - Ceiling");

            // Lighting Setup
            var lightGo = new GameObject("Room_PointLight");
            lightGo.transform.SetParent(roomRoot.transform);
            lightGo.transform.localPosition = new Vector3(0f, 2.7f, 0f);
            var lightComp = lightGo.AddComponent<Light>();
            lightComp.type = LightType.Point;
            lightComp.range = 15f;
            lightComp.intensity = 1.2f;
            Undo.RegisterCreatedObjectUndo(lightGo, "Generate ICU Room - Light");

            MarkSceneAsDirty();
            EditorUtility.DisplayDialog("Success", "Prototype room environment generated.", "OK");
        }

        private void GenerateWall(string wallName, Transform parent, Vector3 localPos, Vector3 scale)
        {
            var wall = GameObject.CreatePrimitive(PrimitiveType.Cube);
            wall.name = wallName;
            wall.transform.SetParent(parent);
            wall.transform.localPosition = localPos;
            wall.transform.localScale = scale;
            Undo.RegisterCreatedObjectUndo(wall, $"Generate ICU Room - {wallName}");
        }

        private void GenerateStandardCanvas()
        {
            if (FindObjectOfType<Canvas>() != null)
            {
                if (!EditorUtility.DisplayDialog("Canvas Detected", 
                    "An active UI Canvas already exists in the scene hierarchy. Do you want to instantiate another one?", 
                    "Create New", "Cancel"))
                {
                    return;
                }
            }

            var canvasGo = new GameObject("UI_Canvas_Main");
            var canvas = canvasGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;

            var scaler = canvasGo.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);

            canvasGo.AddComponent<GraphicRaycaster>();
            Undo.RegisterCreatedObjectUndo(canvasGo, "Create UI Canvas");

            if (FindObjectOfType<EventSystem>() == null)
            {
                var eventSystemGo = new GameObject("EventSystem");
                eventSystemGo.AddComponent<EventSystem>();
                eventSystemGo.AddComponent<StandaloneInputModule>();
                Undo.RegisterCreatedObjectUndo(eventSystemGo, "Create EventSystem");
            }

            MarkSceneAsDirty();
            EditorUtility.DisplayDialog("Success", "Standard Canvas hierarchy generated successfully.", "OK");
        }

        private void SpawnManagerHierarchy()
        {
            var existingController = GameObject.Find("Core_Systems_Controller");
            if (existingController != null)
            {
                EditorUtility.DisplayDialog("Warning", "A core controller object named 'Core_Systems_Controller' already exists.", "OK");
                return;
            }

            // Central Game Controller and Business Logic Modules
            var controller = new GameObject("Core_Systems_Controller");
            controller.AddComponent<GameController>();
            controller.AddComponent<GameStateManager>();
            controller.AddComponent<ScenarioManager>();
            controller.AddComponent<DecisionTreeEngine>();
            controller.AddComponent<LoggingManager>();
            controller.AddComponent<DebriefingManager>();
            controller.AddComponent<EHRManager>();
            Undo.RegisterCreatedObjectUndo(controller, "Spawn Core Systems");

            // Spatial Interaction Modules
            var hotspotManager = new GameObject("Spatial_Hotspot_Manager");
            hotspotManager.AddComponent<HotspotManager>();
            Undo.RegisterCreatedObjectUndo(hotspotManager, "Spawn Hotspot Systems");

            // UI Orchestrators
            if (FindObjectOfType<Canvas>() != null)
            {
                var uiManager = new GameObject("UI_Orchestration_Manager");
                uiManager.AddComponent<UIManager>();
                uiManager.AddComponent<HelpSystem>();
                Undo.RegisterCreatedObjectUndo(uiManager, "Spawn UI Orchestrators");
            }

            MarkSceneAsDirty();
            EditorUtility.DisplayDialog("Success", "System managers successfully instantiated and registered with Scene Undo.", "OK");
        }

        #endregion

        #region Helper Methods

        private void ValidateScenarioLoad()
        {
            var scenario = Resources.Load<TextAsset>("Scenarios/icu_scenario_hypoxia_v1");
            if (scenario != null)
            {
                EditorUtility.DisplayDialog("Diagnostics Passed", "Scenario JSON successfully loaded and verified via Resources pipeline.", "OK");
            }
            else
            {
                EditorUtility.DisplayDialog("Diagnostics Failed", "Unable to load 'icu_scenario_hypoxia_v1' from Resources/Scenarios/ directory.", "OK");
            }
        }

        private void OpenDocFile(string filename)
        {
            string path = Path.Combine(Application.dataPath, "..", filename);
            if (File.Exists(path))
            {
                Application.OpenURL("file:///" + path);
            }
            else
            {
                EditorUtility.DisplayDialog("File Missing", $"Documentation file '{filename}' was not found at the project root.", "OK");
            }
        }

        private void MarkSceneAsDirty()
        {
            if (!Application.isPlaying)
            {
                EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
            }
        }

        #endregion
    }
}