// ============================================================================
// Industrial Reactor Simulator - Main Reactor Controller
// Copyright (c) 2024 Industrial Reactor Simulator. MIT License.
// ============================================================================

using UnityEngine;

namespace IndustrialReactorSimulator
{
    /// <summary>
    /// Main controller for the industrial reactor simulation.
    /// Manages simulation state and coordinates all sub-components.
    /// </summary>
    [AddComponentMenu("Industrial Reactor/Reactor Controller")]
    [DisallowMultipleComponent]
    public class ReactorController : MonoBehaviour
    {
        private static ReactorController _instance;
        public static ReactorController Instance => _instance ??= FindObjectOfType<ReactorController>();

        [Header("Component References")]
        [SerializeField] private ValveController inletValve;
        [SerializeField] private ValveController outletValve;
        [SerializeField] private AgitatorController agitator;
        [SerializeField] private WaterSimulator waterSimulator;
        [SerializeField] private TankCutawayController tankCutaway;
        [SerializeField] private BubbleEffectController bubbleEffect;
        [SerializeField] private PipeFlowVisualizer inletPipeFlow;
        [SerializeField] private PipeFlowVisualizer outletPipeFlow;

        [Header("Simulation Configuration")]
        [SerializeField] private ReactorSimulationData simulationData;

        [Header("Runtime State")]
        [SerializeField] private ReactorState currentState = ReactorState.Idle;
        [SerializeField] private float currentWaterLevel = 0f;
        [SerializeField] private float currentTemperature = 25f;
        [SerializeField] private float currentAgitatorRPM = 0f;
        [SerializeField] private float currentBubbleIntensity = 0f;

        [Header("Control Parameters")]
        [Range(0f, 100f)][SerializeField] private float inletFlowSpeed = 20f;
        [Range(0f, 100f)][SerializeField] private float outletFlowSpeed = 15f;
        [Range(0f, 300f)][SerializeField] private float targetAgitatorRPM = 60f;
        [Range(0f, 100f)][SerializeField] private float targetTemperature = 25f;

        // Properties
        public ReactorState CurrentState => currentState;
        public float CurrentWaterLevel => currentWaterLevel;
        public float CurrentTemperature => currentTemperature;
        public float CurrentAgitatorRPM => currentAgitatorRPM;
        public float CurrentBubbleIntensity => currentBubbleIntensity;
        public float TankFillPercentage => currentWaterLevel * 100f;
        public ReactorSimulationData SimulationData => simulationData;
        public ValveController InletValve => inletValve;
        public ValveController OutletValve => outletValve;
        public AgitatorController Agitator => agitator;
        public WaterSimulator WaterSim => waterSimulator;

        public float InletFlowSpeed
        {
            get => inletFlowSpeed;
            set => inletFlowSpeed = Mathf.Clamp(value, 0f, simulationData?.maxInletFlowRate ?? 100f);
        }

        public float OutletFlowSpeed
        {
            get => outletFlowSpeed;
            set => outletFlowSpeed = Mathf.Clamp(value, 0f, simulationData?.maxOutletFlowRate ?? 80f);
        }

        public float TargetAgitatorRPM
        {
            get => targetAgitatorRPM;
            set => targetAgitatorRPM = Mathf.Clamp(value, 0f, simulationData?.maxAgitatorRPM ?? 300f);
        }

        public float TargetTemperature
        {
            get => targetTemperature;
            set => targetTemperature = Mathf.Clamp(value, simulationData?.minTemperature ?? 0f, simulationData?.maxTemperature ?? 100f);
        }

        private void Awake()
        {
            if (_instance != null && _instance != this) { Destroy(gameObject); return; }
            _instance = this;
            if (simulationData == null) simulationData = ScriptableObject.CreateInstance<ReactorSimulationData>();
            InitializeDefaultValues();
        }

        private void OnEnable() => SubscribeToEvents();
        private void OnDisable() => UnsubscribeFromEvents();
        private void OnDestroy() { if (_instance == this) _instance = null; }

        private void Update()
        {
            if (!Application.isPlaying || currentState != ReactorState.Running) return;
            UpdateSimulation();
        }

        private void InitializeDefaultValues()
        {
            if (simulationData == null) return;
            currentWaterLevel = simulationData.initialWaterLevel;
            currentTemperature = simulationData.ambientTemperature;
            inletFlowSpeed = simulationData.defaultInletFlowRate;
            outletFlowSpeed = simulationData.defaultOutletFlowRate;
            targetAgitatorRPM = simulationData.defaultAgitatorRPM;
            targetTemperature = simulationData.ambientTemperature;
        }

        private void SubscribeToEvents()
        {
            ReactorEvents.OnWaterLevelChanged += h => currentWaterLevel = h;
            ReactorEvents.OnWaterTemperatureChanged += h => currentTemperature = h;
            ReactorEvents.OnAgitatorRPMChanged += h => currentAgitatorRPM = h;
            ReactorEvents.OnBubbleIntensityChanged += h => currentBubbleIntensity = h;
        }

        private void UnsubscribeFromEvents() => ReactorEvents.ClearAllEvents();

