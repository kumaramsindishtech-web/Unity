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
    /// Handles sequential flow: inlet pipe fills → tank fills → outlet pipe drains
    /// </summary>
    [AddComponentMenu("Industrial Reactor/Reactor Controller")]
    [DisallowMultipleComponent]
    public class ReactorController : MonoBehaviour
    {
        private static ReactorController _instance;
        public static ReactorController Instance => _instance ??= FindFirstObjectByType<ReactorController>();

        [Header("Component References")]
        [SerializeField] private ValveController inletValve;
        [SerializeField] private ValveController outletValve;
        [SerializeField] private AgitatorController agitator;
        [SerializeField] private WaterSimulator waterSimulator;
        [SerializeField] private TankCutawayController tankCutaway;
        [SerializeField] private BubbleEffectController bubbleEffect;
        [SerializeField] private PipeFlowVisualizer inletPipeFlow;
        [SerializeField] private PipeFlowVisualizer outletPipeFlow;
        [SerializeField] private SimulationMaterialManager materialManager;

        [Header("Simulation Configuration")]
        [SerializeField] private ReactorSimulationData simulationData;

        [Header("Runtime State")]
        [SerializeField] private ReactorState currentState = ReactorState.Idle;
        [SerializeField] private float currentWaterLevel = 0f;
        [SerializeField] private float currentTemperature = 25f;
        [SerializeField] private float currentAgitatorRPM = 0f;
        [SerializeField] private float currentBubbleIntensity = 0f;

        [Header("Control Parameters (L/s)")]
        [Tooltip("Inlet flow rate in Liters per second")]
        [Range(0f, 100f)][SerializeField] private float inletFlowRateLPS = 20f;
        [Tooltip("Outlet flow rate in Liters per second")]
        [Range(0f, 100f)][SerializeField] private float outletFlowRateLPS = 15f;
        [Range(0f, 300f)][SerializeField] private float targetAgitatorRPM = 60f;
        [Range(0f, 100f)][SerializeField] private float targetTemperature = 25f;


        [Header("Flow State (Read Only)")]
        [SerializeField] private bool inletPipeFilled = false;
#pragma warning disable CS0414 // Field is assigned but its value is never used
        [SerializeField] private bool tankReceivingWater = false;
        [SerializeField] private bool outletPipeDraining = false;
#pragma warning restore CS0414

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

        public float InletFlowRateLPS
        {
            get => inletFlowRateLPS;
            set
            {
                inletFlowRateLPS = Mathf.Clamp(value, 0f, simulationData?.maxInletFlowRateLPS ?? 100f);
                if (inletPipeFlow != null) inletPipeFlow.FlowRateLitersPerSecond = inletFlowRateLPS;
            }
        }

        public float OutletFlowRateLPS
        {
            get => outletFlowRateLPS;
            set
            {
                outletFlowRateLPS = Mathf.Clamp(value, 0f, simulationData?.maxOutletFlowRateLPS ?? 100f);
                if (outletPipeFlow != null) outletPipeFlow.FlowRateLitersPerSecond = outletFlowRateLPS;
            }
        }

        // Legacy compatibility
        public float InletFlowSpeed { get => inletFlowRateLPS; set => InletFlowRateLPS = value; }
        public float OutletFlowSpeed { get => outletFlowRateLPS; set => OutletFlowRateLPS = value; }

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

        private void OnEnable() { SubscribeToEvents(); SubscribeToPipeEvents(); }
        private void OnDisable() { UnsubscribeFromEvents(); UnsubscribeFromPipeEvents(); }
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
            inletFlowRateLPS = simulationData.defaultInletFlowRateLPS;
            outletFlowRateLPS = simulationData.defaultOutletFlowRateLPS;
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

        private void SubscribeToPipeEvents()
        {
            if (inletPipeFlow != null)
            {
                inletPipeFlow.OnPipeFilled += OnInletPipeFilled;
                inletPipeFlow.OnPipeEmptied += OnInletPipeEmptied;
            }
            if (outletPipeFlow != null)
            {
                outletPipeFlow.OnPipeFilled += OnOutletPipeFilled;
                outletPipeFlow.OnPipeEmptied += OnOutletPipeEmptied;
            }
        }

        private void UnsubscribeFromPipeEvents()
        {
            if (inletPipeFlow != null)
            {
                inletPipeFlow.OnPipeFilled -= OnInletPipeFilled;
                inletPipeFlow.OnPipeEmptied -= OnInletPipeEmptied;
            }
            if (outletPipeFlow != null)
            {
                outletPipeFlow.OnPipeFilled -= OnOutletPipeFilled;
                outletPipeFlow.OnPipeEmptied -= OnOutletPipeEmptied;
            }
        }


        // === PIPE FLOW EVENT HANDLERS ===
        private void OnInletPipeFilled()
        {
            inletPipeFilled = true;
            tankReceivingWater = true;
            if (waterSimulator != null) waterSimulator.CanFill = true;
            Debug.Log("[Reactor] Inlet pipe filled - tank now receiving water");
        }

        private void OnInletPipeEmptied()
        {
            inletPipeFilled = false;
            tankReceivingWater = false;
            if (waterSimulator != null) waterSimulator.CanFill = false;
        }

        private void OnOutletPipeFilled() { Debug.Log("[Reactor] Outlet pipe filled"); }
        private void OnOutletPipeEmptied() { outletPipeDraining = false; }

        // === PUBLIC CONTROL METHODS ===
        public void StartSimulation()
        {
            if (!Application.isPlaying) { Debug.LogWarning("[Reactor] Play Mode required."); return; }
            if (currentState == ReactorState.Running) return;

            SetState(ReactorState.Starting);
            materialManager?.ActivateSimulationMode();
            tankCutaway?.ActivateCutaway();

            if (inletPipeFlow != null) inletPipeFlow.FlowRateLitersPerSecond = inletFlowRateLPS;
            if (outletPipeFlow != null) outletPipeFlow.FlowRateLitersPerSecond = outletFlowRateLPS;

            SetState(ReactorState.Running);
            ReactorEvents.RaiseSimulationStarted();
        }

        public void StopSimulation()
        {
            if (currentState == ReactorState.Idle) return;
            SetState(ReactorState.Stopping);

            inletValve?.CloseValve();
            outletValve?.CloseValve();
            agitator?.StopAgitator();
            inletPipeFlow?.StopFlow();
            outletPipeFlow?.StopFlow();

            if (waterSimulator != null) waterSimulator.CanFill = false;
            inletPipeFilled = false;
            tankReceivingWater = false;
            outletPipeDraining = false;

            materialManager?.DeactivateSimulationMode();
            SetState(ReactorState.Idle);
            ReactorEvents.RaiseSimulationStopped();
        }

        public void ResetSimulation()
        {
            if (currentState == ReactorState.Running) StopSimulation();

            inletValve?.ResetValve();
            outletValve?.ResetValve();
            agitator?.ResetAgitator();
            waterSimulator?.ResetWater();
            tankCutaway?.DeactivateCutaway();
            bubbleEffect?.ResetEffect();
            inletPipeFlow?.ResetFlow();
            outletPipeFlow?.ResetFlow();
            materialManager?.DeactivateSimulationMode();

            inletPipeFilled = false;
            tankReceivingWater = false;
            outletPipeDraining = false;

            InitializeDefaultValues();
            currentAgitatorRPM = 0f;
            currentBubbleIntensity = 0f;

            SetState(ReactorState.Idle);
            ReactorEvents.RaiseSimulationReset();
        }


        /// <summary>
        /// Open inlet valve - water flows through inlet pipe first, then fills tank
        /// </summary>
        public void OpenInletValve()
        {
            if (currentState != ReactorState.Running) return;
            inletValve?.OpenValve();
            if (inletPipeFlow != null)
            {
                inletPipeFlow.FlowRateLitersPerSecond = inletFlowRateLPS;
                inletPipeFlow.StartFlow(FlowDirection.Forward);
            }
        }

        public void CloseInletValve()
        {
            inletValve?.CloseValve();
            inletPipeFlow?.StopFlow();
            if (waterSimulator != null) waterSimulator.CanFill = false;
            tankReceivingWater = false;
        }

        /// <summary>
        /// Open outlet valve - water drains from tank through outlet pipe
        /// </summary>
        public void OpenOutletValve()
        {
            if (currentState != ReactorState.Running) return;
            outletValve?.OpenValve();
            outletPipeDraining = true;
            if (outletPipeFlow != null)
            {
                outletPipeFlow.FlowRateLitersPerSecond = outletFlowRateLPS;
                outletPipeFlow.StartFlow(FlowDirection.Forward);
            }
        }

        public void CloseOutletValve()
        {
            outletValve?.CloseValve();
            outletPipeFlow?.StopFlow();
            outletPipeDraining = false;
        }

        public void StartAgitator() { if (currentState != ReactorState.Running) return; agitator?.StartAgitator(targetAgitatorRPM); }
        public void StopAgitator() => agitator?.StopAgitator();

        public void SetAgitatorRPM(float rpm)
        {
            targetAgitatorRPM = Mathf.Clamp(rpm, 0f, simulationData?.maxAgitatorRPM ?? 300f);
            if (agitator != null && agitator.IsRunning) agitator.SetTargetRPM(targetAgitatorRPM);
        }

        public void SetInletFlowRate(float litersPerSecond) => InletFlowRateLPS = litersPerSecond;
        public void SetOutletFlowRate(float litersPerSecond) => OutletFlowRateLPS = litersPerSecond;

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

            // Inlet flow only if valve open AND inlet pipe is filled
            float inletFlow = 0f;
            if (inletValve != null && inletValve.IsOpen && inletPipeFilled)
                inletFlow = inletFlowRateLPS * inletValve.OpenAmount;

            // Outlet flow if valve open
            float outletFlow = 0f;
            if (outletValve != null && outletValve.IsOpen)
            {
                outletFlow = outletFlowRateLPS * outletValve.OpenAmount;
                // Keep outlet pipe filled while draining
                if (outletPipeFlow != null && outletFlow > 0 && !outletPipeFlow.IsFilled && currentWaterLevel > 0.01f)
                    outletPipeFlow.FillInstant();
            }

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

        private void SetState(ReactorState newState)
        {
            if (currentState != newState) { currentState = newState; ReactorEvents.RaiseSimulationStateChanged(currentState); }
        }

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
            materialManager ??= GetComponentInChildren<SimulationMaterialManager>();
            foreach (var p in GetComponentsInChildren<PipeFlowVisualizer>())
                if (p.AssociatedValveType == ValveType.Inlet && inletPipeFlow == null) inletPipeFlow = p;
                else if (p.AssociatedValveType == ValveType.Outlet && outletPipeFlow == null) outletPipeFlow = p;
            UnityEditor.EditorUtility.SetDirty(this);
        }
#endif
    }
}
