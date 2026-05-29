// ============================================================================
// Industrial Reactor Simulator - Simulation Data ScriptableObject
// Copyright (c) 2024 Industrial Reactor Simulator. MIT License.
// ============================================================================

using UnityEngine;

namespace IndustrialReactorSimulator
{
    /// <summary>
    /// ScriptableObject containing all configurable simulation parameters.
    /// Create via Assets > Create > Industrial Reactor > Simulation Data
    /// </summary>
    [CreateAssetMenu(fileName = "ReactorSimulationData", menuName = "Industrial Reactor/Simulation Data", order = 1)]
    public class ReactorSimulationData : ScriptableObject
    {
        [Header("General Settings")]
        public string configurationName = "Default Configuration";
        [Range(0.1f, 5f)] public float simulationTimeScale = 1f;

        [Header("Tank Settings")]
        public float tankCapacity = 1000f;
        [Range(0f, 1f)] public float initialWaterLevel = 0f;
        [Range(0f, 0.1f)] public float emptyThreshold = 0.01f;
        [Range(0.9f, 1f)] public float fullThreshold = 0.99f;

        [Header("Valve Settings")]
        [Range(0.1f, 5f)] public float valveTransitionTime = 0.5f;

        [Header("Flow Rate Settings (Liters per Second)")]
        [Tooltip("Default inlet flow rate in L/s")]
        [Range(1f, 100f)] public float defaultInletFlowRateLPS = 20f;
        [Tooltip("Default outlet flow rate in L/s")]
        [Range(1f, 100f)] public float defaultOutletFlowRateLPS = 15f;
        [Tooltip("Maximum inlet flow rate in L/s")]
        [Range(10f, 200f)] public float maxInletFlowRateLPS = 100f;
        [Tooltip("Maximum outlet flow rate in L/s")]
        [Range(10f, 200f)] public float maxOutletFlowRateLPS = 80f;

        [Header("Pipe Settings")]
        [Tooltip("Default pipe length in meters")]
        [Range(0.5f, 10f)] public float defaultPipeLength = 2f;
        [Tooltip("Default pipe cross-section area in m²")]
        [Range(0.001f, 0.1f)] public float defaultPipeCrossSectionArea = 0.01f;

        // Legacy compatibility
        public float defaultInletFlowRate => defaultInletFlowRateLPS;
        public float defaultOutletFlowRate => defaultOutletFlowRateLPS;
        public float maxInletFlowRate => maxInletFlowRateLPS;
        public float maxOutletFlowRate => maxOutletFlowRateLPS;

        [Header("Agitator Settings")]
        [Range(10f, 500f)] public float defaultAgitatorRPM = 60f;
        [Range(100f, 1000f)] public float maxAgitatorRPM = 300f;
        [Range(0.5f, 10f)] public float agitatorSpinUpTime = 2f;
        [Range(0.5f, 10f)] public float agitatorSpinDownTime = 3f;
        public Vector3 agitatorRotationAxis = Vector3.up;

        [Header("Temperature Settings")]
        public float ambientTemperature = 25f;
        public float minTemperature = 0f;
        public float maxTemperature = 100f;
        [Range(0.1f, 10f)] public float heatingRate = 2f;
        [Range(0.1f, 10f)] public float coolingRate = 1f;

        [Header("Bubble Effect Settings")]
        public float minBubbleRate = 0f;
        public float maxBubbleRate = 100f;
        [Range(0.1f, 2f)] public float bubbleSizeMultiplier = 1f;
        [Range(0.1f, 3f)] public float bubbleFadeTime = 1f;

        [Header("Swirl Effect Settings")]
        [Range(0f, 360f)] public float maxSwirlSpeed = 180f;
        [Range(0.1f, 5f)] public float swirlFadeTime = 2f;

        [Header("Glow Effect Settings")]
        public Color waterEmissionColor = new Color(0.2f, 0.5f, 1f, 1f);
        [Range(0f, 5f)] public float maxEmissionIntensity = 2f;
        [Range(0f, 5f)] public float emissionPulseSpeed = 1f;

        [Header("Pipe Flow Settings")]
        [Range(0.1f, 10f)] public float pipeFlowScrollSpeed = 2f;
        [Range(0.1f, 2f)] public float pipeFlowIntensity = 1f;

        [Header("Cutaway Settings")]
        public Vector3 cutawayNormal = Vector3.right;
        [Range(0.1f, 2f)] public float cutawayTransitionTime = 0.5f;

        public float GetNormalizedRPM(float rpm) => Mathf.Clamp01(rpm / maxAgitatorRPM);
        public float GetBubbleIntensity(float rpm) => Mathf.Lerp(0f, 1f, GetNormalizedRPM(rpm));
        public float GetSwirlSpeed(float rpm) => Mathf.Lerp(0f, maxSwirlSpeed, GetNormalizedRPM(rpm));
        
        /// <summary>
        /// Calculate time to fill tank at given flow rate
        /// </summary>
        public float CalculateFillTime(float flowRateLPS) => flowRateLPS <= 0f ? float.MaxValue : tankCapacity / flowRateLPS;
        
        /// <summary>
        /// Calculate time to fill a pipe based on its dimensions and flow rate
        /// </summary>
        public float CalculatePipeFillTime(float pipeLengthMeters, float crossSectionM2, float flowRateLPS)
        {
            if (flowRateLPS <= 0f) return float.MaxValue;
            float volumeLiters = pipeLengthMeters * crossSectionM2 * 1000f; // Convert m³ to liters
            return volumeLiters / flowRateLPS;
        }

        public void ResetToDefaults()
        {
            configurationName = "Default Configuration";
            simulationTimeScale = 1f;
            tankCapacity = 1000f;
            initialWaterLevel = 0f;
            emptyThreshold = 0.01f;
            fullThreshold = 0.99f;
            valveTransitionTime = 0.5f;
            defaultInletFlowRateLPS = 20f;
            defaultOutletFlowRateLPS = 15f;
            maxInletFlowRateLPS = 100f;
            maxOutletFlowRateLPS = 80f;
            defaultPipeLength = 2f;
            defaultPipeCrossSectionArea = 0.01f;
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
