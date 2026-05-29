// ============================================================================
// Industrial Reactor Simulator - Pipe Flow Visualizer
// Copyright (c) 2024 Industrial Reactor Simulator. MIT License.
// ============================================================================

using UnityEngine;
using System;
using System.Collections;

namespace IndustrialReactorSimulator
{
    /// <summary>
    /// Visualizes fluid flow through pipes with fill progress and UV-based flow animation.
    /// Supports glass pipe mode: shows transparent glass pipe with water inside when flowing.
    /// </summary>
    [AddComponentMenu("Industrial Reactor/Pipe Flow Visualizer")]
    public class PipeFlowVisualizer : MonoBehaviour
    {
        [Header("Pipe Configuration")]
        [SerializeField] private ValveType associatedValveType = ValveType.Inlet;
        [SerializeField] private Renderer pipeRenderer;
        [SerializeField] private int materialIndex = 0;

        [Header("Material Mode")]
        [Tooltip("Metal material shown in editor/when not flowing")]
        [SerializeField] private Material metalMaterial;
        [Tooltip("Glass pipe material with water flow effect")]
        [SerializeField] private Material glassPipeMaterial;
        [Tooltip("Use glass pipe effect when flowing")]
        [SerializeField] private bool useGlassPipeEffect = true;

        [Header("Pipe Dimensions")]
        [Tooltip("Length of the pipe in meters (for flow time calculation)")]
        [SerializeField] private float pipeLength = 2f;
        [Tooltip("Cross-sectional area of pipe in square meters")]
        [SerializeField] private float pipeCrossSectionArea = 0.01f;

        [Header("Flow Rate Settings (L/s)")]
        [Tooltip("Flow rate in Liters per second")]
        [Range(0.1f, 100f)]
        [SerializeField] private float flowRateLitersPerSecond = 10f;
        
        [Header("Shader Properties")]
        [SerializeField] private string fillProgressProperty = "_FillProgress";
        [SerializeField] private string flowSpeedProperty = "_FlowSpeed";
        [SerializeField] private string flowIntensityProperty = "_FlowIntensity";
        [SerializeField] private string invertFillProperty = "_InvertFill";
        [SerializeField] private string fillMinProperty = "_FillMin";
        [SerializeField] private string fillMaxProperty = "_FillMax";
        [SerializeField] private string fillAxisProperty = "_FillAxis";

        [Header("Fill Direction")]
        [Tooltip("Local axis along which the pipe fills (its long axis). Y is typical for cylinder meshes.")]
        [SerializeField] private PipeFillAxis fillAxisLocal = PipeFillAxis.Y;
        [Tooltip("Enable if water fills from the wrong end of the pipe.")]
        [SerializeField] private bool invertFillDirection = false;

        [Header("Visual Settings")]
        [Tooltip("UV scroll speed multiplier for flow animation")]
        [Range(0.1f, 10f)]
        [SerializeField] private float flowAnimationSpeed = 2f;

        [Header("Optional Particle Flow")]
        [SerializeField] private ParticleSystem flowParticles;
        [SerializeField] private float particleEmissionRate = 50f;

        [Header("Transition Settings")]
        [Range(0.1f, 2f)]
        [SerializeField] private float fadeTime = 0.3f;

        [Header("Runtime State")]
        [SerializeField] private bool isFlowing = false;
        [SerializeField] private FlowDirection flowDirection = FlowDirection.None;
        [SerializeField][Range(0f, 1f)] private float flowIntensity = 0f;
        [SerializeField][Range(0f, 1f)] private float fillProgress = 0f;
        [SerializeField] private PipeFlowState flowState = PipeFlowState.Empty;

        private MaterialPropertyBlock propertyBlock;
        private Coroutine fillCoroutine;
        private Coroutine fadeCoroutine;
        private Material originalMaterial;
        private bool isGlassModeActive = false;
        private bool initialized = false;
        private Vector3 boundsMin = new Vector3(-0.5f, -0.5f, -0.5f);
        private Vector3 boundsMax = new Vector3(0.5f, 0.5f, 0.5f);

        // Events for sequential flow coordination
        public event Action OnPipeFilled;
        public event Action OnPipeEmptied;
        public event Action<float> OnFillProgressChanged;

        // Properties
        public ValveType AssociatedValveType => associatedValveType;
        public bool IsFlowing => isFlowing;
        public FlowDirection CurrentDirection => flowDirection;
        public float FlowIntensity => flowIntensity;
        public float FillProgress => fillProgress;
        public PipeFlowState FlowState => flowState;
        public bool IsFilled => fillProgress >= 0.99f;
        public bool IsEmpty => fillProgress <= 0.01f;
        public bool UseGlassPipeEffect => useGlassPipeEffect;
        public bool IsGlassModeActive => isGlassModeActive;

