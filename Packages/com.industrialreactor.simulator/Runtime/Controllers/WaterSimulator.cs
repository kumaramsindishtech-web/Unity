// ============================================================================
// Industrial Reactor Simulator - Water Simulator
// Copyright (c) 2024 Industrial Reactor Simulator. MIT License.
// ============================================================================

using UnityEngine;
using System;

namespace IndustrialReactorSimulator
{
    /// <summary>
    /// Simulates water level in the reactor tank.
    ///
    /// Default (recommended) mode is SHADER-BASED fill:
    ///   - The water mesh stays completely STATIC (no scaling, no moving).
    ///   - The shader reveals water from BOTTOM to TOP using the mesh's real
    ///     object-space height bounds. This is independent of the mesh pivot
    ///     and independent of UV mapping, so it can never "fill from the middle".
    ///
    /// A legacy mesh-scaling mode is still available for special cases.
    /// </summary>
    [AddComponentMenu("Industrial Reactor/Water Simulator")]
    public class WaterSimulator : MonoBehaviour
    {
        public enum FillMethod
        {
            ShaderClip,   // Recommended: static mesh, shader clips bottom-to-top
            MeshScaling   // Legacy: scale/move the mesh
        }

        [Header("Fill Method")]
        [Tooltip("ShaderClip (recommended) keeps the mesh static and fills bottom-to-top in the shader. MeshScaling scales the mesh (legacy).")]
        [SerializeField] private FillMethod fillMethod = FillMethod.ShaderClip;

        [Header("Water Renderer")]
        [Tooltip("Renderer that uses the 'Industrial Reactor/Water' shader. Auto-found from this object if empty.")]
        [SerializeField] private Renderer waterRenderer;
        [Tooltip("Transform of the water mesh. Auto-found if empty.")]
        [SerializeField] private Transform waterMesh;

        [Header("Tank Configuration")]
        [Tooltip("Total tank capacity in Liters.")]
        [SerializeField] private float tankCapacity = 1000f;
        [Tooltip("Fill axis in the mesh's local space. Y = vertical tank (default).")]
        [SerializeField] private WaterFillAxis fillAxis = WaterFillAxis.Y;

        [Header("Mesh Scaling Mode (legacy only)")]
        [SerializeField] private float minScale = 0.001f;
        [SerializeField] private float maxScale = 1f;

        [Header("Shader Property Names")]
        [SerializeField] private string waterLevelProperty = "_WaterLevel";
        [SerializeField] private string fillMinProperty = "_FillMin";
        [SerializeField] private string fillMaxProperty = "_FillMax";
        [SerializeField] private string fillAxisProperty = "_FillAxis";
        [SerializeField] private string swirlSpeedProperty = "_SwirlSpeed";
        [SerializeField] private string emissionIntensityProperty = "_EmissionIntensity";

        [Header("Runtime State (Read Only)")]
        [SerializeField][Range(0f, 1f)] private float currentWaterLevel = 0f;
        [SerializeField] private float currentVolume = 0f;
        [SerializeField] private float currentSwirlSpeed = 0f;
        [SerializeField] private bool canFill = false;

        // --- internals ---
        private MaterialPropertyBlock propertyBlock;
        private Vector3 initialScale;
        private Vector3 initialPosition;
        private Vector3 boundsMin;     // object-space mesh bounds (min)
        private Vector3 boundsMax;     // object-space mesh bounds (max)
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

        /// <summary>Enable/disable filling (used for sequential pipe→tank flow).</summary>
        public bool CanFill
        {
            get => canFill;
            set => canFill = value;
        }

        private void Awake() => Initialize();
        private void OnEnable() => Initialize();

        private void Initialize()
        {
            if (initialized) return;

            propertyBlock = new MaterialPropertyBlock();

            // Auto-resolve references
            if (waterRenderer == null) waterRenderer = GetComponentInChildren<Renderer>();
            if (waterMesh == null && waterRenderer != null) waterMesh = waterRenderer.transform;
            if (waterMesh == null) waterMesh = transform;

            // Cache initial transform
            initialScale = waterMesh.localScale;
            initialPosition = waterMesh.localPosition;

            // Cache object-space mesh bounds (used by the shader to clip bottom-to-top)
            CacheMeshBounds();

            initialized = true;
        }

