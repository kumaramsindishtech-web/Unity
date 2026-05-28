// ============================================================================
// Industrial Reactor Simulator - Water Simulator
// Version: 1.0.0
// Description: Simulates water level, flow, and visual effects in the tank
// ============================================================================

using UnityEngine;
using IndustrialReactorSimulator.Core;

namespace IndustrialReactorSimulator.Controllers
{
    /// <summary>
    /// Simulates water behavior in the reactor tank.
    /// Handles water level changes, visual scaling, and shader properties.
    /// </summary>
    [AddComponentMenu("Industrial Reactor/Water Simulator")]
    public class WaterSimulator : MonoBehaviour
    {
        // ====================================================================
        // CONFIGURATION
        // ====================================================================
        
        [Header("Water Mesh Configuration")]
        [Tooltip("Water mesh transform to scale")]
        [SerializeField] private Transform waterMesh;
        
        [Tooltip("Scale mode for water level visualization")]
        [SerializeField] private WaterScaleMode scaleMode = WaterScaleMode.ScaleY;
        
        [Tooltip("Minimum Y scale when empty")]
        [SerializeField] private float minScale = 0.01f;
        
        [Tooltip("Maximum Y scale when full")]
        [SerializeField] private float maxScale = 1f;
        
        [Tooltip("Base position offset when empty")]
        [SerializeField] private Vector3 emptyPositionOffset = Vector3.zero;
        
        [Tooltip("Position offset when full")]
        [SerializeField] private Vector3 fullPositionOffset = Vector3.zero;

        [Header("Tank Configuration")]
        [Tooltip("Tank capacity in liters")]
        [SerializeField] private float tankCapacity = 1000f;


        [Header("Water Material")]
        [Tooltip("Renderer for water material")]
        [SerializeField] private Renderer waterRenderer;
        
        [Tooltip("Use MaterialPropertyBlock for efficiency")]
        [SerializeField] private bool useMaterialPropertyBlock = true;

        [Header("Shader Properties")]
        [SerializeField] private string waterLevelProperty = "_WaterLevel";
        [SerializeField] private string swirlSpeedProperty = "_SwirlSpeed";
        [SerializeField] private string emissionIntensityProperty = "_EmissionIntensity";
        [SerializeField] private string turbulenceProperty = "_Turbulence";

        // ====================================================================
        // RUNTIME STATE
        // ====================================================================
        
        [Header("Runtime State (Read-Only)")]
        [SerializeField] [Range(0f, 1f)] private float currentWaterLevel = 0f;
        [SerializeField] private float currentVolume = 0f;
        [SerializeField] private float currentSwirlSpeed = 0f;
        
        private MaterialPropertyBlock propertyBlock;
        private Vector3 initialScale;
        private Vector3 initialPosition;

        // ====================================================================
        // PROPERTIES
        // ====================================================================
        
        public float CurrentWaterLevel => currentWaterLevel;
        public float CurrentVolume => currentVolume;
        public float FillPercentage => currentWaterLevel * 100f;
        public bool IsEmpty => currentWaterLevel <= 0.01f;
        public bool IsFull => currentWaterLevel >= 0.99f;

        // ====================================================================
        // ENUMS
        // ====================================================================
        
        public enum WaterScaleMode
        {
            ScaleY,         // Scale Y axis only
            ScaleXYZ,       // Uniform scale
            MoveAndScale,   // Move position and scale
            ShaderOnly      // Only update shader, no mesh changes
        }



        // ====================================================================
        // UNITY LIFECYCLE
        // ====================================================================
        
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

        // ====================================================================
        // PUBLIC METHODS
        // ====================================================================
        
        /// <summary>
        /// Update water level based on inlet and outlet flow
        /// </summary>
        public void UpdateWaterLevel(float inletFlow, float outletFlow, float deltaTime)
        {
            // Calculate net flow (liters per second)
            float netFlow = inletFlow - outletFlow;
            
            // Calculate volume change
            float volumeChange = netFlow * deltaTime;
            
            // Update volume
            currentVolume = Mathf.Clamp(currentVolume + volumeChange, 0f, tankCapacity);
            
            // Calculate normalized level
            float previousLevel = currentWaterLevel;
            currentWaterLevel = tankCapacity > 0 ? currentVolume / tankCapacity : 0f;
            
            // Only update if level changed significantly
            if (!Mathf.Approximately(previousLevel, currentWaterLevel))
            {
                UpdateWaterVisuals();
                ReactorEvents.RaiseWaterLevelChanged(currentWaterLevel);
            }
        }

        /// <summary>
        /// Set water level directly (0-1)
        /// </summary>
        public void SetWaterLevel(float level)
        {
            currentWaterLevel = Mathf.Clamp01(level);
            currentVolume = currentWaterLevel * tankCapacity;
            
            UpdateWaterVisuals();
            ReactorEvents.RaiseWaterLevelChanged(currentWaterLevel);
        }

        /// <summary>
        /// Set swirl speed for water animation
        /// </summary>
        public void SetSwirlSpeed(float speed)
        {
            currentSwirlSpeed = speed;
            UpdateShaderProperties();
        }



        /// <summary>
        /// Set emission intensity for water glow effect
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
        /// Reset water to initial state
        /// </summary>
        public void ResetWater()
        {
            currentWaterLevel = 0f;
            currentVolume = 0f;
            currentSwirlSpeed = 0f;
            
            if (waterMesh != null)
            {
                waterMesh.localScale = new Vector3(initialScale.x, minScale, initialScale.z);
                waterMesh.localPosition = initialPosition + emptyPositionOffset;
            }
            
            UpdateWaterVisuals();
            ReactorEvents.RaiseWaterLevelChanged(currentWaterLevel);
        }

        // ====================================================================
        // VISUAL UPDATES
        // ====================================================================
        
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
                    float yScale = Mathf.Lerp(minScale, maxScale, currentWaterLevel);
                    waterMesh.localScale = new Vector3(initialScale.x, yScale, initialScale.z);
                    break;
                    
                case WaterScaleMode.ScaleXYZ:
                    float uniformScale = Mathf.Lerp(minScale, maxScale, currentWaterLevel);
                    waterMesh.localScale = Vector3.one * uniformScale;
                    break;
                    
                case WaterScaleMode.MoveAndScale:
                    float scale = Mathf.Lerp(minScale, maxScale, currentWaterLevel);
                    waterMesh.localScale = new Vector3(initialScale.x, scale, initialScale.z);
                    waterMesh.localPosition = Vector3.Lerp(
                        initialPosition + emptyPositionOffset, 
                        initialPosition + fullPositionOffset, 
                        currentWaterLevel
                    );
                    break;
                    
                case WaterScaleMode.ShaderOnly:
                    // No mesh changes, handled by shader
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

        // ====================================================================
        // EDITOR HELPERS
        // ====================================================================
        
        #if UNITY_EDITOR
        [ContextMenu("Set Empty")]
        private void EditorSetEmpty()
        {
            SetWaterLevel(0f);
        }

        [ContextMenu("Set Half Full")]
        private void EditorSetHalf()
        {
            SetWaterLevel(0.5f);
        }

        [ContextMenu("Set Full")]
        private void EditorSetFull()
        {
            SetWaterLevel(1f);
        }

        private void OnDrawGizmosSelected()
        {
            if (waterMesh == null) return;
            
            // Draw water level indicator
            Gizmos.color = new Color(0f, 0.5f, 1f, 0.5f);
            
            Vector3 center = waterMesh.position;
            Vector3 size = waterMesh.localScale;
            
            Gizmos.DrawWireCube(center, size);
        }
        #endif
    }
}