        /// <summary>
        /// Flow rate in Liters per second
        /// </summary>
        public float FlowRateLitersPerSecond
        {
            get => flowRateLitersPerSecond;
            set => flowRateLitersPerSecond = Mathf.Clamp(value, 0.1f, 100f);
        }

        /// <summary>
        /// Pipe length in meters
        /// </summary>
        public float PipeLength
        {
            get => pipeLength;
            set => pipeLength = Mathf.Max(0.1f, value);
        }

        /// <summary>
        /// Calculate time to fill pipe based on flow rate
        /// </summary>
        public float CalculateFillTime()
        {
            float volumeCubicMeters = pipeLength * pipeCrossSectionArea;
            float volumeLiters = volumeCubicMeters * 1000f;
            return volumeLiters / flowRateLitersPerSecond;
        }

        private void Awake()
        {
            Initialize();
        }
        
        private void Initialize()
        {
            if (initialized) return;
            
            propertyBlock = new MaterialPropertyBlock();
            
            // Store original material
            if (pipeRenderer != null && pipeRenderer.sharedMaterials.Length > materialIndex)
            {
                originalMaterial = pipeRenderer.sharedMaterials[materialIndex];
            }

            CacheMeshBounds();
            
            initialized = true;
        }

        /// <summary>Cache the pipe mesh's object-space bounds for shader-based fill.</summary>
        private void CacheMeshBounds()
        {
            boundsMin = new Vector3(-0.5f, -0.5f, -0.5f);
            boundsMax = new Vector3(0.5f, 0.5f, 0.5f);

            if (pipeRenderer == null) return;

            var mf = pipeRenderer.GetComponent<MeshFilter>();
            if (mf != null && mf.sharedMesh != null)
            {
                Bounds b = mf.sharedMesh.bounds;
                boundsMin = b.min;
                boundsMax = b.max;
            }
            else if (pipeRenderer is SkinnedMeshRenderer smr && smr.sharedMesh != null)
            {
                Bounds b = smr.sharedMesh.bounds;
                boundsMin = b.min;
                boundsMax = b.max;
            }
        }

        private void Start()
        {
            Initialize();
            
            if (!Application.isPlaying) return;
            
            // Start in metal mode with empty pipe
            SetMetalMode();
            fillProgress = 0f;
            flowIntensity = 0f;
            isGlassModeActive = false;
            UpdateMaterial();
        }

        private void OnEnable()
        {
            Initialize();
            
            // In editor, ensure metal material is shown
            if (!Application.isPlaying && pipeRenderer != null && metalMaterial != null)
            {
                SetPipeMaterial(metalMaterial);
            }
        }

        private void OnDestroy()
        {
            // Restore original material
            if (pipeRenderer != null && originalMaterial != null)
            {
                SetPipeMaterial(originalMaterial);
            }
        }

        private void Update()
        {
            if (!Application.isPlaying) return;
            
            // Update continuous flow animation when flowing and filled
            if (isFlowing && flowIntensity > 0f && fillProgress > 0f)
            {
                UpdateMaterial();
            }
        }

        /// <summary>
        /// Start filling the pipe with water flow animation.
        /// </summary>
        public void StartFlow(FlowDirection direction)
        {
            if (isFlowing && flowDirection == direction && flowState == PipeFlowState.Filled) return;

            flowDirection = direction;
            isFlowing = true;

            // Switch to glass pipe material when flowing
            if (useGlassPipeEffect)
            {
                SetGlassMode();
            }

            // Stop any existing coroutines
            if (fillCoroutine != null) StopCoroutine(fillCoroutine);
            if (fadeCoroutine != null) StopCoroutine(fadeCoroutine);

            // Start fill animation
            fillCoroutine = StartCoroutine(FillPipe());

            // Start particles
            if (flowParticles != null && !flowParticles.isPlaying)
                flowParticles.Play();
        }

        /// <summary>
        /// Stop flow and drain the pipe
        /// </summary>
        public void StopFlow()
        {
            if (!isFlowing && flowState == PipeFlowState.Empty) return;

            if (fillCoroutine != null) StopCoroutine(fillCoroutine);
            if (fadeCoroutine != null) StopCoroutine(fadeCoroutine);

            fillCoroutine = StartCoroutine(DrainPipe());
        }

        /// <summary>
        /// Set fill progress directly (0-1)
        /// </summary>
        public void SetFillProgress(float progress)
        {
            fillProgress = Mathf.Clamp01(progress);
            UpdateFlowState();
            UpdateMaterial();
            OnFillProgressChanged?.Invoke(fillProgress);
        }

