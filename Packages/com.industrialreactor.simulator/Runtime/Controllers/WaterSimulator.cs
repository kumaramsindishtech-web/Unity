// ============================================================================
// Industrial Reactor Simulator - Water Simulator
// Copyright (c) 2024 Industrial Reactor Simulator. MIT License.
// ============================================================================

using UnityEngine;

namespace IndustrialReactorSimulator
{
    /// <summary>
    /// Simulates water behavior in the reactor tank.
    /// </summary>
    [AddComponentMenu("Industrial Reactor/Water Simulator")]
    public class WaterSimulator : MonoBehaviour
    {
        [Header("Water Mesh Configuration")]
        [SerializeField] private Transform waterMesh;
        [SerializeField] private WaterScaleMode scaleMode = WaterScaleMode.ScaleY;
        [SerializeField] private float minScale = 0.01f;
        [SerializeField] private float maxScale = 1f;
        [SerializeField] private Vector3 emptyPositionOffset = Vector3.zero;
        [SerializeField] private Vector3 fullPositionOffset = Vector3.zero;

        [Header("Tank Configuration")]
        [SerializeField] private float tankCapacity = 1000f;

        [Header("Water Material")]
        [SerializeField] private Renderer waterRenderer;
        [SerializeField] private bool useMaterialPropertyBlock = true;

        [Header("Shader Properties")]
        [SerializeField] private string waterLevelProperty = "_WaterLevel";
        [SerializeField] private string swirlSpeedProperty = "_SwirlSpeed";
        [SerializeField] private string emissionIntensityProperty = "_EmissionIntensity";
        [SerializeField] private string turbulenceProperty = "_Turbulence";

        [Header("Runtime State")]
        [SerializeField][Range(0f, 1f)] private float currentWaterLevel = 0f;
        [SerializeField] private float currentVolume = 0f;
        [SerializeField] private float currentSwirlSpeed = 0f;

        private MaterialPropertyBlock propertyBlock;
        private Vector3 initialScale;
        private Vector3 initialPosition;

        public float CurrentWaterLevel => currentWaterLevel;
        public float CurrentVolume => currentVolume;
        public float FillPercentage => currentWaterLevel * 100f;
        public bool IsEmpty => currentWaterLevel <= 0.01f;
        public bool IsFull => currentWaterLevel >= 0.99f;

        private void Awake()
        {
            propertyBlock = new MaterialPropertyBlock();
            if (waterMesh != null)
            {
                initialScale = waterMesh.localScale;
                initialPosition = waterMesh.localPosition;
            }
        }

        private void Start() => UpdateWaterVisuals();

        public void UpdateWaterLevel(float inletFlow, float outletFlow, float deltaTime)
        {
            float netFlow = inletFlow - outletFlow;
            currentVolume = Mathf.Clamp(currentVolume + netFlow * deltaTime, 0f, tankCapacity);
            float previousLevel = currentWaterLevel;
            currentWaterLevel = tankCapacity > 0 ? currentVolume / tankCapacity : 0f;
            if (!Mathf.Approximately(previousLevel, currentWaterLevel))
            {
                UpdateWaterVisuals();
                ReactorEvents.RaiseWaterLevelChanged(currentWaterLevel);
            }
        }

        public void SetWaterLevel(float level)
        {
            currentWaterLevel = Mathf.Clamp01(level);
            currentVolume = currentWaterLevel * tankCapacity;
            UpdateWaterVisuals();
            ReactorEvents.RaiseWaterLevelChanged(currentWaterLevel);
        }

        public void SetSwirlSpeed(float speed) { currentSwirlSpeed = speed; UpdateShaderProperties(); }

        public void SetEmissionIntensity(float intensity)
        {
            if (waterRenderer == null) return;
            if (useMaterialPropertyBlock)
            {
                waterRenderer.GetPropertyBlock(propertyBlock);
                propertyBlock.SetFloat(emissionIntensityProperty, intensity);
                waterRenderer.SetPropertyBlock(propertyBlock);
            }
            else waterRenderer.material.SetFloat(emissionIntensityProperty, intensity);
        }

        public void ResetWater()
        {
            currentWaterLevel = 0f; currentVolume = 0f; currentSwirlSpeed = 0f;
            if (waterMesh != null)
            {
                waterMesh.localScale = new Vector3(initialScale.x, minScale, initialScale.z);
                waterMesh.localPosition = initialPosition + emptyPositionOffset;
            }
            UpdateWaterVisuals();
            ReactorEvents.RaiseWaterLevelChanged(currentWaterLevel);
        }

        private void UpdateWaterVisuals() { UpdateMeshScale(); UpdateShaderProperties(); }

        private void UpdateMeshScale()
        {
            if (waterMesh == null) return;
            switch (scaleMode)
            {
                case WaterScaleMode.ScaleY:
                    waterMesh.localScale = new Vector3(initialScale.x, Mathf.Lerp(minScale, maxScale, currentWaterLevel), initialScale.z);
                    break;
                case WaterScaleMode.ScaleXYZ:
                    waterMesh.localScale = Vector3.one * Mathf.Lerp(minScale, maxScale, currentWaterLevel);
                    break;
                case WaterScaleMode.MoveAndScale:
                    float scale = Mathf.Lerp(minScale, maxScale, currentWaterLevel);
                    waterMesh.localScale = new Vector3(initialScale.x, scale, initialScale.z);
                    waterMesh.localPosition = Vector3.Lerp(initialPosition + emptyPositionOffset, initialPosition + fullPositionOffset, currentWaterLevel);
                    break;
            }
        }

        private void UpdateShaderProperties()
        {
            if (waterRenderer == null) return;
            if (useMaterialPropertyBlock)
            {
                waterRenderer.GetPropertyBlock(propertyBlock);
                propertyBlock.SetFloat(waterLevelProperty, currentWaterLevel);
                propertyBlock.SetFloat(swirlSpeedProperty, currentSwirlSpeed);
                propertyBlock.SetFloat(turbulenceProperty, currentSwirlSpeed * 0.1f);
                waterRenderer.SetPropertyBlock(propertyBlock);
            }
            else
            {
                Material mat = waterRenderer.material;
                mat.SetFloat(waterLevelProperty, currentWaterLevel);
                mat.SetFloat(swirlSpeedProperty, currentSwirlSpeed);
                mat.SetFloat(turbulenceProperty, currentSwirlSpeed * 0.1f);
            }
        }
    }
}
