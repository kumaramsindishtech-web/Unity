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
        
        [Header("Glass Pipe Shader Properties")]
        [SerializeField] private string fillProgressProperty = "_FillProgress";
        [SerializeField] private string flowSpeedProperty = "_FlowSpeed";
        [SerializeField] private string flowIntensityProperty = "_FlowIntensity";
        [SerializeField] private string fillDirectionProperty = "_FillDirection";
        [SerializeField] private string invertFillProperty = "_InvertFill";
        
        [Header("Fill Direction Fix")]
        [Tooltip("Enable if water fills from wrong direction (UV mapping issue)")]
        [SerializeField] private bool invertFillDirection = false;
        [SerializeField] private string invertFillProperty = "_InvertFill";

        [Header("Visual Settings")]
        [Tooltip("UV scroll speed multiplier for flow animation")]
        [Range(0.1f, 10f)]
        [SerializeField] private float flowAnimationSpeed = 2f;
        [Tooltip("Flow direction: 1 = forward (top to bottom in UV), -1 = reverse")]
        [SerializeField] private float flowDirectionMultiplier = 1f;

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
#pragma warning disable CS0414 // Field is assigned but its value is never used
        private bool isGlassModeActive = false;
#pragma warning restore CS0414

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

        /// <summary>
        /// Flow rate in Liters per second - editable in inspector
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
            // Volume = Length * CrossSection (in cubic meters)
            float volumeCubicMeters = pipeLength * pipeCrossSectionArea;
            // Convert to liters (1 cubic meter = 1000 liters)
            float volumeLiters = volumeCubicMeters * 1000f;
            // Time = Volume / FlowRate
            return volumeLiters / flowRateLitersPerSecond;
        }

        private void Awake()
        {
            propertyBlock = new MaterialPropertyBlock();
            
            // Store original material
            if (pipeRenderer != null && pipeRenderer.sharedMaterials.Length > materialIndex)
            {
                originalMaterial = pipeRenderer.sharedMaterials[materialIndex];
            }
        }

        private void Start()
        {
            // Start with metal material
            if (!Application.isPlaying) return;
            
            // Ensure we start in metal mode with empty pipe
            SetMetalMode();
            fillProgress = 0f;
            flowIntensity = 0f;
            UpdateMaterial();
        }

        private void OnEnable()
        {
            // Ensure proper state in editor
            if (!Application.isPlaying && pipeRenderer != null)
            {
                if (propertyBlock == null) propertyBlock = new MaterialPropertyBlock();
                // Reset to metal appearance in editor
                if (metalMaterial != null)
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
                UpdateFlowAnimation();
            }
        }

        /// <summary>
        /// Start filling the pipe with water flow animation.
        /// Water visually fills from entry point to exit point based on flow direction.
        /// </summary>
        public void StartFlow(FlowDirection direction)
        {
            if (isFlowing && flowDirection == direction && flowState == PipeFlowState.Filled) return;

            flowDirection = direction;
            isFlowing = true;
            flowDirectionMultiplier = direction == FlowDirection.Reverse ? -1f : 1f;

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
            if (materialIndex < mats.Length)
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
                
                // Smooth fill with slight ease-out
                fillProgress = Mathf.Lerp(startProgress, 1f, t * t * (3f - 2f * t));
                
                UpdateMaterial();
                OnFillProgressChanged?.Invoke(fillProgress);
                
                yield return null;
            }

            fillProgress = 1f;
            flowState = PipeFlowState.Filled;
            UpdateMaterial();
            OnFillProgressChanged?.Invoke(fillProgress);
            
            // Notify that pipe is filled - tank can start filling
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

        private void UpdateFlowAnimation()
        {
            // Continuous flow animation is handled by shader using _Time
            // We just ensure the material properties are up to date
            UpdateMaterial();
        }

        private void UpdateMaterial()
        {
            if (pipeRenderer == null) return;

            pipeRenderer.GetPropertyBlock(propertyBlock, materialIndex);
            
            propertyBlock.SetFloat(fillProgressProperty, fillProgress);
            propertyBlock.SetFloat(flowSpeedProperty, flowAnimationSpeed);
            propertyBlock.SetFloat(fillDirectionProperty, flowDirectionMultiplier);
            propertyBlock.SetFloat(flowIntensityProperty, flowIntensity);
            propertyBlock.SetFloat(invertFillProperty, invertFillDirection ? 1f : 0f);
            
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
        }

        [ContextMenu("Test Drain Pipe")]
        private void TestDrainPipe()
        {
            if (Application.isPlaying)
                StopFlow();
        }

        [ContextMenu("Log Fill Time")]
        private void LogFillTime()
        {
            Debug.Log($"[PipeFlow] Fill time at {flowRateLitersPerSecond} L/s: {CalculateFillTime():F2} seconds");
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
}
