// ============================================================================
// Industrial Reactor Simulator - Water Simulator
// Copyright (c) 2024 Industrial Reactor Simulator. MIT License.
// ============================================================================

using UnityEngine;
using System;

namespace IndustrialReactorSimulator
{
    /// <summary>
    /// Simulates water behavior in the reactor tank.
    /// Supports Z-axis fill mode for horizontal tanks.
    /// </summary>
    [AddComponentMenu("Industrial Reactor/Water Simulator")]
    public class WaterSimulator : MonoBehaviour
    {
        [Header("Water Mesh Configuration")]
        [SerializeField] private Transform waterMesh;
        [SerializeField] private WaterScaleMode scaleMode = WaterScaleMode.ScaleY;
        [Tooltip("Fill axis: Y for vertical tanks, Z for horizontal tanks")]
        [SerializeField] private WaterFillAxis fillAxis = WaterFillAxis.Y;
        [Tooltip("Fill direction: Normal = bottom to top, Inverted = top to bottom")]
        [SerializeField] private WaterFillDirection fillDirection = WaterFillDirection.Normal;
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

        [Header("Runtime State")]
        [SerializeField][Range(0f, 1f)] private float currentWaterLevel = 0f;
        [SerializeField] private float currentVolume = 0f;
        [SerializeField] private float currentSwirlSpeed = 0f;
        [SerializeField] private bool canFill = false;

        private MaterialPropertyBlock propertyBlock;
        private Vector3 initialScale;
        private Vector3 initialPosition;

        // Events
        public event Action<float> OnWaterLevelChanged;
        public event Action OnTankFull;
        public event Action OnTankEmpty;

        // Properties
        public float CurrentWaterLevel => currentWaterLevel;
        public float CurrentVolume => currentVolume;
        public float FillPercentage => currentWaterLevel * 100f;
        public bool IsEmpty => currentWaterLevel <= 0.01f;
        public bool IsFull => currentWaterLevel >= 0.99f;
        public WaterFillAxis FillAxis => fillAxis;
        public WaterFillDirection FillDirection => fillDirection;

        /// <summary>
        /// Enable or disable tank filling (used for sequential flow)
        /// </summary>
        public bool CanFill
        {
            get => canFill;
            set => canFill = value;
        }

        private void Awake()
        {
            propertyBlock = new MaterialPropertyBlock();
            if (waterMesh != null)
            {
                initialScale = waterMesh.localScale;
                initialPosition = waterMesh.localPosition;
            }
        }

        private void Start()
        {
            UpdateWaterVisuals();
        }

        /// <summary>
        /// Update water level based on inlet and outlet flow
        /// </summary>
        public void UpdateWaterLevel(float inletFlow, float outletFlow, float deltaTime)
        {
            // Only allow filling if canFill is true (inlet pipe is filled)
            float effectiveInletFlow = canFill ? inletFlow : 0f;
            
            float netFlow = effectiveInletFlow - outletFlow;
            float previousVolume = currentVolume;
            currentVolume = Mathf.Clamp(currentVolume + netFlow * deltaTime, 0f, tankCapacity);
            
            float previousLevel = currentWaterLevel;
            currentWaterLevel = tankCapacity > 0 ? currentVolume / tankCapacity : 0f;
            
            if (!Mathf.Approximately(previousLevel, currentWaterLevel))
            {
                UpdateWaterVisuals();
                ReactorEvents.RaiseWaterLevelChanged(currentWaterLevel);
                OnWaterLevelChanged?.Invoke(currentWaterLevel);
                
                // Check for full/empty states
                if (previousLevel < 0.99f && currentWaterLevel >= 0.99f)
                    OnTankFull?.Invoke();
                else if (previousLevel > 0.01f && currentWaterLevel <= 0.01f)
                    OnTankEmpty?.Invoke();
            }
        }

        /// <summary>
        /// Set water level directly (0-1)
        /// </summary>
        public void SetWaterLevel(float level)
        {
            float previousLevel = currentWaterLevel;
            currentWaterLevel = Mathf.Clamp01(level);
            currentVolume = currentWaterLevel * tankCapacity;
            UpdateWaterVisuals();
            ReactorEvents.RaiseWaterLevelChanged(currentWaterLevel);
            OnWaterLevelChanged?.Invoke(currentWaterLevel);
            
            if (previousLevel < 0.99f && currentWaterLevel >= 0.99f)
                OnTankFull?.Invoke();
            else if (previousLevel > 0.01f && currentWaterLevel <= 0.01f)
                OnTankEmpty?.Invoke();
        }

        /// <summary>
        /// Set swirl speed for agitator effect
        /// </summary>
        public void SetSwirlSpeed(float speed)
        {
            currentSwirlSpeed = speed;
            UpdateShaderProperties();
        }

        /// <summary>
        /// Set emission intensity for glow effect
        /// </summary>
        public void SetEmissionIntensity(float intensity)
        {
            if (waterRenderer == null) return;
            
            if (useMaterialPropertyBlock)
            {
                waterRenderer.GetPropertyBlock(propertyBlock);
                propertyBlock.SetFloat(emissionIntensityProperty, intensity);
                waterRenderer.SetPropertyBlock(propertyBlock);
            }
            else
            {
                waterRenderer.material.SetFloat(emissionIntensityProperty, intensity);
            }
        }

