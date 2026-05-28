// ============================================================================
// Industrial Reactor Simulator - Pipe Flow Visualizer
// Version: 1.0.0
// Description: Visualizes flow through pipes using UV scrolling or particles
// ============================================================================

using UnityEngine;
using System.Collections;
using IndustrialReactorSimulator.Core;

namespace IndustrialReactorSimulator.Controllers
{
    /// <summary>
    /// Visualizes fluid flow through pipes using shader UV scrolling
    /// or optional particle effects.
    /// </summary>
    [AddComponentMenu("Industrial Reactor/Pipe Flow Visualizer")]
    public class PipeFlowVisualizer : MonoBehaviour
    {
        // ====================================================================
        // CONFIGURATION
        // ====================================================================
        
        [Header("Pipe Configuration")]
        [Tooltip("Type of valve this pipe is associated with")]
        [SerializeField] private ValveType associatedValveType = ValveType.Inlet;
        
        [Tooltip("Renderer for pipe material")]
        [SerializeField] private Renderer pipeRenderer;
        
        [Tooltip("Material index if multiple materials")]
        [SerializeField] private int materialIndex = 0;

        [Header("UV Scroll Settings")]
        [Tooltip("Base scroll speed")]
        [SerializeField] private float scrollSpeed = 2f;
        
        [Tooltip("Scroll direction (usually along pipe axis)")]
        [SerializeField] private Vector2 scrollDirection = new Vector2(0f, 1f);
        
        [Tooltip("Shader property for UV offset")]
        [SerializeField] private string uvOffsetProperty = "_MainTex_ST";
        
        [Tooltip("Alternative: use _BaseMap offset")]
        [SerializeField] private bool useBaseMapOffset = true;


        [Header("Optional Particle Flow")]
        [Tooltip("Particle system for flow visualization")]
        [SerializeField] private ParticleSystem flowParticles;
        
        [Tooltip("Particle emission rate when flowing")]
        [SerializeField] private float particleEmissionRate = 50f;

        [Header("Flow Intensity")]
        [Tooltip("Use flow speed from valve controller")]
        [SerializeField] private bool useValveFlowSpeed = true;
        
        [Tooltip("Intensity multiplier")]
        [Range(0.1f, 5f)]
        [SerializeField] private float intensityMultiplier = 1f;

        [Header("Transition")]
        [Tooltip("Time to fade flow effect in/out")]
        [Range(0.1f, 2f)]
        [SerializeField] private float fadeTime = 0.3f;

        // ====================================================================
        // RUNTIME STATE
        // ====================================================================
        
        [Header("Runtime State (Read-Only)")]
        [SerializeField] private bool isFlowing = false;
        [SerializeField] private FlowDirection flowDirection = FlowDirection.None;
        [SerializeField] [Range(0f, 1f)] private float flowIntensity = 0f;
        
        private float currentOffset = 0f;
        private MaterialPropertyBlock propertyBlock;
        private Coroutine fadeCoroutine;

        // ====================================================================
        // PROPERTIES
        // ====================================================================
        
        public ValveType AssociatedValveType => associatedValveType;
        public bool IsFlowing => isFlowing;
        public FlowDirection CurrentDirection => flowDirection;
        public float FlowIntensity => flowIntensity;



        // ====================================================================
        // UNITY LIFECYCLE
        // ====================================================================
        
        private void Awake()
        {
            propertyBlock = new MaterialPropertyBlock();
        }

        private void Update()
        {
            if (!Application.isPlaying) return;
            
            if (isFlowing && flowIntensity > 0f)
            {
                UpdateFlowAnimation();
            }
        }

        // ====================================================================
        // PUBLIC METHODS
        // ====================================================================
        
        /// <summary>
        /// Start flow animation in specified direction
        /// </summary>
        public void StartFlow(FlowDirection direction)
        {
            if (isFlowing && flowDirection == direction) return;
            
            flowDirection = direction;
            isFlowing = true;
            
            Debug.Log($"[PipeFlow] Starting flow: {associatedValveType} pipe, direction: {direction}");
            
            if (fadeCoroutine != null)
            {
                StopCoroutine(fadeCoroutine);
            }
            
            fadeCoroutine = StartCoroutine(FadeFlow(1f));
            
            // Start particles if available
            if (flowParticles != null && !flowParticles.isPlaying)
            {
                flowParticles.Play();
            }
        }

