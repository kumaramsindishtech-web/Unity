// ============================================================================
// Industrial Reactor Simulator - Custom Editor Window
// Copyright (c) 2024 Industrial Reactor Simulator. MIT License.
// ============================================================================

using UnityEngine;
using UnityEditor;

namespace IndustrialReactorSimulator.Editor
{
    /// <summary>
    /// Custom editor window for controlling the industrial reactor simulation.
    /// </summary>
    public class ReactorSimulatorWindow : EditorWindow
    {
        [MenuItem("Tools/Industrial Reactor Simulator")]
        public static void ShowWindow()
        {
            var window = GetWindow<ReactorSimulatorWindow>();
            window.titleContent = new GUIContent("Reactor Simulator");
            window.minSize = new Vector2(350, 600);
            window.Show();
        }

        private GUIStyle headerStyle, subHeaderStyle, valueDisplayStyle, buttonStyle, sectionStyle;
        private bool stylesInitialized = false;
        private ReactorController reactor;
        private Vector2 scrollPosition;
        private float inletFlowSpeed = 20f, outletFlowSpeed = 15f, agitatorRPM = 60f, waterTemperature = 25f;

        private void OnEnable()
        {
            EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
            EditorApplication.update += Repaint;
        }

        private void OnDisable()
        {
            EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;
            EditorApplication.update -= Repaint;
        }

        private void OnPlayModeStateChanged(PlayModeStateChange state) => reactor = null;

        private void OnGUI()
        {
            InitializeStyles();
            scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);

            DrawHeader();
            EditorGUILayout.Space(10);

            if (!Application.isPlaying) { DrawPlayModeWarning(); EditorGUILayout.EndScrollView(); return; }

            FindReactor();
            if (reactor == null) { DrawNoReactorWarning(); EditorGUILayout.EndScrollView(); return; }

            DrawSimulationControls();
            EditorGUILayout.Space(10);
            DrawValveControls();
            EditorGUILayout.Space(10);
            DrawAgitatorControls();
            EditorGUILayout.Space(10);
            DrawParameterSliders();
            EditorGUILayout.Space(10);
            DrawRealtimeDisplay();

            EditorGUILayout.EndScrollView();
        }

        private void InitializeStyles()
        {
            if (stylesInitialized) return;
            headerStyle = new GUIStyle(EditorStyles.boldLabel) { fontSize = 18, alignment = TextAnchor.MiddleCenter, padding = new RectOffset(0, 0, 10, 10) };
            subHeaderStyle = new GUIStyle(EditorStyles.boldLabel) { fontSize = 12, padding = new RectOffset(5, 0, 5, 5) };
            valueDisplayStyle = new GUIStyle(EditorStyles.helpBox) { fontSize = 14, alignment = TextAnchor.MiddleCenter, fontStyle = FontStyle.Bold, padding = new RectOffset(10, 10, 8, 8) };
            buttonStyle = new GUIStyle(GUI.skin.button) { fontSize = 12, fontStyle = FontStyle.Bold, padding = new RectOffset(15, 15, 8, 8) };
            sectionStyle = new GUIStyle(EditorStyles.helpBox) { padding = new RectOffset(10, 10, 10, 10), margin = new RectOffset(5, 5, 5, 5) };
            stylesInitialized = true;
        }

        private void DrawHeader()
        {
            EditorGUILayout.BeginVertical(sectionStyle);
            GUILayout.Label("Industrial Reactor Simulator", headerStyle);
            GUILayout.Label("Real-time reactor control and monitoring", EditorStyles.centeredGreyMiniLabel);
            EditorGUILayout.EndVertical();
        }