        /// <summary>
        /// Set flow intensity (0-1)
        /// </summary>
        public void SetFlowIntensity(float intensity)
        {
            flowIntensity = Mathf.Clamp01(intensity);
            UpdateMaterial();

            if (flowParticles != null)
            {
                var emission = flowParticles.emission;
                emission.rateOverTime = particleEmissionRate * flowIntensity;
            }
        }

        /// <summary>
        /// Reset pipe to empty state
        /// </summary>
        public void ResetFlow()
        {
            if (fillCoroutine != null) { StopCoroutine(fillCoroutine); fillCoroutine = null; }
            if (fadeCoroutine != null) { StopCoroutine(fadeCoroutine); fadeCoroutine = null; }

            isFlowing = false;
            flowDirection = FlowDirection.None;
            flowIntensity = 0f;
            fillProgress = 0f;
            flowState = PipeFlowState.Empty;

            // Switch back to metal material
            SetMetalMode();

            if (flowParticles != null)
            {
                flowParticles.Stop();
                flowParticles.Clear();
            }

            UpdateMaterial();
        }

        /// <summary>
        /// Switch to metal/opaque pipe material
        /// </summary>
        public void SetMetalMode()
        {
            if (!useGlassPipeEffect || metalMaterial == null) return;
            SetPipeMaterial(metalMaterial);
            isGlassModeActive = false;
        }

        /// <summary>
        /// Switch to glass pipe material with water
        /// </summary>
        public void SetGlassMode()
        {
            if (!useGlassPipeEffect || glassPipeMaterial == null) return;
            SetPipeMaterial(glassPipeMaterial);
            isGlassModeActive = true;
        }

        private void SetPipeMaterial(Material mat)
        {
            if (pipeRenderer == null || mat == null) return;
            
            Material[] mats = pipeRenderer.sharedMaterials;
            if (materialIndex >= 0 && materialIndex < mats.Length)
            {
                mats[materialIndex] = mat;
                pipeRenderer.sharedMaterials = mats;
            }
        }

        /// <summary>
        /// Instantly fill the pipe (skip animation)
        /// </summary>
        public void FillInstant()
        {
            if (fillCoroutine != null) StopCoroutine(fillCoroutine);
            
            if (useGlassPipeEffect) SetGlassMode();
            
            fillProgress = 1f;
            flowIntensity = 1f;
            flowState = PipeFlowState.Filled;
            isFlowing = true;
            
            UpdateMaterial();
            OnFillProgressChanged?.Invoke(fillProgress);
            OnPipeFilled?.Invoke();
        }

        /// <summary>
        /// Instantly empty the pipe (skip animation)
        /// </summary>
        public void EmptyInstant()
        {
            if (fillCoroutine != null) StopCoroutine(fillCoroutine);
            
            fillProgress = 0f;
            flowIntensity = 0f;
            flowState = PipeFlowState.Empty;
            isFlowing = false;
            flowDirection = FlowDirection.None;
            
            SetMetalMode();
            UpdateMaterial();
            OnFillProgressChanged?.Invoke(fillProgress);
            OnPipeEmptied?.Invoke();
        }

        private IEnumerator FillPipe()
        {
            flowState = PipeFlowState.Filling;
            float fillTime = CalculateFillTime();
            float startProgress = fillProgress;
            float elapsed = 0f;

            // Fade in flow intensity
            fadeCoroutine = StartCoroutine(FadeFlowIntensity(1f));

            while (elapsed < fillTime && fillProgress < 1f)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / fillTime);
                
                // Smooth fill with ease
                fillProgress = Mathf.Lerp(startProgress, 1f, t * t * (3f - 2f * t));
                
                UpdateMaterial();
                OnFillProgressChanged?.Invoke(fillProgress);
                
                yield return null;
            }

            fillProgress = 1f;
            flowState = PipeFlowState.Filled;
            UpdateMaterial();
            OnFillProgressChanged?.Invoke(fillProgress);
            
            // Notify that pipe is filled
            OnPipeFilled?.Invoke();
            