        public void StartSimulation()
        {
            if (!Application.isPlaying) { Debug.LogWarning("[Reactor] Play Mode required."); return; }
            if (currentState == ReactorState.Running) return;
            SetState(ReactorState.Starting);
            tankCutaway?.ActivateCutaway();
            SetState(ReactorState.Running);
            ReactorEvents.RaiseSimulationStarted();
        }

        public void StopSimulation()
        {
            if (currentState == ReactorState.Idle) return;
            SetState(ReactorState.Stopping);
            inletValve?.CloseValve(); outletValve?.CloseValve();
            agitator?.StopAgitator();
            inletPipeFlow?.StopFlow(); outletPipeFlow?.StopFlow();
            SetState(ReactorState.Idle);
            ReactorEvents.RaiseSimulationStopped();
        }

        public void ResetSimulation()
        {
            if (currentState == ReactorState.Running) StopSimulation();
            inletValve?.ResetValve(); outletValve?.ResetValve();
            agitator?.ResetAgitator(); waterSimulator?.ResetWater();
            tankCutaway?.DeactivateCutaway(); bubbleEffect?.ResetEffect();
            inletPipeFlow?.ResetFlow(); outletPipeFlow?.ResetFlow();
            InitializeDefaultValues();
            currentAgitatorRPM = 0f; currentBubbleIntensity = 0f;
            SetState(ReactorState.Idle);
            ReactorEvents.RaiseSimulationReset();
        }

        public void OpenInletValve() { if (currentState != ReactorState.Running) return; inletValve?.OpenValve(); inletPipeFlow?.StartFlow(FlowDirection.Forward); }
        public void CloseInletValve() { inletValve?.CloseValve(); inletPipeFlow?.StopFlow(); }
        public void OpenOutletValve() { if (currentState != ReactorState.Running) return; outletValve?.OpenValve(); outletPipeFlow?.StartFlow(FlowDirection.Forward); }
        public void CloseOutletValve() { outletValve?.CloseValve(); outletPipeFlow?.StopFlow(); }
        public void StartAgitator() { if (currentState != ReactorState.Running) return; agitator?.StartAgitator(targetAgitatorRPM); }
        public void StopAgitator() => agitator?.StopAgitator();

        public void SetAgitatorRPM(float rpm)
        {
            targetAgitatorRPM = Mathf.Clamp(rpm, 0f, simulationData?.maxAgitatorRPM ?? 300f);
            if (agitator != null && agitator.IsRunning) agitator.SetTargetRPM(targetAgitatorRPM);
        }

        private void UpdateSimulation()
        {
            float deltaTime = Time.deltaTime * (simulationData?.simulationTimeScale ?? 1f);
            UpdateWaterLevel(deltaTime);
            UpdateTemperature(deltaTime);
            UpdateEffects();
        }

        private void UpdateWaterLevel(float deltaTime)
        {
            if (waterSimulator == null) return;
            float inletFlow = inletValve != null && inletValve.IsOpen ? inletFlowSpeed * inletValve.OpenAmount : 0f;
            float outletFlow = outletValve != null && outletValve.IsOpen ? outletFlowSpeed * outletValve.OpenAmount : 0f;
            waterSimulator.UpdateWaterLevel(inletFlow, outletFlow, deltaTime);
        }

        private void UpdateTemperature(float deltaTime)
        {
            if (simulationData == null) return;
            if (currentTemperature < targetTemperature)
                currentTemperature = Mathf.Min(currentTemperature + simulationData.heatingRate * deltaTime, targetTemperature);
            else if (currentTemperature > targetTemperature)
                currentTemperature = Mathf.Max(currentTemperature - simulationData.coolingRate * deltaTime, targetTemperature);
            ReactorEvents.RaiseWaterTemperatureChanged(currentTemperature);
        }

        private void UpdateEffects()
        {
            if (simulationData == null) return;
            float bubbleIntensity = simulationData.GetNormalizedRPM(currentAgitatorRPM) * Mathf.Clamp01(currentWaterLevel);
            bubbleEffect?.SetIntensity(bubbleIntensity);
            waterSimulator?.SetSwirlSpeed(simulationData.GetSwirlSpeed(currentAgitatorRPM));
        }

        private void SetState(ReactorState newState) { if (currentState != newState) { currentState = newState; ReactorEvents.RaiseSimulationStateChanged(currentState); } }
        public string GetStateString() => currentState.ToString();

#if UNITY_EDITOR
        [ContextMenu("Auto Find Components")]
        public void AutoFindComponents()
        {
            foreach (var v in GetComponentsInChildren<ValveController>())
                if (v.ValveType == ValveType.Inlet && inletValve == null) inletValve = v;
                else if (v.ValveType == ValveType.Outlet && outletValve == null) outletValve = v;
            agitator ??= GetComponentInChildren<AgitatorController>();
            waterSimulator ??= GetComponentInChildren<WaterSimulator>();
            tankCutaway ??= GetComponentInChildren<TankCutawayController>();
            bubbleEffect ??= GetComponentInChildren<BubbleEffectController>();
            foreach (var p in GetComponentsInChildren<PipeFlowVisualizer>())
                if (p.AssociatedValveType == ValveType.Inlet && inletPipeFlow == null) inletPipeFlow = p;
                else if (p.AssociatedValveType == ValveType.Outlet && outletPipeFlow == null) outletPipeFlow = p;
            UnityEditor.EditorUtility.SetDirty(this);
        }
#endif
    }
}