        private void DrawPlayModeWarning()
        {
            EditorGUILayout.BeginVertical(sectionStyle);
            EditorGUILayout.HelpBox("PLAY MODE REQUIRED\n\nThe reactor simulation only runs during Play Mode.\n\nPress the Play button to start.", MessageType.Warning);
            EditorGUILayout.Space(10);
            GUI.backgroundColor = new Color(0.3f, 0.8f, 0.3f);
            if (GUILayout.Button("Enter Play Mode", buttonStyle, GUILayout.Height(40))) EditorApplication.isPlaying = true;
            GUI.backgroundColor = Color.white;
            EditorGUILayout.EndVertical();
        }

        private void DrawNoReactorWarning()
        {
            EditorGUILayout.BeginVertical(sectionStyle);
            EditorGUILayout.HelpBox("NO REACTOR FOUND\n\nPlease add a ReactorController component to your scene.", MessageType.Warning);
            EditorGUILayout.Space(10);
            if (GUILayout.Button("Create Reactor Controller", buttonStyle, GUILayout.Height(30))) CreateReactorController();
            EditorGUILayout.EndVertical();
        }

        private void FindReactor() { if (reactor == null) reactor = FindFirstObjectByType<ReactorController>(); }

        private void CreateReactorController()
        {
            GameObject reactorObj = new GameObject("ReactorController");
            reactorObj.AddComponent<ReactorController>();
            Selection.activeGameObject = reactorObj;
        }

        private void DrawSimulationControls()
        {
            EditorGUILayout.BeginVertical(sectionStyle);
            GUILayout.Label("Simulation Controls", subHeaderStyle);
            EditorGUILayout.BeginHorizontal();

            GUI.backgroundColor = reactor.CurrentState == ReactorState.Running ? new Color(0.5f, 0.5f, 0.5f) : new Color(0.3f, 0.8f, 0.3f);
            if (GUILayout.Button("Start Simulation", buttonStyle, GUILayout.Height(35))) reactor.StartSimulation();

            GUI.backgroundColor = reactor.CurrentState != ReactorState.Running ? new Color(0.5f, 0.5f, 0.5f) : new Color(0.9f, 0.3f, 0.3f);
            if (GUILayout.Button("Stop Simulation", buttonStyle, GUILayout.Height(35))) reactor.StopSimulation();

            GUI.backgroundColor = Color.white;
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.Space(5);

            GUI.backgroundColor = new Color(0.9f, 0.7f, 0.2f);
            if (GUILayout.Button("Reset Simulation", buttonStyle, GUILayout.Height(30))) reactor.ResetSimulation();
            GUI.backgroundColor = Color.white;
            EditorGUILayout.EndVertical();
        }

        private void DrawValveControls()
        {
            EditorGUILayout.BeginVertical(sectionStyle);
            GUILayout.Label("Valve Controls", subHeaderStyle);

            // Inlet Valve
            EditorGUILayout.BeginHorizontal();
            GUILayout.Label("Inlet Valve:", GUILayout.Width(100));
            bool inletOpen = reactor.InletValve != null && reactor.InletValve.IsOpen;
            GUI.backgroundColor = inletOpen ? new Color(0.3f, 0.8f, 0.3f) : new Color(0.8f, 0.8f, 0.8f);
            if (GUILayout.Button("Open", buttonStyle, GUILayout.Width(80))) reactor.OpenInletValve();
            GUI.backgroundColor = !inletOpen ? new Color(0.9f, 0.3f, 0.3f) : new Color(0.8f, 0.8f, 0.8f);
            if (GUILayout.Button("Close", buttonStyle, GUILayout.Width(80))) reactor.CloseInletValve();
            GUI.backgroundColor = Color.white;
            GUILayout.Label(inletOpen ? "OPEN" : "CLOSED", inletOpen ? EditorStyles.boldLabel : EditorStyles.label);
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space(5);

            // Outlet Valve
            EditorGUILayout.BeginHorizontal();
            GUILayout.Label("Outlet Valve:", GUILayout.Width(100));
            bool outletOpen = reactor.OutletValve != null && reactor.OutletValve.IsOpen;
            GUI.backgroundColor = outletOpen ? new Color(0.3f, 0.8f, 0.3f) : new Color(0.8f, 0.8f, 0.8f);
            if (GUILayout.Button("Open", buttonStyle, GUILayout.Width(80))) reactor.OpenOutletValve();
            GUI.backgroundColor = !outletOpen ? new Color(0.9f, 0.3f, 0.3f) : new Color(0.8f, 0.8f, 0.8f);
            if (GUILayout.Button("Close", buttonStyle, GUILayout.Width(80))) reactor.CloseOutletValve();
            GUI.backgroundColor = Color.white;
            GUILayout.Label(outletOpen ? "OPEN" : "CLOSED", outletOpen ? EditorStyles.boldLabel : EditorStyles.label);
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.EndVertical();
        }

