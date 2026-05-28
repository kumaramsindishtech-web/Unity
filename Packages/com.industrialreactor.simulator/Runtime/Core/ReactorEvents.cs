// ============================================================================
// Industrial Reactor Simulator - Event System
// Copyright (c) 2024 Industrial Reactor Simulator. MIT License.
// ============================================================================

using System;

namespace IndustrialReactorSimulator
{
    /// <summary>
    /// Central event hub for reactor simulation communication.
    /// </summary>
    public static class ReactorEvents
    {
        // Simulation State Events
        public static event Action<ReactorState> OnSimulationStateChanged;
        public static event Action OnSimulationStarted;
        public static event Action OnSimulationStopped;
        public static event Action OnSimulationReset;

        // Valve Events
        public static event Action<ValveType, ValveState> OnValveStateChanged;
        public static event Action OnInletValveOpened;
        public static event Action OnInletValveClosed;
        public static event Action OnOutletValveOpened;
        public static event Action OnOutletValveClosed;

        // Agitator Events
        public static event Action<AgitatorState> OnAgitatorStateChanged;
        public static event Action<float> OnAgitatorRPMChanged;
        public static event Action OnAgitatorStarted;
        public static event Action OnAgitatorStopped;

        // Water/Tank Events
        public static event Action<float> OnWaterLevelChanged;
        public static event Action<float> OnWaterTemperatureChanged;
        public static event Action OnTankFull;
        public static event Action OnTankEmpty;

        // Effect Events
        public static event Action<float> OnBubbleIntensityChanged;
        public static event Action<float> OnSwirlIntensityChanged;

        // Event Invokers
        public static void RaiseSimulationStateChanged(ReactorState state) => OnSimulationStateChanged?.Invoke(state);
        public static void RaiseSimulationStarted() => OnSimulationStarted?.Invoke();
        public static void RaiseSimulationStopped() => OnSimulationStopped?.Invoke();
        public static void RaiseSimulationReset() => OnSimulationReset?.Invoke();

        public static void RaiseValveStateChanged(ValveType type, ValveState state)
        {
            OnValveStateChanged?.Invoke(type, state);
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

        public static void RaiseAgitatorStateChanged(AgitatorState state)
        {
            OnAgitatorStateChanged?.Invoke(state);
            if (state == AgitatorState.Running) OnAgitatorStarted?.Invoke();
            else if (state == AgitatorState.Stopped) OnAgitatorStopped?.Invoke();
        }

        public static void RaiseAgitatorRPMChanged(float rpm) => OnAgitatorRPMChanged?.Invoke(rpm);

        public static void RaiseWaterLevelChanged(float level)
        {
            OnWaterLevelChanged?.Invoke(level);
            if (level >= 1f) OnTankFull?.Invoke();
            else if (level <= 0f) OnTankEmpty?.Invoke();
        }

        public static void RaiseWaterTemperatureChanged(float temperature) => OnWaterTemperatureChanged?.Invoke(temperature);
        public static void RaiseBubbleIntensityChanged(float intensity) => OnBubbleIntensityChanged?.Invoke(intensity);
        public static void RaiseSwirlIntensityChanged(float intensity) => OnSwirlIntensityChanged?.Invoke(intensity);

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
