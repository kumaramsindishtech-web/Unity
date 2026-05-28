// ============================================================================
// Industrial Reactor Simulator - Agitator Controller
// Copyright (c) 2024 Industrial Reactor Simulator. MIT License.
// ============================================================================

using UnityEngine;
using System.Collections;

namespace IndustrialReactorSimulator
{
    /// <summary>
    /// Controls the agitator component with smooth RPM-based rotation.
    /// </summary>
    [AddComponentMenu("Industrial Reactor/Agitator Controller")]
    public class AgitatorController : MonoBehaviour
    {
        [Header("Agitator Configuration")]
        [SerializeField] private Transform agitatorTransform;
        [SerializeField] private Vector3 rotationAxis = Vector3.up;
        [Range(10f, 1000f)][SerializeField] private float maxRPM = 300f;
        [Range(0.5f, 10f)][SerializeField] private float spinUpTime = 2f;
        [Range(0.5f, 10f)][SerializeField] private float spinDownTime = 3f;

        [Header("Audio (Optional)")]
        [SerializeField] private AudioSource motorAudio;
        [Range(0.5f, 2f)][SerializeField] private float maxAudioPitch = 1.5f;

        [Header("Runtime State")]
        [SerializeField] private AgitatorState currentState = AgitatorState.Stopped;
        [SerializeField] private float currentRPM = 0f;
        [SerializeField] private float targetRPM = 0f;

        private Coroutine rpmTransitionCoroutine;
        private float currentRotation = 0f;

        public AgitatorState CurrentState => currentState;
        public float CurrentRPM => currentRPM;
        public float TargetRPM => targetRPM;
        public float NormalizedRPM => maxRPM > 0 ? currentRPM / maxRPM : 0f;
        public bool IsRunning => currentState == AgitatorState.Running;
        public bool IsStopped => currentState == AgitatorState.Stopped;

        private void Awake() { if (agitatorTransform == null) agitatorTransform = transform; }

        private void Update()
        {
            if (!Application.isPlaying) return;
            if (currentRPM > 0f)
            {
                float degreesPerSecond = currentRPM * 6f;
                currentRotation += degreesPerSecond * Time.deltaTime;
                if (agitatorTransform != null) agitatorTransform.localRotation = Quaternion.AngleAxis(currentRotation, rotationAxis);
            }
            UpdateAudio();
        }

        public void StartAgitator(float rpm)
        {
            if (currentState == AgitatorState.Running && Mathf.Approximately(targetRPM, rpm)) return;
            targetRPM = Mathf.Clamp(rpm, 0f, maxRPM);
            if (rpmTransitionCoroutine != null) StopCoroutine(rpmTransitionCoroutine);
            rpmTransitionCoroutine = StartCoroutine(TransitionRPM(targetRPM, spinUpTime));
        }

        public void StopAgitator()
        {
            if (currentState == AgitatorState.Stopped) return;
            targetRPM = 0f;
            if (rpmTransitionCoroutine != null) StopCoroutine(rpmTransitionCoroutine);
            rpmTransitionCoroutine = StartCoroutine(TransitionRPM(0f, spinDownTime));
        }

        public void SetTargetRPM(float rpm)
        {
            targetRPM = Mathf.Clamp(rpm, 0f, maxRPM);
            if (currentState == AgitatorState.Running || currentState == AgitatorState.Starting)
            {
                if (rpmTransitionCoroutine != null) StopCoroutine(rpmTransitionCoroutine);
                float transitionTime = rpm > currentRPM ? spinUpTime : spinDownTime;
                rpmTransitionCoroutine = StartCoroutine(TransitionRPM(rpm, transitionTime));
            }
        }

        public void ResetAgitator()
        {
            if (rpmTransitionCoroutine != null) { StopCoroutine(rpmTransitionCoroutine); rpmTransitionCoroutine = null; }
            currentRPM = 0f; targetRPM = 0f; currentRotation = 0f;
            currentState = AgitatorState.Stopped;
            if (agitatorTransform != null) agitatorTransform.localRotation = Quaternion.identity;
            ReactorEvents.RaiseAgitatorStateChanged(currentState);
            ReactorEvents.RaiseAgitatorRPMChanged(currentRPM);
        }

        private IEnumerator TransitionRPM(float target, float duration)
        {
            bool isSpinningUp = target > currentRPM;
            currentState = isSpinningUp ? AgitatorState.Starting : AgitatorState.Stopping;
            ReactorEvents.RaiseAgitatorStateChanged(currentState);

            float startRPM = currentRPM;
            float elapsed = 0f;
            float scaledDuration = Mathf.Max(duration * (Mathf.Abs(target - startRPM) / maxRPM), 0.1f);

            while (elapsed < scaledDuration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / scaledDuration);
                t = isSpinningUp ? 1f - Mathf.Pow(1f - t, 2f) : Mathf.Pow(t, 2f);
                currentRPM = Mathf.Lerp(startRPM, target, t);
                ReactorEvents.RaiseAgitatorRPMChanged(currentRPM);
                yield return null;
            }

            currentRPM = target;
            currentState = target > 0f ? AgitatorState.Running : AgitatorState.Stopped;
            ReactorEvents.RaiseAgitatorStateChanged(currentState);
            ReactorEvents.RaiseAgitatorRPMChanged(currentRPM);
            rpmTransitionCoroutine = null;
        }

        private void UpdateAudio()
        {
            if (motorAudio == null) return;
            if (currentRPM > 0f)
            {
                if (!motorAudio.isPlaying) motorAudio.Play();
                float normalizedRPM = currentRPM / maxRPM;
                motorAudio.pitch = Mathf.Lerp(0.5f, maxAudioPitch, normalizedRPM);
                motorAudio.volume = Mathf.Lerp(0.1f, 1f, normalizedRPM);
            }
            else if (motorAudio.isPlaying) motorAudio.Stop();
        }
    }
}
