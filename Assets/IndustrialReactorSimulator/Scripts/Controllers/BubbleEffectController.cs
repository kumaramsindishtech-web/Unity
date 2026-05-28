// ============================================================================
// Industrial Reactor Simulator - Bubble Effect Controller
// Version: 1.0.0
// Description: Controls bubble particle effects based on agitator state
// ============================================================================

using UnityEngine;
using System.Collections;
using IndustrialReactorSimulator.Core;

namespace IndustrialReactorSimulator.Controllers
{
    /// <summary>
    /// Controls bubble particle effects inside the reactor.
    /// Intensity scales with agitator RPM and water level.
    /// </summary>
    [AddComponentMenu("Industrial Reactor/Bubble Effect Controller")]
    public class BubbleEffectController : MonoBehaviour
    {
        // ====================================================================
        // CONFIGURATION
        // ====================================================================
        
        [Header("Particle System")]
        [Tooltip("Main bubble particle system")]
        [SerializeField] private ParticleSystem bubbleParticles;
        
        [Tooltip("Secondary foam/spray particles (optional)")]
        [SerializeField] private ParticleSystem foamParticles;

        [Header("Emission Settings")]
        [Tooltip("Minimum emission rate")]
        [SerializeField] private float minEmissionRate = 0f;
        
        [Tooltip("Maximum emission rate at full intensity")]
        [SerializeField] private float maxEmissionRate = 100f;
        
        [Tooltip("Time for effect to fade in/out")]
        [Range(0.1f, 5f)]
        [SerializeField] private float fadeTime = 1f;


        [Header("Bubble Size")]
        [Tooltip("Minimum bubble size")]
        [SerializeField] private float minBubbleSize = 0.01f;
        
        [Tooltip("Maximum bubble size at full intensity")]
        [SerializeField] private float maxBubbleSize = 0.05f;

        [Header("Bubble Movement")]
        [Tooltip("Base upward velocity")]
        [SerializeField] private float baseUpwardVelocity = 0.5f;
        
        [Tooltip("Velocity multiplier at high intensity")]
        [SerializeField] private float maxVelocityMultiplier = 2f;
        
        [Tooltip("Swirl force applied to bubbles")]
        [SerializeField] private float swirlForce = 1f;

        // ====================================================================
        // RUNTIME STATE
        // ====================================================================
        
        [Header("Runtime State (Read-Only)")]
        [SerializeField] [Range(0f, 1f)] private float currentIntensity = 0f;
        [SerializeField] private float targetIntensity = 0f;
        
        private ParticleSystem.EmissionModule bubbleEmission;
        private ParticleSystem.MainModule bubbleMain;
        private ParticleSystem.VelocityOverLifetimeModule bubbleVelocity;
        private Coroutine fadeCoroutine;

        // ====================================================================
        // PROPERTIES
        // ====================================================================
        
        public float CurrentIntensity => currentIntensity;
        public bool IsActive => currentIntensity > 0.01f;

        // ====================================================================
        // UNITY LIFECYCLE
        // ====================================================================
        
        private void Awake()
        {
            if (bubbleParticles != null)
            {
                bubbleEmission = bubbleParticles.emission;
                bubbleMain = bubbleParticles.main;
                bubbleVelocity = bubbleParticles.velocityOverLifetime;
            }
        }

        private void Start()
        {
            // Ensure particles start stopped
            SetIntensityImmediate(0f);
        }

        private void OnEnable()
        {
            // Subscribe to agitator events
            ReactorEvents.OnAgitatorRPMChanged += HandleAgitatorRPMChanged;
            ReactorEvents.OnSimulationReset += HandleSimulationReset;
        }

        private void OnDisable()
        {
            ReactorEvents.OnAgitatorRPMChanged -= HandleAgitatorRPMChanged;
            ReactorEvents.OnSimulationReset -= HandleSimulationReset;
        }



        // ====================================================================
        // EVENT HANDLERS
        // ====================================================================
        
        private void HandleAgitatorRPMChanged(float rpm)
        {
            // Intensity based on RPM (assuming max RPM around 300)
            float normalizedRPM = Mathf.Clamp01(rpm / 300f);
            SetIntensity(normalizedRPM);
        }