        private void DrawAgitatorControls()
        {
            EditorGUILayout.BeginVertical(sectionStyle);
            GUILayout.Label("Agitator Controls", subHeaderStyle);
            EditorGUILayout.BeginHorizontal();

            bool agitatorRunning = reactor.Agitator != null && reactor.Agitator.IsRunning;
            GUI.backgroundColor = agitatorRunning ? new Color(0.5f, 0.5f, 0.5f) : new Color(0.3f, 0.8f, 0.3f);
            if (GUILayout.Button("Start Agitator", buttonStyle, GUILayout.Height(30))) reactor.StartAgitator();
            GUI.backgroundColor = !agitatorRunning ? new Color(0.5f, 0.5f, 0.5f) : new Color(0.9f, 0.3f, 0.3f);
            if (GUILayout.Button("Stop Agitator", buttonStyle, GUILayout.Height(30))) reactor.StopAgitator();
            GUI.backgroundColor = Color.white;
            GUILayout.Label(agitatorRunning ? "RUNNING" : "STOPPED", agitatorRunning ? EditorStyles.boldLabel : EditorStyles.label, GUILayout.Width(80));

            EditorGUILayout.EndHorizontal();
            EditorGUILayout.EndVertical();
        }

        private void DrawParameterSliders()
        {
            EditorGUILayout.BeginVertical(sectionStyle);
            GUILayout.Label("Control Parameters", subHeaderStyle);

            float maxInlet = reactor.SimulationData != null ? reactor.SimulationData.maxInletFlowRate : 100f;
            float maxOutlet = reactor.SimulationData != null ? reactor.SimulationData.maxOutletFlowRate : 80f;
            float maxRPM = reactor.SimulationData != null ? reactor.SimulationData.maxAgitatorRPM : 300f;
            float minTemp = reactor.SimulationData != null ? reactor.SimulationData.minTemperature : 0f;
            float maxTemp = reactor.SimulationData != null ? reactor.SimulationData.maxTemperature : 100f;

            EditorGUILayout.BeginHorizontal();
            GUILayout.Label("Inlet Flow:", GUILayout.Width(100));
            inletFlowSpeed = EditorGUILayout.Slider(reactor.InletFlowSpeed, 0f, maxInlet);
            GUILayout.Label("L/s", GUILayout.Width(30));
            EditorGUILayout.EndHorizontal();
            if (!Mathf.Approximately(inletFlowSpeed, reactor.InletFlowSpeed)) reactor.InletFlowSpeed = inletFlowSpeed;

            EditorGUILayout.BeginHorizontal();
            GUILayout.Label("Outlet Flow:", GUILayout.Width(100));
            outletFlowSpeed = EditorGUILayout.Slider(reactor.OutletFlowSpeed, 0f, maxOutlet);
            GUILayout.Label("L/s", GUILayout.Width(30));
            EditorGUILayout.EndHorizontal();
            if (!Mathf.Approximately(outletFlowSpeed, reactor.OutletFlowSpeed)) reactor.OutletFlowSpeed = outletFlowSpeed;

            EditorGUILayout.BeginHorizontal();
            GUILayout.Label("Agitator RPM:", GUILayout.Width(100));
            agitatorRPM = EditorGUILayout.Slider(reactor.TargetAgitatorRPM, 0f, maxRPM);
            GUILayout.Label("RPM", GUILayout.Width(30));
            EditorGUILayout.EndHorizontal();
            if (!Mathf.Approximately(agitatorRPM, reactor.TargetAgitatorRPM)) reactor.SetAgitatorRPM(agitatorRPM);

            EditorGUILayout.BeginHorizontal();
            GUILayout.Label("Temperature:", GUILayout.Width(100));
            waterTemperature = EditorGUILayout.Slider(reactor.TargetTemperature, minTemp, maxTemp);
            GUILayout.Label("°C", GUILayout.Width(30));
            EditorGUILayout.EndHorizontal();
            if (!Mathf.Approximately(waterTemperature, reactor.TargetTemperature)) reactor.TargetTemperature = waterTemperature;

            EditorGUILayout.EndVertical();
        }

