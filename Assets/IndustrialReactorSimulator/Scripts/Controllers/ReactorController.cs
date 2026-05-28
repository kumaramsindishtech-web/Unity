// ============================================================================
// Industrial Reactor Simulator - Main Reactor Controller
// Version: 1.0.0
// Description: Central controller managing all reactor components and states
// ============================================================================

using UnityEngine;
using IndustrialReactorSimulator.Core;

namespace IndustrialReactorSimulator.Controllers
{
    /// <summary>
    /// Main controller for the industrial reactor simulation.
    /// Manages simulation state, coordinates all sub-components, and handles
    /// the simulation lifecycle (start, stop, reset).
    /// </summary>
    [AddComponentMenu("Industrial Reactor/Reactor Controller")]
    [DisallowMultipleComponent]
    public class ReactorController : MonoBehaviour
    {
        // ====================================================================
        // SINGLETON INSTANCE
        // ====================================================================
        
        private static ReactorController _instance;
        public static ReactorController Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = FindObjectOfType<ReactorController>();
                }
                return _instance;
            }
        }

        // ====================================================================
        // COMPONENT REFERENCES
        // ====================================================================
        
        [Header("Component References")]
        [Tooltip("Reference to the inlet valve controller")]
        [SerializeField] private ValveController inletValve;
        
        [Tooltip("Reference to the outlet valve controller")]
        [SerializeField] private ValveController outletValve;
        
        [Tooltip("Reference to the agitator controller")]
        [SerializeField] private AgitatorController agitator;
        
        [Tooltip("Reference to the water simulator")]
        [SerializeField] private WaterSimulator waterSimulator;
        
        [Tooltip("Reference to the tank cutaway controller")]
        [SerializeField] private TankCutawayController tankCutaway;
        
        [Tooltip("Reference to the bubble effect controller")]
        [SerializeField] private BubbleEffectController bubbleEffect;
        
        [Tooltip("Reference to inlet pipe flow visualizer")]
        [SerializeField] private PipeFlowVisualizer inletPipeFlow;
        
        [Tooltip("Reference to outlet pipe flow visualizer")]
        [SerializeField] private PipeFlowVisualizer outletPipeFlow;

        // ====================================================================
        // SIMULATION DATA
        // ====================================================================
        
        [Header("Simulation Configuration")]
        [Tooltip("Simulation data asset containing all parameters")]
        [SerializeField] private ReactorSimulationData simulationData;

        // ====================================================================
        // RUNTIME STATE (Read-Only in Inspector)
        // ====================================================================
        
        [Header("Runtime State (Read-Only)")]
        [SerializeField] private ReactorState currentState = ReactorState.Idle;
        [SerializeField] private float currentWaterLevel = 0f;
        [SerializeField] private float currentTemperature = 25f;
        [SerializeField] private float currentAgitatorRPM = 0f;
        [SerializeField] private float currentBubbleIntensity = 0f;

        // ====================================================================
        // CONTROL PARAMETERS (Modified via Editor Window)
        // ====================================================================
        
        [Header("Control Parameters")]
        [Range(0f, 100f)]
        [SerializeField] private float inletFlowSpeed = 20f;
        
        [Range(0f, 100f)]
        [SerializeField] private float outletFlowSpeed = 15f;
        
        [Range(0f, 300f)]
        [SerializeField] private float targetAgitatorRPM = 60f;
        
        [Range(0f, 100f)]
        [SerializeField] private float targetTemperature = 25f;

        // ====================================================================
        // PROPERTIES
        // ====================================================================
        
        public ReactorState CurrentState => currentState;
        public float CurrentWaterLevel => currentWaterLevel;
        public float CurrentTemperature => currentTemperature;
        public float CurrentAgitatorRPM => currentAgitatorRPM;
        public float CurrentBubbleIntensity => currentBubbleIntensity;
        public float TankFillPercentage => currentWaterLevel * 100f;
        public ReactorSimulationData SimulationData => simulationData;
        
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
            set => targetTemperature = Mathf.Clamp(value, simulationData?.minTemperature ?? 0f, 
                                                          simulationData?.maxTemperature ?? 100f);
        }

        // Component accessors
        public ValveController InletValve => inletValve;
        public ValveController OutletValve => outletValve;
        public AgitatorController Agitator => agitator;
        public WaterSimulator WaterSim => waterSimulator;

        // ====================================================================
        // UNITY LIFECYCLE
        // ====================================================================
        
        private void Awake()
        {
            // Singleton pattern
            if (_instance != null && _instance != this)
            {
                Debug.LogWarning("[ReactorController] Multiple instances detected. Destroying duplicate.");
                Destroy(gameObject);
                return;
            }
            _instance = this;
            
            // Create default simulation data if not assigned
            if (simulationData == null)
            {
                simulationData = ScriptableObject.CreateInstance<ReactorSimulationData>();
                Debug.LogWarning("[ReactorController] No SimulationData assigned. Using default values.");
            }
            
            InitializeDefaultValues();
        }

        private void OnEnable()
        {
            SubscribeToEvents();
        }

        private void OnDisable()
        {
            UnsubscribeFromEvents();
        }

        private void Update()
        {
            if (!Application.isPlaying) return;
            
            if (currentState == ReactorState.Running)
            {
                UpdateSimulation();
            }
        }

        private void OnDestroy()
        {
            if (_instance == this)
            {
                _instance = null;
            }
        }

        // ====================================================================
        // INITIALIZATION
        // ====================================================================
        
        private void InitializeDefaultValues()
        {
            if (simulationData != null)
            {
                currentWaterLevel = simulationData.initialWaterLevel;
                currentTemperature = simulationData.ambientTemperature;
                inletFlowSpeed = simulationData.defaultInletFlowRate;
                outletFlowSpeed = simulationData.defaultOutletFlowRate;
                targetAgitatorRPM = simulationData.defaultAgitatorRPM;
                targetTemperature = simulationData.ambientTemperature;
            }
        }

        // ====================================================================
        // EVENT SUBSCRIPTION
        // ====================================================================
        
        private void SubscribeToEvents()
        {
            ReactorEvents.OnWaterLevelChanged += HandleWaterLevelChanged;
            ReactorEvents.OnWaterTemperatureChanged += HandleTemperatureChanged;
            ReactorEvents.OnAgitatorRPMChanged += HandleAgitatorRPMChanged;
            ReactorEvents.OnBubbleIntensityChanged += HandleBubbleIntensityChanged;
        }

        private void UnsubscribeFromEvents()
        {
            ReactorEvents.OnWaterLevelChanged -= HandleWaterLevelChanged;
            ReactorEvents.OnWaterTemperatureChanged -= HandleTemperatureChanged;
            ReactorEvents.OnAgitatorRPMChanged -= HandleAgitatorRPMChanged;
            ReactorEvents.OnBubbleIntensityChanged -= HandleBubbleIntensityChanged;
        }

        // ====================================================================
        // EVENT HANDLERS
        // ====================================================================
        
        private void HandleWaterLevelChanged(float level)
        {
            currentWaterLevel = level;
        }

        private void HandleTemperatureChanged(float temp)
        {
            currentTemperature = temp;
        }

        private void HandleAgitatorRPMChanged(float rpm)
        {
            currentAgitatorRPM = rpm;
        }

        private void HandleBubbleIntensityChanged(float intensity)
        {
            currentBubbleIntensity = intensity;
        }

        // ====================================================================
        // SIMULATION CONTROL
        // ====================================================================
        
        /// <summary>
        /// Start the reactor simulation
        /// </summary>
        public void StartSimulation()
        {
            if (!Application.isPlaying)
            {
                Debug.LogWarning("[ReactorController] Simulation can only run in Play Mode.");
                return;
            }
            
            if (currentState == ReactorState.Running)
            {
                Debug.Log("[ReactorController] Simulation already running.");
                return;
            }

            Debug.Log("[ReactorController] Starting simulation...");
            
            SetState(ReactorState.Starting);
            
            // Activate cutaway view
            if (tankCutaway != null)
            {
                tankCutaway.ActivateCutaway();
            }
            
            SetState(ReactorState.Running);
            ReactorEvents.RaiseSimulationStarted();
            
            Debug.Log("[ReactorController] Simulation started.");
        }

        /// <summary>
        /// Stop the reactor simulation
        /// </summary>
        public void StopSimulation()
        {
            if (currentState == ReactorState.Idle || currentState == ReactorState.Stopped)
            {
                Debug.Log("[ReactorController] Simulation not running.");
                return;
            }

            Debug.Log("[ReactorController] Stopping simulation...");
            
            SetState(ReactorState.Stopping);
            
            // Close all valves
            if (inletValve != null) inletValve.CloseValve();
            if (outletValve != null) outletValve.CloseValve();
            
            // Stop agitator
            if (agitator != null) agitator.StopAgitator();
            
            // Stop pipe flow visuals
            if (inletPipeFlow != null) inletPipeFlow.StopFlow();
            if (outletPipeFlow != null) outletPipeFlow.StopFlow();
            
            SetState(ReactorState.Idle);
            ReactorEvents.RaiseSimulationStopped();
            
            Debug.Log("[ReactorController] Simulation stopped.");
        }

        /// <summary>
        /// Reset the reactor to initial state
        /// </summary>
        public void ResetSimulation()
        {
            Debug.Log("[ReactorController] Resetting simulation...");
            
            // Stop if running
            if (currentState == ReactorState.Running)
            {
                StopSimulation();
            }
            
            // Reset all components
            if (inletValve != null) inletValve.ResetValve();
            if (outletValve != null) outletValve.ResetValve();
            if (agitator != null) agitator.ResetAgitator();
            if (waterSimulator != null) waterSimulator.ResetWater();
            if (tankCutaway != null) tankCutaway.DeactivateCutaway();
            if (bubbleEffect != null) bubbleEffect.ResetEffect();
            if (inletPipeFlow != null) inletPipeFlow.ResetFlow();
            if (outletPipeFlow != null) outletPipeFlow.ResetFlow();
            
            // Reset values
            InitializeDefaultValues();
            currentAgitatorRPM = 0f;
            currentBubbleIntensity = 0f;
            
            SetState(ReactorState.Idle);
            ReactorEvents.RaiseSimulationReset();
            
            Debug.Log("[ReactorController] Simulation reset complete.");
        }

        // ====================================================================
        // VALVE CONTROLS
        // ====================================================================
        
        /// <summary>
        /// Open the inlet valve
        /// </summary>
        public void OpenInletValve()
        {
            if (currentState != ReactorState.Running)
            {
                Debug.LogWarning("[ReactorController] Cannot control valves when simulation is not running.");
                return;
            }
            
            if (inletValve != null)
            {
                inletValve.OpenValve();
                if (inletPipeFlow != null)
                {
                    inletPipeFlow.StartFlow(FlowDirection.Forward);
                }
            }
        }

        /// <summary>
        /// Close the inlet valve
        /// </summary>
        public void CloseInletValve()
        {
            if (inletValve != null)
            {
                inletValve.CloseValve();
                if (inletPipeFlow != null)
                {
                    inletPipeFlow.StopFlow();
                }
            }
        }

        /// <summary>
        /// Open the outlet valve
        /// </summary>
        public void OpenOutletValve()
        {
            if (currentState != ReactorState.Running)
            {
                Debug.LogWarning("[ReactorController] Cannot control valves when simulation is not running.");
                return;
            }
            
            if (outletValve != null)
            {
                outletValve.OpenValve();
                if (outletPipeFlow != null)
                {
                    outletPipeFlow.StartFlow(FlowDirection.Forward);
                }
            }
        }

        /// <summary>
        /// Close the outlet valve
        /// </summary>
        public void CloseOutletValve()
        {
            if (outletValve != null)
            {
                outletValve.CloseValve();
                if (outletPipeFlow != null)
                {
                    outletPipeFlow.StopFlow();
                }
            }
        }

        // ====================================================================
        // AGITATOR CONTROLS
        // ====================================================================
        
        /// <summary>
        /// Start the agitator
        /// </summary>
        public void StartAgitator()
        {
            if (currentState != ReactorState.Running)
            {
                Debug.LogWarning("[ReactorController] Cannot control agitator when simulation is not running.");
                return;
            }
            
            if (agitator != null)
            {
                agitator.StartAgitator(targetAgitatorRPM);
            }
        }

        /// <summary>
        /// Stop the agitator
        /// </summary>
        public void StopAgitator()
        {
            if (agitator != null)
            {
                agitator.StopAgitator();
            }
        }

        /// <summary>
        /// Set agitator RPM while running
        /// </summary>
        public void SetAgitatorRPM(float rpm)
        {
            targetAgitatorRPM = Mathf.Clamp(rpm, 0f, simulationData?.maxAgitatorRPM ?? 300f);
            
            if (agitator != null && agitator.CurrentState == AgitatorState.Running)
            {
                agitator.SetTargetRPM(targetAgitatorRPM);
            }
        }

        // ====================================================================
        // SIMULATION UPDATE
        // ====================================================================
        
        private void UpdateSimulation()
        {
            float deltaTime = Time.deltaTime * (simulationData?.simulationTimeScale ?? 1f);
            
            // Update water level based on valve states
            UpdateWaterLevel(deltaTime);
            
            // Update temperature
            UpdateTemperature(deltaTime);
            
            // Update visual effects based on agitator
            UpdateEffects();
        }

        private void UpdateWaterLevel(float deltaTime)
        {
            if (waterSimulator == null) return;
            
            float inletFlow = 0f;
            float outletFlow = 0f;
            
            // Calculate inlet flow
            if (inletValve != null && inletValve.CurrentState == ValveState.Open)
            {
                inletFlow = inletFlowSpeed * inletValve.OpenAmount;
            }
            
            // Calculate outlet flow
            if (outletValve != null && outletValve.CurrentState == ValveState.Open)
            {
                outletFlow = outletFlowSpeed * outletValve.OpenAmount;
            }
            
            // Apply flow to water simulator
            waterSimulator.UpdateWaterLevel(inletFlow, outletFlow, deltaTime);
        }

        private void UpdateTemperature(float deltaTime)
        {
            if (simulationData == null) return;
            
            // Gradually move current temperature toward target
            if (currentTemperature < targetTemperature)
            {
                currentTemperature += simulationData.heatingRate * deltaTime;
                currentTemperature = Mathf.Min(currentTemperature, targetTemperature);
            }
            else if (currentTemperature > targetTemperature)
            {
                currentTemperature -= simulationData.coolingRate * deltaTime;
                currentTemperature = Mathf.Max(currentTemperature, targetTemperature);
            }
            
            ReactorEvents.RaiseWaterTemperatureChanged(currentTemperature);
        }

        private void UpdateEffects()
        {
            if (simulationData == null) return;
            
            // Calculate bubble intensity based on agitator RPM and water level
            float rpmFactor = simulationData.GetNormalizedRPM(currentAgitatorRPM);
            float waterFactor = Mathf.Clamp01(currentWaterLevel);
            float bubbleIntensity = rpmFactor * waterFactor;
            
            if (bubbleEffect != null)
            {
                bubbleEffect.SetIntensity(bubbleIntensity);
            }
            
            // Update water shader swirl
            if (waterSimulator != null)
            {
                float swirlSpeed = simulationData.GetSwirlSpeed(currentAgitatorRPM);
                waterSimulator.SetSwirlSpeed(swirlSpeed);
            }
        }

        // ====================================================================
        // STATE MANAGEMENT
        // ====================================================================
        
        private void SetState(ReactorState newState)
        {
            if (currentState != newState)
            {
                currentState = newState;
                ReactorEvents.RaiseSimulationStateChanged(currentState);
            }
        }

        /// <summary>
        /// Get state as readable string
        /// </summary>
        public string GetStateString()
        {
            return currentState.ToString();
        }

        // ====================================================================
        // EDITOR HELPERS
        // ====================================================================
        
        #if UNITY_EDITOR
        /// <summary>
        /// Auto-find component references in editor
        /// </summary>
        [ContextMenu("Auto Find Components")]
        public void AutoFindComponents()
        {
            // Find valve controllers
            ValveController[] valves = GetComponentsInChildren<ValveController>();
            foreach (var valve in valves)
            {
                if (valve.ValveType == ValveType.Inlet && inletValve == null)
                    inletValve = valve;
                else if (valve.ValveType == ValveType.Outlet && outletValve == null)
                    outletValve = valve;
            }
            
            // Find other components
            if (agitator == null)
                agitator = GetComponentInChildren<AgitatorController>();
            if (waterSimulator == null)
                waterSimulator = GetComponentInChildren<WaterSimulator>();
            if (tankCutaway == null)
                tankCutaway = GetComponentInChildren<TankCutawayController>();
            if (bubbleEffect == null)
                bubbleEffect = GetComponentInChildren<BubbleEffectController>();
            
            // Find pipe flow visualizers
            PipeFlowVisualizer[] pipes = GetComponentsInChildren<PipeFlowVisualizer>();
            foreach (var pipe in pipes)
            {
                if (pipe.AssociatedValveType == ValveType.Inlet && inletPipeFlow == null)
                    inletPipeFlow = pipe;
                else if (pipe.AssociatedValveType == ValveType.Outlet && outletPipeFlow == null)
                    outletPipeFlow = pipe;
            }
            
            UnityEditor.EditorUtility.SetDirty(this);
            Debug.Log("[ReactorController] Auto-find complete.");
        }
        #endif
    }
}
