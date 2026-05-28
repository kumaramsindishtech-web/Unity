// ============================================================================
// Industrial Reactor Simulator - Bubble Effect Controller
// Copyright (c) 2024 Industrial Reactor Simulator. MIT License.
// ============================================================================

using UnityEngine;
using System.Collections;

namespace IndustrialReactorSimulator
{
    /// <summary>
    /// Controls bubble particle effects inside the reactor.
    /// </summary>
    [AddComponentMenu("Industrial Reactor/Bubble Effect Controller")]
    public class BubbleEffectController : MonoBehaviour
    {
        [Header("Particle System")]
        [SerializeField] private ParticleSystem bubbleParticles;
        [SerializeField] private ParticleSystem foamParticles;

        [Header("Emission Settings")]
        [SerializeField] private float minEmissionRate = 0f;
        [SerializeField] private float maxEmissionRate = 100f;
        [Range(0.1f, 5f)][SerializeField] private float fadeTime = 1f;

        [Header("Bubble Size")]
        [SerializeField] private float minBubbleSize = 0.01f;
        [SerializeField] private float maxBubbleSize = 0.05f;

        [Header("Bubble Movement")]
        [SerializeField] private float baseUpwardVelocity = 0.5f;
        [SerializeField] private float maxVelocityMultiplier = 2f;

        [Header("Runtime State")]
        [SerializeField][Range(0f, 1f)] private float currentIntensity = 0f;
        [SerializeField] private float targetIntensity = 0f;

        private ParticleSystem.EmissionModule bubbleEmission;
        private ParticleSystem.MainModule bubbleMain;
        private Coroutine fadeCoroutine;

        public float CurrentIntensity => currentIntensity;
        public bool IsActive => currentIntensity > 0.01f;

        private void Awake()
        {
            if (bubbleParticles != null)
            {
                bubbleEmission = bubbleParticles.emission;
                bubbleMain = bubbleParticles.main;
            }
        }

        private void Start() => SetIntensityImmediate(0f);

        private void OnEnable()
        {
            ReactorEvents.OnAgitatorRPMChanged += HandleAgitatorRPMChanged;
            ReactorEvents.OnSimulationReset += HandleSimulationReset;
        }

        private void OnDisable()
        {
            ReactorEvents.OnAgitatorRPMChanged -= HandleAgitatorRPMChanged;
            ReactorEvents.OnSimulationReset -= HandleSimulationReset;
        }

        private void HandleAgitatorRPMChanged(float rpm) => SetIntensity(Mathf.Clamp01(rpm / 300f));
        private void HandleSimulationReset() => ResetEffect();

        public void SetIntensity(float intensity)
        {
            targetIntensity = Mathf.Clamp01(intensity);
            if (fadeCoroutine != null) StopCoroutine(fadeCoroutine);
            fadeCoroutine = StartCoroutine(FadeToIntensity(targetIntensity));
        }

        public void SetIntensityImmediate(float intensity)
        {
            currentIntensity = Mathf.Clamp01(intensity);
            targetIntensity = currentIntensity;
            ApplyIntensity();
            ReactorEvents.RaiseBubbleIntensityChanged(currentIntensity);
        }

        public void ResetEffect()
        {
            if (fadeCoroutine != null) { StopCoroutine(fadeCoroutine); fadeCoroutine = null; }
            SetIntensityImmediate(0f);
            if (bubbleParticles != null) { bubbleParticles.Clear(); bubbleParticles.Stop(); }
            if (foamParticles != null) { foamParticles.Clear(); foamParticles.Stop(); }
        }

        private IEnumerator FadeToIntensity(float target)
        {
            float startIntensity = currentIntensity;
            float elapsed = 0f;
            float duration = fadeTime * Mathf.Abs(target - startIntensity);

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                currentIntensity = Mathf.Lerp(startIntensity, target, Mathf.Clamp01(elapsed / duration));
                ApplyIntensity();
                yield return null;
            }

            currentIntensity = target;
            ApplyIntensity();
            ReactorEvents.RaiseBubbleIntensityChanged(currentIntensity);
            fadeCoroutine = null;
        }

        private void ApplyIntensity()
        {
            if (bubbleParticles == null) return;
            bubbleEmission.rateOverTime = Mathf.Lerp(minEmissionRate, maxEmissionRate, currentIntensity);
            bubbleMain.startSize = Mathf.Lerp(minBubbleSize, maxBubbleSize, currentIntensity);
            bubbleMain.startSpeed = baseUpwardVelocity * Mathf.Lerp(1f, maxVelocityMultiplier, currentIntensity);

            if (currentIntensity > 0.01f) { if (!bubbleParticles.isPlaying) bubbleParticles.Play(); }
            else { if (bubbleParticles.isPlaying) bubbleParticles.Stop(); }

            if (foamParticles != null)
            {
                var foamEmission = foamParticles.emission;
                foamEmission.rateOverTime = maxEmissionRate * 0.3f * currentIntensity;
                if (currentIntensity > 0.3f) { if (!foamParticles.isPlaying) foamParticles.Play(); }
                else { if (foamParticles.isPlaying) foamParticles.Stop(); }
            }
        }
    }
}