        private void CacheMeshBounds()
        {
            boundsMin = new Vector3(-0.5f, -0.5f, -0.5f);
            boundsMax = new Vector3(0.5f, 0.5f, 0.5f);

            var mf = waterMesh != null ? waterMesh.GetComponent<MeshFilter>() : null;
            if (mf != null && mf.sharedMesh != null)
            {
                Bounds b = mf.sharedMesh.bounds; // object/local space
                boundsMin = b.min;
                boundsMax = b.max;
            }
            else if (waterRenderer is SkinnedMeshRenderer smr && smr.sharedMesh != null)
            {
                Bounds b = smr.sharedMesh.bounds;
                boundsMin = b.min;
                boundsMax = b.max;
            }
        }

        private void Start()
        {
            Initialize();
            currentWaterLevel = 0f;
            currentVolume = 0f;
            UpdateVisuals();
        }

        /// <summary>Update water level from inlet/outlet flow rates (L/s).</summary>
        public void UpdateWaterLevel(float inletFlow, float outletFlow, float deltaTime)
        {
            float effectiveInlet = canFill ? inletFlow : 0f;
            float netFlow = effectiveInlet - outletFlow;

            currentVolume = Mathf.Clamp(currentVolume + netFlow * deltaTime, 0f, tankCapacity);

            float previousLevel = currentWaterLevel;
            currentWaterLevel = tankCapacity > 0f ? currentVolume / tankCapacity : 0f;

            if (!Mathf.Approximately(previousLevel, currentWaterLevel))
            {
                UpdateVisuals();
                ReactorEvents.RaiseWaterLevelChanged(currentWaterLevel);
                OnWaterLevelChanged?.Invoke(currentWaterLevel);

                if (previousLevel < 0.99f && currentWaterLevel >= 0.99f) OnTankFull?.Invoke();
                else if (previousLevel > 0.01f && currentWaterLevel <= 0.01f) OnTankEmpty?.Invoke();
            }
        }

        /// <summary>Set the water level directly (0..1).</summary>
        public void SetWaterLevel(float level)
        {
            float previousLevel = currentWaterLevel;
            currentWaterLevel = Mathf.Clamp01(level);
            currentVolume = currentWaterLevel * tankCapacity;
            UpdateVisuals();
            ReactorEvents.RaiseWaterLevelChanged(currentWaterLevel);
            OnWaterLevelChanged?.Invoke(currentWaterLevel);

            if (previousLevel < 0.99f && currentWaterLevel >= 0.99f) OnTankFull?.Invoke();
            else if (previousLevel > 0.01f && currentWaterLevel <= 0.01f) OnTankEmpty?.Invoke();
        }

        /// <summary>Set swirl speed (driven by agitator RPM).</summary>
        public void SetSwirlSpeed(float speed)
        {
            currentSwirlSpeed = speed;
            ApplyShaderProperties();
        }

        /// <summary>Set emission intensity for a glow effect.</summary>
        public void SetEmissionIntensity(float intensity)
        {
            if (waterRenderer == null) return;
            if (propertyBlock == null) propertyBlock = new MaterialPropertyBlock();
            waterRenderer.GetPropertyBlock(propertyBlock);
            propertyBlock.SetFloat(emissionIntensityProperty, intensity);
            waterRenderer.SetPropertyBlock(propertyBlock);
        }

        public void SetFillAxis(WaterFillAxis axis)
        {
            fillAxis = axis;
            UpdateVisuals();
        }

