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
        [Range(1f, 100f)] public float defaultInletFlowRate = 20f;
        [Range(1f, 100f)] public float defaultOutletFlowRate = 15f;
        [Range(10f, 200f)] public float maxInletFlowRate = 100f;
        [Range(10f, 200f)] public float maxOutletFlowRate = 80f;

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
        public float CalculateFillTime(float flowRate) => flowRate <= 0f ? float.MaxValue : tankCapacity / flowRate;

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
