// ============================================================================
// Industrial Reactor Simulator - Simulation Data ScriptableObject
// Version: 1.0.0
// Description: Configurable simulation parameters as reusable asset
// ============================================================================

using UnityEngine;

namespace IndustrialReactorSimulator.Core
{
    /// <summary>
    /// ScriptableObject containing all configurable simulation parameters.
    /// Create instances via Assets > Create > Industrial Reactor > Simulation Data
    /// </summary>
    [CreateAssetMenu(fileName = "ReactorSimulationData", menuName = "Industrial Reactor/Simulation Data", order = 1)]
    public class ReactorSimulationData : ScriptableObject
    {
        // ====================================================================
        // GENERAL SETTINGS
        // ====================================================================
        
        [Header("General Settings")]
        [Tooltip("Name identifier for this simulation configuration")]
        public string configurationName = "Default Configuration";
        
        [Tooltip("Time scale multiplier for simulation speed")]
        [Range(0.1f, 5f)]
        public float simulationTimeScale = 1f;

        // ====================================================================
        // TANK SETTINGS
        // ====================================================================
        
        [Header("Tank Settings")]
        [Tooltip("Maximum tank capacity in liters")]
        public float tankCapacity = 1000f;
        
        [Tooltip("Initial water level (0-1 normalized)")]
        [Range(0f, 1f)]
        public float initialWaterLevel = 0f;
        
        [Tooltip("Minimum water level before empty state")]
        [Range(0f, 0.1f)]
        public float emptyThreshold = 0.01f;
        
        [Tooltip("Maximum water level before full state")]
        [Range(0.9f, 1f)]
        public float fullThreshold = 0.99f;

        // ====================================================================
        // VALVE SETTINGS
        // ====================================================================
        
        [Header("Valve Settings")]
        [Tooltip("Time to fully open/close valves in seconds")]
        [Range(0.1f, 5f)]
        public float valveTransitionTime = 0.5f;
        
        [Tooltip("Default inlet flow rate (liters per second)")]
        [Range(1f, 100f)]
        public float defaultInletFlowRate = 20f;
        
        [Tooltip("Default outlet flow rate (liters per second)")]
        [Range(1f, 100f)]
        public float defaultOutletFlowRate = 15f;
        
        [Tooltip("Maximum inlet flow rate")]
        [Range(10f, 200f)]
        public float maxInletFlowRate = 100f;
        
        [Tooltip("Maximum outlet flow rate")]
        [Range(10f, 200f)]
        public float maxOutletFlowRate = 80f;

        // ====================================================================
        // AGITATOR SETTINGS
        // ====================================================================
        
        [Header("Agitator Settings")]
        [Tooltip("Default agitator RPM")]
        [Range(10f, 500f)]
        public float defaultAgitatorRPM = 60f;
        
        [Tooltip("Maximum agitator RPM")]
        [Range(100f, 1000f)]
        public float maxAgitatorRPM = 300f;
        
        [Tooltip("Time for agitator to reach target RPM")]
        [Range(0.5f, 10f)]
        public float agitatorSpinUpTime = 2f;
        
        [Tooltip("Time for agitator to stop from max RPM")]
        [Range(0.5f, 10f)]
        public float agitatorSpinDownTime = 3f;
        
        [Tooltip("Rotation axis for agitator (local space)")]
        public Vector3 agitatorRotationAxis = Vector3.up;

        // ====================================================================
        // TEMPERATURE SETTINGS
        // ====================================================================
        
        [Header("Temperature Settings")]
        [Tooltip("Ambient/initial temperature in Celsius")]
        public float ambientTemperature = 25f;
        
        [Tooltip("Minimum temperature")]
        public float minTemperature = 0f;
        
        [Tooltip("Maximum temperature")]
        public float maxTemperature = 100f;
        
        [Tooltip("Temperature change rate per second when heating")]
        [Range(0.1f, 10f)]
        public float heatingRate = 2f;
        
        [Tooltip("Temperature change rate per second when cooling")]
        [Range(0.1f, 10f)]
        public float coolingRate = 1f;

        // ====================================================================
        // VISUAL EFFECT SETTINGS
        // ====================================================================
        
        [Header("Bubble Effect Settings")]
        [Tooltip("Minimum bubble emission rate")]
        public float minBubbleRate = 0f;
        
        [Tooltip("Maximum bubble emission rate")]
        public float maxBubbleRate = 100f;
        