        /// <summary>Reset to empty.</summary>
        public void ResetWater()
        {
            currentWaterLevel = 0f;
            currentVolume = 0f;
            currentSwirlSpeed = 0f;
            canFill = false;
            UpdateVisuals();
            ReactorEvents.RaiseWaterLevelChanged(currentWaterLevel);
            OnWaterLevelChanged?.Invoke(currentWaterLevel);
        }

        // ====================================================================
        // Visuals
        // ====================================================================
        private void UpdateVisuals()
        {
            if (fillMethod == FillMethod.MeshScaling)
                UpdateMeshScaling();
            else
                RestoreMeshTransform(); // ensure mesh is static in shader mode

            ApplyShaderProperties();
        }

        private void RestoreMeshTransform()
        {
            if (waterMesh == null) return;
            // Keep the mesh exactly where the artist placed it.
            waterMesh.localScale = initialScale;
            waterMesh.localPosition = initialPosition;
        }

        private void UpdateMeshScaling()
        {
            if (waterMesh == null) return;

            float scaleFactor = Mathf.Lerp(minScale, maxScale, currentWaterLevel);

            // Scale only on the fill axis
            Vector3 s = initialScale;
            if (fillAxis == WaterFillAxis.Y) s.y = scaleFactor;
            else s.z = scaleFactor;
            waterMesh.localScale = s;

            // Shift so the mesh grows upward from the bottom (anchored at bottom).
            float fullSizeAlongAxis = (fillAxis == WaterFillAxis.Y)
                ? (boundsMax.y - boundsMin.y) * initialScale.y
                : (boundsMax.z - boundsMin.z) * initialScale.z;

            // Bottom should stay fixed; center moves up by half of the missing height.
            float missing = fullSizeAlongAxis * (1f - scaleFactor / Mathf.Max(maxScale, 0.0001f));
            Vector3 p = initialPosition;
            if (fillAxis == WaterFillAxis.Y) p.y = initialPosition.y - missing * 0.5f;
            else p.z = initialPosition.z - missing * 0.5f;
            waterMesh.localPosition = p;
        }

        private void ApplyShaderProperties()
        {
            if (waterRenderer == null) return;
            if (propertyBlock == null) propertyBlock = new MaterialPropertyBlock();

            // Min/Max along the chosen fill axis (object space)
            float fillMin = (fillAxis == WaterFillAxis.Y) ? boundsMin.y : boundsMin.z;
            float fillMax = (fillAxis == WaterFillAxis.Y) ? boundsMax.y : boundsMax.z;

            waterRenderer.GetPropertyBlock(propertyBlock);
            propertyBlock.SetFloat(waterLevelProperty, currentWaterLevel);
            propertyBlock.SetFloat(fillMinProperty, fillMin);
            propertyBlock.SetFloat(fillMaxProperty, fillMax);
            propertyBlock.SetFloat(fillAxisProperty, fillAxis == WaterFillAxis.Y ? 0f : 1f);
            propertyBlock.SetFloat(swirlSpeedProperty, currentSwirlSpeed);
            waterRenderer.SetPropertyBlock(propertyBlock);
        }

#if UNITY_EDITOR
        [ContextMenu("Test Fill 50%")]
        private void TestFill50() { Initialize(); SetWaterLevel(0.5f); }

        [ContextMenu("Test Fill 100%")]
        private void TestFill100() { Initialize(); SetWaterLevel(1f); }

        [ContextMenu("Test Empty")]
        private void TestEmpty() { Initialize(); SetWaterLevel(0f); }

        [ContextMenu("Log Mesh Bounds")]
        private void LogBounds()
        {
            Initialize();
            Debug.Log($"[WaterSimulator] Object-space bounds  min:{boundsMin}  max:{boundsMax}  (fill axis: {fillAxis})");
        }

        private void OnValidate()
        {
            if (!Application.isPlaying) return;
            initialized = false;
            Initialize();
            UpdateVisuals();
        }
#endif
    }

    /// <summary>Water fill axis direction.</summary>
    public enum WaterFillAxis
    {
        Y,  // Vertical fill (default)
        Z   // Horizontal fill (for horizontal tanks)
    }
}