        private void HandleSimulationReset()
        {
            ResetEffect();
        }

        // ====================================================================
        // PUBLIC METHODS
        // ====================================================================
        
        /// <summary>
        /// Set bubble intensity with smooth transition (0-1)
        /// </summary>
        public void SetIntensity(float intensity)
        {
            targetIntensity = Mathf.Clamp01(intensity);
            
            if (fadeCoroutine != null)
            {
                StopCoroutine(fadeCoroutine);
            }
            
            fadeCoroutine = StartCoroutine(FadeToIntensity(targetIntensity));
        }

        /// <summary>
        /// Set intensity immediately without transition
        /// </summary>
        public void SetIntensityImmediate(float intensity)
        {
            currentIntensity = Mathf.Clamp01(intensity);
            targetIntensity = currentIntensity;
            ApplyIntensity();
            
            ReactorEvents.RaiseBubbleIntensityChanged(currentIntensity);
        }

        /// <summary>
        /// Reset effect to inactive state
        /// </summary>
        public void ResetEffect()
        {
            if (fadeCoroutine != null)
            {
                StopCoroutine(fadeCoroutine);
                fadeCoroutine = null;
            }
            
            SetIntensityImmediate(0f);
            
            if (bubbleParticles != null)
            {
                bubbleParticles.Clear();
                bubbleParticles.Stop();
            }
            
            if (foamParticles != null)
            {
                foamParticles.Clear();
                foamParticles.Stop();
            }
        }



        // ====================================================================
        // FADE TRANSITION
        // ====================================================================
        
        private IEnumerator FadeToIntensity(float target)
        {
            float startIntensity = currentIntensity;
            float elapsed = 0f;
            
            // Calculate duration based on intensity difference
            float intensityDiff = Mathf.Abs(target - startIntensity);
            float duration = fadeTime * intensityDiff;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                
                currentIntensity = Mathf.Lerp(startIntensity, target, t);
                ApplyIntensity();
                
                yield return null;
            }

            currentIntensity = target;
            ApplyIntensity();
            
            ReactorEvents.RaiseBubbleIntensityChanged(currentIntensity);
            
            fadeCoroutine = null;
        }

        // ====================================================================
        // APPLY INTENSITY TO PARTICLES
        // ====================================================================
        
        private void ApplyIntensity()
        {
            if (bubbleParticles == null) return;
            
            // Update emission rate
            bubbleEmission.rateOverTime = Mathf.Lerp(minEmissionRate, maxEmissionRate, currentIntensity);
            
            // Update bubble size
            float bubbleSize = Mathf.Lerp(minBubbleSize, maxBubbleSize, currentIntensity);
            bubbleMain.startSize = bubbleSize;
            
            // Update velocity
            float velocityMult = Mathf.Lerp(1f, maxVelocityMultiplier, currentIntensity);
            bubbleMain.startSpeed = baseUpwardVelocity * velocityMult;
            
            // Control play state
            if (currentIntensity > 0.01f)
            {
                if (!bubbleParticles.isPlaying)
                {
                    bubbleParticles.Play();
                }
            }
            else
            {
                if (bubbleParticles.isPlaying)
                {
                    bubbleParticles.Stop();
                }
            }
            
            // Update foam particles if present
            if (foamParticles != null)
            {
                var foamEmission = foamParticles.emission;
                foamEmission.rateOverTime = maxEmissionRate * 0.3f * currentIntensity;
                
                if (currentIntensity > 0.3f)
                {
                    if (!foamParticles.isPlaying) foamParticles.Play();
                }
                else
                {
                    if (foamParticles.isPlaying) foamParticles.Stop();
                }
            }
        }

        // ====================================================================
        // EDITOR HELPERS
        // ====================================================================
        
        #if UNITY_EDITOR
        [ContextMenu("Test Half Intensity")]
        private void TestHalfIntensity()
        {
            SetIntensityImmediate(0.5f);
        }

        [ContextMenu("Test Full Intensity")]
        private void TestFullIntensity()
        {
            SetIntensityImmediate(1f);
        }

        [ContextMenu("Stop Effect")]
        private void StopEffect()
        {
            ResetEffect();
        }
        #endif
    }
}