        private void DrawRealtimeDisplay()
        {
            EditorGUILayout.BeginVertical(sectionStyle);
            GUILayout.Label("Realtime Monitoring", subHeaderStyle);

            EditorGUILayout.BeginHorizontal();
            GUILayout.Label("Reactor State:", GUILayout.Width(100));
            GUI.backgroundColor = GetStateColor(reactor.CurrentState);
            GUILayout.Label(reactor.GetStateString(), valueDisplayStyle);
            GUI.backgroundColor = Color.white;
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.Space(5);

            EditorGUILayout.BeginHorizontal();
            GUILayout.Label("Tank Fill:", GUILayout.Width(100));
            Rect fillRect = EditorGUILayout.GetControlRect(GUILayout.Height(25));
            EditorGUI.ProgressBar(fillRect, reactor.CurrentWaterLevel, $"{reactor.TankFillPercentage:F1}%");
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.Space(5);

            DrawValueDisplay("Temperature:", $"{reactor.CurrentTemperature:F1} °C", GetTemperatureColor(reactor.CurrentTemperature));
            DrawValueDisplay("Agitator RPM:", $"{reactor.CurrentAgitatorRPM:F0} RPM", GetRPMColor(reactor.CurrentAgitatorRPM));
            DrawValueDisplay("Bubble:", $"{reactor.CurrentBubbleIntensity * 100:F0}%", GetIntensityColor(reactor.CurrentBubbleIntensity));

            EditorGUILayout.EndVertical();
        }

        private void DrawValueDisplay(string label, string value, Color color)
        {
            EditorGUILayout.BeginHorizontal();
            GUILayout.Label(label, GUILayout.Width(100));
            GUI.backgroundColor = color;
            GUILayout.Label(value, valueDisplayStyle);
            GUI.backgroundColor = Color.white;
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.Space(2);
        }

        private Color GetStateColor(ReactorState state) => state switch
        {
            ReactorState.Running => new Color(0.3f, 0.8f, 0.3f),
            ReactorState.Starting or ReactorState.Stopping => new Color(0.9f, 0.7f, 0.2f),
            ReactorState.Error => new Color(0.9f, 0.3f, 0.3f),
            _ => new Color(0.5f, 0.5f, 0.5f)
        };

        private Color GetTemperatureColor(float temp) => temp < 30f ? new Color(0.3f, 0.5f, 0.9f) : temp < 60f ? new Color(0.9f, 0.7f, 0.2f) : new Color(0.9f, 0.3f, 0.2f);
        private Color GetRPMColor(float rpm) => Color.Lerp(new Color(0.5f, 0.5f, 0.5f), new Color(0.3f, 0.8f, 0.3f), Mathf.Clamp01(rpm / 300f));
        private Color GetIntensityColor(float intensity) => Color.Lerp(new Color(0.5f, 0.5f, 0.5f), new Color(0.3f, 0.6f, 0.9f), intensity);
    }
}