        /// <summary>
        /// Stop flow animation
        /// </summary>
        public void StopFlow()
        {
            if (!isFlowing) return;
            
            Debug.Log($"[PipeFlow] Stopping flow: {associatedValveType} pipe");
            
            if (fadeCoroutine != null)
            {
                StopCoroutine(fadeCoroutine);
            }
            
            fadeCoroutine = StartCoroutine(FadeFlow(0f));
        }

        /// <summary>
        /// Set flow intensity directly (0-1)
        /// </summary>
        public void SetFlowIntensity(float intensity)
        {
            flowIntensity = Mathf.Clamp01(intensity);
            
            if (flowParticles != null)
            {
                var emission = flowParticles.emission;
                emission.rateOverTime = particleEmissionRate * flowIntensity;
            }
        }

        /// <summary>
        /// Reset flow to initial state
        /// </summary>
        public void ResetFlow()
        {
            if (fadeCoroutine != null)
            {
                StopCoroutine(fadeCoroutine);
                fadeCoroutine = null;
            }
            
            isFlowing = false;
            flowDirection = FlowDirection.None;
            flowIntensity = 0f;
            currentOffset = 0f;
            
            if (flowParticles != null)
            {
                flowParticles.Stop();
                flowParticles.Clear();
            }
            
            UpdateMaterial();
        }



        // ====================================================================
        // FLOW ANIMATION
        // ====================================================================
        
        private void UpdateFlowAnimation()
        {
            // Calculate scroll direction based on flow direction
            float directionMultiplier = flowDirection == FlowDirection.Reverse ? -1f : 1f;
            
            // Update offset
            float speed = scrollSpeed * intensityMultiplier * flowIntensity * directionMultiplier;
            currentOffset += speed * Time.deltaTime;
            
            // Keep offset in reasonable range
            if (currentOffset > 100f) currentOffset -= 100f;
            if (currentOffset < -100f) currentOffset += 100f;
            
            UpdateMaterial();
        }

        private void UpdateMaterial()
        {
            if (pipeRenderer == null) return;
            
            pipeRenderer.GetPropertyBlock(propertyBlock, materialIndex);
            
            Vector2 offset = scrollDirection * currentOffset;
            
            if (useBaseMapOffset)
            {
                propertyBlock.SetVector("_BaseMap_ST", new Vector4(1, 1, offset.x, offset.y));
            }
            else
            {
                propertyBlock.SetVector(uvOffsetProperty, new Vector4(1, 1, offset.x, offset.y));
            }
            
            pipeRenderer.SetPropertyBlock(propertyBlock, materialIndex);
        }

        // ====================================================================
        // FADE TRANSITION
        // ====================================================================
        
        private IEnumerator FadeFlow(float targetIntensity)
        {
            float startIntensity = flowIntensity;
            float elapsed = 0f;

            while (elapsed < fadeTime)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / fadeTime);
                
                flowIntensity = Mathf.Lerp(startIntensity, targetIntensity, t);
                
                // Update particle emission
                if (flowParticles != null)
                {
                    var emission = flowParticles.emission;
                    emission.rateOverTime = particleEmissionRate * flowIntensity;
                }
                
                yield return null;
            }

            flowIntensity = targetIntensity;
            
            if (targetIntensity <= 0f)
            {
                isFlowing = false;
                flowDirection = FlowDirection.None;
                
                if (flowParticles != null)
                {
                    flowParticles.Stop();
                }
            }
            
            fadeCoroutine = null;
        }

        // ====================================================================
        // EDITOR HELPERS
        // ====================================================================
        
        #if UNITY_EDITOR
        [ContextMenu("Test Flow Forward")]
        private void TestFlowForward()
        {
            StartFlow(FlowDirection.Forward);
        }

        [ContextMenu("Test Flow Reverse")]
        private void TestFlowReverse()
        {
            StartFlow(FlowDirection.Reverse);
        }

        [ContextMenu("Stop Test Flow")]
        private void StopTestFlow()
        {
            StopFlow();
        }

        private void OnDrawGizmosSelected()
        {
            // Draw flow direction arrow
            Gizmos.color = Color.cyan;
            
            Vector3 start = transform.position;
            Vector3 direction = transform.TransformDirection(
                new Vector3(scrollDirection.x, 0f, scrollDirection.y)
            );
            
            Gizmos.DrawRay(start, direction * 0.5f);
        }
        #endif
    }
}