        /// <summary>
        /// Set fill axis at runtime
        /// </summary>
        public void SetFillAxis(WaterFillAxis axis)
        {
            fillAxis = axis;
            UpdateWaterVisuals();
        }

        /// <summary>
        /// Set fill direction at runtime
        /// </summary>
        public void SetFillDirection(WaterFillDirection direction)
        {
            fillDirection = direction;
            UpdateWaterVisuals();
        }

        /// <summary>
        /// Reset water to empty state
        /// </summary>
        public void ResetWater()
        {
            currentWaterLevel = 0f;
            currentVolume = 0f;
            currentSwirlSpeed = 0f;
            canFill = false;
            
            if (waterMesh != null)
            {
                // Reset based on fill axis
                if (fillAxis == WaterFillAxis.Z)
                    waterMesh.localScale = new Vector3(initialScale.x, initialScale.y, minScale);
                else
                    waterMesh.localScale = new Vector3(initialScale.x, minScale, initialScale.z);
                    
                waterMesh.localPosition = initialPosition + emptyPositionOffset;
            }
            
            UpdateWaterVisuals();
            ReactorEvents.RaiseWaterLevelChanged(currentWaterLevel);
            OnWaterLevelChanged?.Invoke(currentWaterLevel);
        }

        private void UpdateWaterVisuals()
        {
            UpdateMeshScale();
            UpdateShaderProperties();
        }

        private void UpdateMeshScale()
        {
            if (waterMesh == null) return;

            switch (scaleMode)
            {
                case WaterScaleMode.ScaleY:
                    // Scale on Y axis (vertical fill)
                    if (fillAxis == WaterFillAxis.Y)
                    {
                        waterMesh.localScale = new Vector3(
                            initialScale.x,
                            Mathf.Lerp(minScale, maxScale, currentWaterLevel),
                            initialScale.z
                        );
                    }
                    // Scale on Z axis (horizontal fill)
                    else
                    {
                        waterMesh.localScale = new Vector3(
                            initialScale.x,
                            initialScale.y,
                            Mathf.Lerp(minScale, maxScale, currentWaterLevel)
                        );
                    }
                    break;

                case WaterScaleMode.ScaleXYZ:
                    waterMesh.localScale = Vector3.one * Mathf.Lerp(minScale, maxScale, currentWaterLevel);
                    break;

                case WaterScaleMode.MoveAndScale:
                    float scale = Mathf.Lerp(minScale, maxScale, currentWaterLevel);
                    
                    if (fillAxis == WaterFillAxis.Y)
                    {
                        waterMesh.localScale = new Vector3(initialScale.x, scale, initialScale.z);
                    }
                    else
                    {
                        waterMesh.localScale = new Vector3(initialScale.x, initialScale.y, scale);
                    }
                    
                    // Adjust position based on fill direction
                    Vector3 startPos = fillDirection == WaterFillDirection.Normal 
                        ? initialPosition + emptyPositionOffset 
                        : initialPosition + fullPositionOffset;
                    Vector3 endPos = fillDirection == WaterFillDirection.Normal 
                        ? initialPosition + fullPositionOffset 
                        : initialPosition + emptyPositionOffset;
                    
                    waterMesh.localPosition = Vector3.Lerp(startPos, endPos, currentWaterLevel);
                    break;

                case WaterScaleMode.ShaderOnly:
                    // Don't modify mesh, shader handles visualization
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
                waterRenderer.SetPropertyBlock(propertyBlock);
            }
            else
            {
                Material mat = waterRenderer.material;
                mat.SetFloat(waterLevelProperty, currentWaterLevel);
                mat.SetFloat(swirlSpeedProperty, currentSwirlSpeed);
            }
        }

#if UNITY_EDITOR
        [ContextMenu("Test Fill to 50%")]
        private void TestFill50()
        {
            SetWaterLevel(0.5f);
        }

        [ContextMenu("Test Fill to 100%")]
        private void TestFill100()
        {
            SetWaterLevel(1f);
        }

        [ContextMenu("Test Empty")]
        private void TestEmpty()
        {
            SetWaterLevel(0f);
        }

        [ContextMenu("Toggle Fill Axis")]
        private void ToggleFillAxis()
        {
            fillAxis = fillAxis == WaterFillAxis.Y ? WaterFillAxis.Z : WaterFillAxis.Y;
            UpdateWaterVisuals();
        }
#endif
    }

    /// <summary>
    /// Water fill axis direction
    /// </summary>
    public enum WaterFillAxis
    {
        Y,  // Vertical fill (default)
        Z   // Horizontal fill (for horizontal tanks)
    }

    /// <summary>
    /// Water fill direction
    /// </summary>
    public enum WaterFillDirection
    {
        Normal,   // Bottom to top (Y) or back to front (Z)
        Inverted  // Top to bottom (Y) or front to back (Z)
    }
}
