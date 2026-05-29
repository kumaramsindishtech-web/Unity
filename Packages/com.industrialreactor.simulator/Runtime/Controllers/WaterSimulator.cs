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
    /// Water fills from BOTTOM to TOP by default.
    /// </summary>
    [AddComponentMenu("Industrial Reactor/Water Simulator")]
    public class WaterSimulator : MonoBehaviour
    {
        [Header("Water Mesh Configuration")]
        [SerializeField] private Transform waterMesh;
        [SerializeField] private WaterScaleMode scaleMode = WaterScaleMode.MoveAndScale;
        [Tooltip("Fill axis: Y for vertical tanks, Z for horizontal tanks")]
        [SerializeField] private WaterFillAxis fillAxis = WaterFillAxis.Y;
        [SerializeField] private float minScale = 0.01f;
        [SerializeField] private float maxScale = 1f;
        
        [Header("Position Offsets (for MoveAndScale mode)")]
        [Tooltip("Position offset when tank is empty (usually bottom)")]
        [SerializeField] private Vector3 emptyPositionOffset = new Vector3(0, -0.5f, 0);
        [Tooltip("Position offset when tank is full (usually center or top)")]
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
        private bool initialized = false;

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
            Initialize();
        }
        
        private void Initialize()
        {
            if (initialized) return;
            
            propertyBlock = new MaterialPropertyBlock();
            if (waterMesh != null)
            {
                initialScale = waterMesh.localScale;
                initialPosition = waterMesh.localPosition;
            }
            initialized = true;
        }

        private void Start()
        {
            Initialize();
            // Start with water at level 0 (empty)
            currentWaterLevel = 0f;
            currentVolume = 0f;
            UpdateWaterVisuals();
        }
        
        private void OnEnable()
        {
            Initialize();
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
                if (propertyBlock == null) propertyBlock = new MaterialPropertyBlock();
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
        /// Reset water to empty state
        /// </summary>
        public void ResetWater()
        {
            currentWaterLevel = 0f;
            currentVolume = 0f;
            currentSwirlSpeed = 0f;
            canFill = false;
            
            UpdateWaterVisuals();
            ReactorEvents.RaiseWaterLevelChanged(currentWaterLevel);
            OnWaterLevelChanged?.Invoke(currentWaterLevel);
        }

        private void UpdateWaterVisuals()
        {
            UpdateMeshTransform();
            UpdateShaderProperties();
        }

        private void UpdateMeshTransform()
        {
            if (waterMesh == null) return;

            // Calculate scale based on water level
            float scaleFactor = Mathf.Lerp(minScale, maxScale, currentWaterLevel);

            switch (scaleMode)
            {
                case WaterScaleMode.ScaleY:
                    // Scale on fill axis only
                    if (fillAxis == WaterFillAxis.Y)
                    {
                        waterMesh.localScale = new Vector3(initialScale.x, scaleFactor, initialScale.z);
                    }
                    else // Z axis
                    {
                        waterMesh.localScale = new Vector3(initialScale.x, initialScale.y, scaleFactor);
                    }
                    break;

                case WaterScaleMode.ScaleXYZ:
                    waterMesh.localScale = Vector3.one * scaleFactor;
                    break;

                case WaterScaleMode.MoveAndScale:
                    // Scale the water mesh
                    if (fillAxis == WaterFillAxis.Y)
                    {
                        waterMesh.localScale = new Vector3(initialScale.x, scaleFactor, initialScale.z);
                    }
                    else
                    {
                        waterMesh.localScale = new Vector3(initialScale.x, initialScale.y, scaleFactor);
                    }
                    
                    // Move from empty position (bottom) to full position (center/top)
                    // As water level increases, position moves from emptyOffset toward fullOffset
                    waterMesh.localPosition = Vector3.Lerp(
                        initialPosition + emptyPositionOffset,
                        initialPosition + fullPositionOffset,
                        currentWaterLevel
                    );
                    break;

                case WaterScaleMode.ShaderOnly:
                    // Don't modify mesh, shader handles visualization
                    break;
            }
        }

        private void UpdateShaderProperties()
        {
            if (waterRenderer == null) return;
            if (propertyBlock == null) propertyBlock = new MaterialPropertyBlock();

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
            Initialize();
            SetWaterLevel(0.5f);
        }

        [ContextMenu("Test Fill to 100%")]
        private void TestFill100()
        {
            Initialize();
            SetWaterLevel(1f);
        }

        [ContextMenu("Test Empty")]
        private void TestEmpty()
        {
            Initialize();
            SetWaterLevel(0f);
        }

        [ContextMenu("Toggle Fill Axis")]
        private void ToggleFillAxis()
        {
            fillAxis = fillAxis == WaterFillAxis.Y ? WaterFillAxis.Z : WaterFillAxis.Y;
            UpdateWaterVisuals();
        }
        
        [ContextMenu("Capture Current as Empty Position")]
        private void CaptureEmptyPosition()
        {
            if (waterMesh != null)
            {
                emptyPositionOffset = waterMesh.localPosition - initialPosition;
                Debug.Log($"[WaterSimulator] Empty position offset set to: {emptyPositionOffset}");
            }
        }
        
        [ContextMenu("Capture Current as Full Position")]
        private void CaptureFullPosition()
        {
            if (waterMesh != null)
            {
                fullPositionOffset = waterMesh.localPosition - initialPosition;
                Debug.Log($"[WaterSimulator] Full position offset set to: {fullPositionOffset}");
            }
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
}
