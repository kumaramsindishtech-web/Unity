// ============================================================================
// Industrial Reactor Simulator - Pipe Flow Visualizer
// Copyright (c) 2024 Industrial Reactor Simulator. MIT License.
// ============================================================================

using UnityEngine;
using System.Collections;

namespace IndustrialReactorSimulator
{
    /// <summary>
    /// Visualizes fluid flow through pipes using shader UV scrolling.
    /// </summary>
    [AddComponentMenu("Industrial Reactor/Pipe Flow Visualizer")]
    public class PipeFlowVisualizer : MonoBehaviour
    {
        [Header("Pipe Configuration")]
        [SerializeField] private ValveType associatedValveType = ValveType.Inlet;
        [SerializeField] private Renderer pipeRenderer;
        [SerializeField] private int materialIndex = 0;

        [Header("UV Scroll Settings")]
        [SerializeField] private float scrollSpeed = 2f;
        [SerializeField] private Vector2 scrollDirection = new Vector2(0f, 1f);
        [SerializeField] private string uvOffsetProperty = "_MainTex_ST";
        [SerializeField] private bool useBaseMapOffset = true;

        [Header("Optional Particle Flow")]
        [SerializeField] private ParticleSystem flowParticles;
        [SerializeField] private float particleEmissionRate = 50f;

        [Header("Flow Intensity")]
        [SerializeField] private bool useValveFlowSpeed = true;
        [Range(0.1f, 5f)][SerializeField] private float intensityMultiplier = 1f;

        [Header("Transition")]
        [Range(0.1f, 2f)][SerializeField] private float fadeTime = 0.3f;

        [Header("Runtime State")]
        [SerializeField] private bool isFlowing = false;
        [SerializeField] private FlowDirection flowDirection = FlowDirection.None;
        [SerializeField][Range(0f, 1f)] private float flowIntensity = 0f;

        private float currentOffset = 0f;
        private MaterialPropertyBlock propertyBlock;
        private Coroutine fadeCoroutine;

        public ValveType AssociatedValveType => associatedValveType;
        public bool IsFlowing => isFlowing;
        public FlowDirection CurrentDirection => flowDirection;
        public float FlowIntensity => flowIntensity;

        private void Awake() => propertyBlock = new MaterialPropertyBlock();

        private void Update()
        {
            if (!Application.isPlaying) return;
            if (isFlowing && flowIntensity > 0f) UpdateFlowAnimation();
        }

        public void StartFlow(FlowDirection direction)
        {
            if (isFlowing && flowDirection == direction) return;
            flowDirection = direction;
            isFlowing = true;
            if (fadeCoroutine != null) StopCoroutine(fadeCoroutine);
            fadeCoroutine = StartCoroutine(FadeFlow(1f));
            if (flowParticles != null && !flowParticles.isPlaying) flowParticles.Play();
        }

        public void StopFlow()
        {
            if (!isFlowing) return;
            if (fadeCoroutine != null) StopCoroutine(fadeCoroutine);
            fadeCoroutine = StartCoroutine(FadeFlow(0f));
        }

        public void SetFlowIntensity(float intensity)
        {
            flowIntensity = Mathf.Clamp01(intensity);
            if (flowParticles != null)
            {
                var emission = flowParticles.emission;
                emission.rateOverTime = particleEmissionRate * flowIntensity;
            }
        }

        public void ResetFlow()
        {
            if (fadeCoroutine != null) { StopCoroutine(fadeCoroutine); fadeCoroutine = null; }
            isFlowing = false;
            flowDirection = FlowDirection.None;
            flowIntensity = 0f;
            currentOffset = 0f;
            if (flowParticles != null) { flowParticles.Stop(); flowParticles.Clear(); }
            UpdateMaterial();
        }

        private void UpdateFlowAnimation()
        {
            float directionMultiplier = flowDirection == FlowDirection.Reverse ? -1f : 1f;
            currentOffset += scrollSpeed * intensityMultiplier * flowIntensity * directionMultiplier * Time.deltaTime;
            if (currentOffset > 100f) currentOffset -= 100f;
            if (currentOffset < -100f) currentOffset += 100f;
            UpdateMaterial();
        }

        private void UpdateMaterial()
        {
            if (pipeRenderer == null) return;
            pipeRenderer.GetPropertyBlock(propertyBlock, materialIndex);
            Vector2 offset = scrollDirection * currentOffset;
            if (useBaseMapOffset) propertyBlock.SetVector("_BaseMap_ST", new Vector4(1, 1, offset.x, offset.y));
            else propertyBlock.SetVector(uvOffsetProperty, new Vector4(1, 1, offset.x, offset.y));
            pipeRenderer.SetPropertyBlock(propertyBlock, materialIndex);
        }

        private IEnumerator FadeFlow(float targetIntensity)
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
                yield return null;
            }

            flowIntensity = targetIntensity;
            if (targetIntensity <= 0f)
            {
                isFlowing = false;
                flowDirection = FlowDirection.None;
                if (flowParticles != null) flowParticles.Stop();
            }
            fadeCoroutine = null;
        }
    }
}
