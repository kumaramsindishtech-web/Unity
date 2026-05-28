// ============================================================================
// Industrial Reactor Simulator - Event System
// Version: 1.0.0
// Description: Event system for decoupled reactor component communication
// ============================================================================

using System;
using UnityEngine;

namespace IndustrialReactorSimulator.Core
{
    /// <summary>
    /// Central event hub for reactor simulation communication.
    /// Uses C# events for clean, decoupled architecture.
    /// </summary>
    public static class ReactorEvents
    {
        // ====================================================================
        // SIMULATION STATE EVENTS
        // ====================================================================
        
        /// <summary>Fired when simulation state changes</summary>
        public static event Action<ReactorState> OnSimulationStateChanged;
        
        /// <summary>Fired when simulation starts</summary>
        public static event Action OnSimulationStarted;
        
        /// <summary>Fired when simulation stops</summary>
        public static event Action OnSimulationStopped;
        
        /// <summary>Fired when simulation is reset</summary>
        public static event Action OnSimulationReset;

        // ====================================================================
        // VALVE EVENTS
        // ====================================================================
        
        /// <summary>Fired when any valve state changes (ValveType, ValveState)</summary>
        public static event Action<ValveType, ValveState> OnValveStateChanged;
        
        /// <summary>Fired when inlet valve opens</summary>
        public static event Action OnInletValveOpened;
        
        /// <summary>Fired when inlet valve closes</summary>
        public static event Action OnInletValveClosed;
        
        /// <summary>Fired when outlet valve opens</summary>
        public static event Action OnOutletValveOpened;
        
        /// <summary>Fired when outlet valve closes</summary>
        public static event Action OnOutletValveClosed;

        // ====================================================================
        // AGITATOR EVENTS
        // ====================================================================
        
        /// <summary>Fired when agitator state changes</summary>
        public static event Action<AgitatorState> OnAgitatorStateChanged;
        
        /// <summary>Fired when agitator RPM changes (current RPM)</summary>
        public static event Action<float> OnAgitatorRPMChanged;
        
        /// <summary>Fired when agitator starts</summary>
        public static event Action OnAgitatorStarted;
        
        /// <summary>Fired when agitator stops</summary>
        public static event Action OnAgitatorStopped;

        // ====================================================================
        // WATER/TANK EVENTS
        // ====================================================================
        
        /// <summary>Fired when water level changes (0-1 normalized)</summary>
        public static event Action<float> OnWaterLevelChanged;
        
        /// <summary>Fired when water temperature changes</summary>
        public static event Action<float> OnWaterTemperatureChanged;
        
        /// <summary>Fired when tank is full</summary>
        public static event Action OnTankFull;
        
        /// <summary>Fired when tank is empty</summary>
        public static event Action OnTankEmpty;

        // ====================================================================
        // EFFECT EVENTS
        // ====================================================================
        
        /// <summary>Fired when bubble intensity changes (0-1)</summary>
        public static event Action<float> OnBubbleIntensityChanged;
        
        /// <summary>Fired when swirl intensity changes (0-1)</summary>
        public static event Action<float> OnSwirlIntensityChanged;

        // ====================================================================
        // EVENT INVOKERS (Internal use by controllers)
        // ====================================================================

        #region Simulation Invokers
        public static void RaiseSimulationStateChanged(ReactorState state)
        {
            OnSimulationStateChanged?.Invoke(state);
        }

        public static void RaiseSimulationStarted()
        {
            OnSimulationStarted?.Invoke();
        }

        public static void RaiseSimulationStopped()
        {
            OnSimulationStopped?.Invoke();
        }

        public static void RaiseSimulationReset()
        {
            OnSimulationReset?.Invoke();
        }
        #endregion

        #region Valve Invokers
        public static void RaiseValveStateChanged(ValveType type, ValveState state)
        {
            OnValveStateChanged?.Invoke(type, state);
            
            // Also raise specific events
            if (type == ValveType.Inlet)
            {
                if (state == ValveState.Open) OnInletValveOpened?.Invoke();
                else if (state == ValveState.Closed) OnInletValveClosed?.Invoke();
            }
            else
            {
                if (state == ValveState.Open) OnOutletValveOpened?.Invoke();
                else if (state == ValveState.Closed) OnOutletValveClosed?.Invoke();
            }
        }
        #endregion

        #region Agitator Invokers
        public static void RaiseAgitatorStateChanged(AgitatorState state)
        {
            OnAgitatorStateChanged?.Invoke(state);
            
            if (state == AgitatorState.Running) OnAgitatorStarted?.Invoke();
            else if (state == AgitatorState.Stopped) OnAgitatorStopped?.Invoke();
        }

        public static void RaiseAgitatorRPMChanged(float rpm)
        {
            OnAgitatorRPMChanged?.Invoke(rpm);
        }
        #endregion

        #region Water/Tank Invokers
        public static void RaiseWaterLevelChanged(float level)
        {
            OnWaterLevelChanged?.Invoke(level);
            
            if (level >= 1f) OnTankFull?.Invoke();
            else if (level <= 0f) OnTankEmpty?.Invoke();
        }

        public static void RaiseWaterTemperatureChanged(float temperature)
        {
            OnWaterTemperatureChanged?.Invoke(temperature);
        }
        #endregion

        #region Effect Invokers
        public static void RaiseBubbleIntensityChanged(float intensity)
        {
            OnBubbleIntensityChanged?.Invoke(intensity);
        }

        public static void RaiseSwirlIntensityChanged(float intensity)
        {
            OnSwirlIntensityChanged?.Invoke(intensity);
        }
        #endregion

        /// <summary>
        /// Clears all event subscriptions. Call when cleaning up.
        /// </summary>
        public static void ClearAllEvents()
        {
            OnSimulationStateChanged = null;
            OnSimulationStarted = null;
            OnSimulationStopped = null;
            OnSimulationReset = null;
            OnValveStateChanged = null;
            OnInletValveOpened = null;
            OnInletValveClosed = null;
            OnOutletValveOpened = null;
            OnOutletValveClosed = null;
            OnAgitatorStateChanged = null;
            OnAgitatorRPMChanged = null;
            OnAgitatorStarted = null;
            OnAgitatorStopped = null;
            OnWaterLevelChanged = null;
            OnWaterTemperatureChanged = null;
            OnTankFull = null;
            OnTankEmpty = null;
            OnBubbleIntensityChanged = null;
            OnSwirlIntensityChanged = null;
        }
    }
}