        [Tooltip("Bubble size multiplier")]
        [Range(0.1f, 2f)]
        public float bubbleSizeMultiplier = 1f;
        
        [Tooltip("Time for bubble effect to fade in/out")]
        [Range(0.1f, 3f)]
        public float bubbleFadeTime = 1f;

        [Header("Swirl Effect Settings")]
        [Tooltip("Maximum swirl rotation speed")]
        [Range(0f, 360f)]
        public float maxSwirlSpeed = 180f;
        
        [Tooltip("Time for swirl to fade in/out")]
        [Range(0.1f, 5f)]
        public float swirlFadeTime = 2f;

        [Header("Glow Effect Settings")]
        [Tooltip("Base water emission color")]
        public Color waterEmissionColor = new Color(0.2f, 0.5f, 1f, 1f);
        
        [Tooltip("Maximum emission intensity")]
        [Range(0f, 5f)]
        public float maxEmissionIntensity = 2f;
        
        [Tooltip("Emission pulse speed")]
        [Range(0f, 5f)]
        public float emissionPulseSpeed = 1f;

        // ====================================================================
        // PIPE FLOW SETTINGS
        // ====================================================================
        
        [Header("Pipe Flow Settings")]
        [Tooltip("UV scroll speed for pipe flow effect")]
        [Range(0.1f, 10f)]
        public float pipeFlowScrollSpeed = 2f;
        
        [Tooltip("Pipe flow intensity multiplier")]
        [Range(0.1f, 2f)]
        public float pipeFlowIntensity = 1f;

        // ====================================================================
        // CUTAWAY SETTINGS
        // ====================================================================
        
        [Header("Cutaway Settings")]
        [Tooltip("Cutaway plane normal direction")]
        public Vector3 cutawayNormal = Vector3.right;
        
        [Tooltip("Cutaway transition time")]
        [Range(0.1f, 2f)]
        public float cutawayTransitionTime = 0.5f;

        // ====================================================================
        // HELPER METHODS
        // ====================================================================

        /// <summary>
        /// Get normalized RPM value (0-1) from actual RPM
        /// </summary>
        public float GetNormalizedRPM(float rpm)
        {
            return Mathf.Clamp01(rpm / maxAgitatorRPM);
        }

        /// <summary>
        /// Get bubble intensity based on agitator RPM
        /// </summary>
        public float GetBubbleIntensity(float rpm)
        {
            return Mathf.Lerp(0f, 1f, GetNormalizedRPM(rpm));
        }

        /// <summary>
        /// Get swirl speed based on agitator RPM
        /// </summary>
        public float GetSwirlSpeed(float rpm)
        {
            return Mathf.Lerp(0f, maxSwirlSpeed, GetNormalizedRPM(rpm));
        }

        /// <summary>
        /// Calculate fill time based on flow rate and tank capacity
        /// </summary>
        public float CalculateFillTime(float flowRate)
        {
            if (flowRate <= 0f) return float.MaxValue;
            return tankCapacity / flowRate;
        }

        /// <summary>
        /// Reset to default values
        /// </summary>
        public void ResetToDefaults()
        {
            configurationName = "Default Configuration";
            simulationTimeScale = 1f;
            tankCapacity = 1000f;
            initialWaterLevel = 0f;
            emptyThreshold = 0.01f;
            fullThreshold = 0.99f;
            valveTransitionTime = 0.5f;
            defaultInletFlowRate = 20f;
            defaultOutletFlowRate = 15f;
            maxInletFlowRate = 100f;
            maxOutletFlowRate = 80f;
            defaultAgitatorRPM = 60f;
            maxAgitatorRPM = 300f;
            agitatorSpinUpTime = 2f;
            agitatorSpinDownTime = 3f;
            agitatorRotationAxis = Vector3.up;
            ambientTemperature = 25f;
            minTemperature = 0f;
            maxTemperature = 100f;
            heatingRate = 2f;
            coolingRate = 1f;
            minBubbleRate = 0f;
            maxBubbleRate = 100f;
            bubbleSizeMultiplier = 1f;
            bubbleFadeTime = 1f;
            maxSwirlSpeed = 180f;
            swirlFadeTime = 2f;
            waterEmissionColor = new Color(0.2f, 0.5f, 1f, 1f);
            maxEmissionIntensity = 2f;
            emissionPulseSpeed = 1f;
            pipeFlowScrollSpeed = 2f;
            pipeFlowIntensity = 1f;
            cutawayNormal = Vector3.right;
            cutawayTransitionTime = 0.5f;
        }
    }
}