            fillCoroutine = null;
        }

        private IEnumerator DrainPipe()
        {
            flowState = PipeFlowState.Draining;
            float drainTime = CalculateFillTime();
            float startProgress = fillProgress;
            float elapsed = 0f;

            while (elapsed < drainTime && fillProgress > 0f)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / drainTime);
                
                fillProgress = Mathf.Lerp(startProgress, 0f, t);
                
                UpdateMaterial();
                OnFillProgressChanged?.Invoke(fillProgress);
                
                yield return null;
            }

            fillProgress = 0f;
            flowState = PipeFlowState.Empty;
            isFlowing = false;
            flowDirection = FlowDirection.None;

            // Switch back to metal material when empty
            if (useGlassPipeEffect)
            {
                SetMetalMode();
            }

            // Fade out flow intensity
            fadeCoroutine = StartCoroutine(FadeFlowIntensity(0f));

            if (flowParticles != null)
                flowParticles.Stop();

            UpdateMaterial();
            OnFillProgressChanged?.Invoke(fillProgress);
            OnPipeEmptied?.Invoke();
            
            fillCoroutine = null;
        }

        private IEnumerator FadeFlowIntensity(float targetIntensity)
        {
            float startIntensity = flowIntensity;
            float elapsed = 0f;

            while (elapsed < fadeTime)
            {
                elapsed += Time.deltaTime;
                flowIntensity = Mathf.Lerp(startIntensity, targetIntensity, Mathf.Clamp01(elapsed / fadeTime));
                
                if (flowParticles != null)
                {
                    var emission = flowParticles.emission;
                    emission.rateOverTime = particleEmissionRate * flowIntensity;
                }
                
                UpdateMaterial();
                yield return null;
            }

            flowIntensity = targetIntensity;
            UpdateMaterial();
            fadeCoroutine = null;
        }

        private void UpdateMaterial()
        {
            if (pipeRenderer == null || propertyBlock == null) return;
            
            // Validate material index
            int matCount = pipeRenderer.sharedMaterials.Length;
            if (materialIndex < 0 || materialIndex >= matCount) return;

            // Bounds along the chosen local fill axis (object space)
            float fillMin, fillMax;
            switch (fillAxisLocal)
            {
                case PipeFillAxis.X: fillMin = boundsMin.x; fillMax = boundsMax.x; break;
                case PipeFillAxis.Z: fillMin = boundsMin.z; fillMax = boundsMax.z; break;
                default:             fillMin = boundsMin.y; fillMax = boundsMax.y; break;
            }

            // Reverse flow flips the fill direction in addition to the manual toggle.
            bool invert = invertFillDirection ^ (flowDirection == FlowDirection.Reverse);

            pipeRenderer.GetPropertyBlock(propertyBlock, materialIndex);
            
            propertyBlock.SetFloat(fillProgressProperty, fillProgress);
            propertyBlock.SetFloat(flowSpeedProperty, flowAnimationSpeed);
            propertyBlock.SetFloat(flowIntensityProperty, flowIntensity);
            propertyBlock.SetFloat(invertFillProperty, invert ? 1f : 0f);
            propertyBlock.SetFloat(fillMinProperty, fillMin);
            propertyBlock.SetFloat(fillMaxProperty, fillMax);
            propertyBlock.SetFloat(fillAxisProperty, (float)fillAxisLocal); // 0=X,1=Y,2=Z
            
            pipeRenderer.SetPropertyBlock(propertyBlock, materialIndex);
        }

        /// <summary>
        /// Set invert fill direction at runtime
        /// </summary>
        public void SetInvertFillDirection(bool invert)
        {
            invertFillDirection = invert;
            UpdateMaterial();
        }

        private void UpdateFlowState()
        {
            if (fillProgress <= 0.01f)
                flowState = PipeFlowState.Empty;
            else if (fillProgress >= 0.99f)
                flowState = PipeFlowState.Filled;
            else if (isFlowing)
                flowState = flowDirection != FlowDirection.None ? PipeFlowState.Filling : PipeFlowState.Draining;
        }

#if UNITY_EDITOR
        [ContextMenu("Test Fill Pipe")]
        private void TestFillPipe()
        {
            if (Application.isPlaying)
                StartFlow(FlowDirection.Forward);
            else
                Debug.Log("[PipeFlow] Test only works in Play Mode");
        }

        [ContextMenu("Test Drain Pipe")]
        private void TestDrainPipe()
        {
            if (Application.isPlaying)
                StopFlow();
            else
                Debug.Log("[PipeFlow] Test only works in Play Mode");
        }

        [ContextMenu("Log Fill Time")]
        private void LogFillTime()
        {
            Debug.Log($"[PipeFlow] Fill time at {flowRateLitersPerSecond} L/s: {CalculateFillTime():F2} seconds");
        }
        
        [ContextMenu("Toggle Invert Fill Direction")]
        private void ToggleInvertFill()
        {
            invertFillDirection = !invertFillDirection;
            Debug.Log($"[PipeFlow] Invert fill direction: {invertFillDirection}");
        }
#endif
    }

    /// <summary>
    /// State of pipe flow
    /// </summary>
    public enum PipeFlowState
    {
        Empty,
        Filling,
        Filled,
        Draining
    }

    /// <summary>
    /// Local axis of the pipe mesh along which water travels.
    /// Matches the shader's _FillAxis convention (0=X, 1=Y, 2=Z).
    /// </summary>
    public enum PipeFillAxis
    {
        X = 0,
        Y = 1,
        Z = 2
    }
}
