// ============================================================================
// Industrial Reactor Simulator - Agitator Controller
// Version: 1.0.0
// Description: Controls agitator rotation with smooth RPM transitions
// ============================================================================

using UnityEngine;
using System.Collections;
using IndustrialReactorSimulator.Core;

namespace IndustrialReactorSimulator.Controllers
{
    /// <summary>
    /// Controls the agitator component with smooth RPM-based rotation.
    /// Handles spin-up, spin-down, and RPM changes.
    /// </summary>
    [AddComponentMenu("Industrial Reactor/Agitator Controller")]
    public class AgitatorController : MonoBehaviour
    {
        // ====================================================================
        // CONFIGURATION
        // ====================================================================
        
        [Header("Agitator Configuration")]
        [Tooltip("Transform to rotate (agitator shaft/blades)")]
        [SerializeField] private Transform agitatorTransform;
        
        [Tooltip("Rotation axis in local space")]
        [SerializeField] private Vector3 rotationAxis = Vector3.up;
        
        [Tooltip("Maximum RPM")]
        [Range(10f, 1000f)]
        [SerializeField] private float maxRPM = 300f;
        
        [Tooltip("Time to reach target RPM from zero")]
        [Range(0.5f, 10f)]
        [SerializeField] private float spinUpTime = 2f;
        
        [Tooltip("Time to stop from max RPM")]
        [Range(0.5f, 10f)]
        [SerializeField] private float spinDownTime = 3f;

        [Header("Audio (Optional)")]
        [Tooltip("Audio source for motor sound")]
        [SerializeField] private AudioSource motorAudio;
        
        [Tooltip("Maximum audio pitch at max RPM")]
        [Range(0.5f, 2f)]
        [SerializeField] private float maxAudioPitch = 1.5f;



        // ====================================================================
        // RUNTIME STATE
        // ====================================================================
        
        [Header("Runtime State (Read-Only)")]
        [SerializeField] private AgitatorState currentState = AgitatorState.Stopped;
        [SerializeField] private float currentRPM = 0f;
        [SerializeField] private float targetRPM = 0f;
        
        private Coroutine rpmTransitionCoroutine;
        private float currentRotation = 0f;

        // ====================================================================
        // PROPERTIES
        // ====================================================================
        
        public AgitatorState CurrentState => currentState;
        public float CurrentRPM => currentRPM;
        public float TargetRPM => targetRPM;
        public float NormalizedRPM => maxRPM > 0 ? currentRPM / maxRPM : 0f;
        public bool IsRunning => currentState == AgitatorState.Running;
        public bool IsStopped => currentState == AgitatorState.Stopped;

        // ====================================================================
        // UNITY LIFECYCLE
        // ====================================================================
        
        private void Awake()
        {
            if (agitatorTransform == null)
            {
                agitatorTransform = transform;
            }
        }

        private void Update()
        {
            if (!Application.isPlaying) return;
            
            // Rotate based on current RPM
            if (currentRPM > 0f)
            {
                float degreesPerSecond = currentRPM * 6f; // RPM to degrees/sec
                currentRotation += degreesPerSecond * Time.deltaTime;
                
                if (agitatorTransform != null)
                {
                    agitatorTransform.localRotation = Quaternion.AngleAxis(currentRotation, rotationAxis);
                }
            }
            
            // Update audio
            UpdateAudio();
        }



        // ====================================================================
        // PUBLIC METHODS
        // ====================================================================
        
        /// <summary>
        /// Start the agitator with target RPM
        /// </summary>
        public void StartAgitator(float rpm)
        {
            if (currentState == AgitatorState.Running && Mathf.Approximately(targetRPM, rpm))
            {
                return;
            }

            targetRPM = Mathf.Clamp(rpm, 0f, maxRPM);
            
            Debug.Log($"[AgitatorController] Starting agitator to {targetRPM} RPM...");
            
            if (rpmTransitionCoroutine != null)
            {
                StopCoroutine(rpmTransitionCoroutine);
            }
            
            rpmTransitionCoroutine = StartCoroutine(TransitionRPM(targetRPM, spinUpTime));
        }

        /// <summary>
        /// Stop the agitator with smooth deceleration
        /// </summary>
        public void StopAgitator()
        {
            if (currentState == AgitatorState.Stopped)
            {
                return;
            }

            Debug.Log("[AgitatorController] Stopping agitator...");
            
            targetRPM = 0f;
            
            if (rpmTransitionCoroutine != null)
            {
                StopCoroutine(rpmTransitionCoroutine);
            }
            
            rpmTransitionCoroutine = StartCoroutine(TransitionRPM(0f, spinDownTime));
        }

        /// <summary>
        /// Set new target RPM while running
        /// </summary>
        public void SetTargetRPM(float rpm)
        {
            targetRPM = Mathf.Clamp(rpm, 0f, maxRPM);
            
            if (currentState == AgitatorState.Running || currentState == AgitatorState.Starting)
            {
                if (rpmTransitionCoroutine != null)
                {
                    StopCoroutine(rpmTransitionCoroutine);
                }
                
                float transitionTime = rpm > currentRPM ? spinUpTime : spinDownTime;
                rpmTransitionCoroutine = StartCoroutine(TransitionRPM(rpm, transitionTime));
            }
        }



        /// <summary>
        /// Reset agitator to stopped state immediately
        /// </summary>
        public void ResetAgitator()
        {
            if (rpmTransitionCoroutine != null)
            {
                StopCoroutine(rpmTransitionCoroutine);
                rpmTransitionCoroutine = null;
            }
            
            currentRPM = 0f;
            targetRPM = 0f;
            currentRotation = 0f;
            currentState = AgitatorState.Stopped;
            
            if (agitatorTransform != null)
            {
                agitatorTransform.localRotation = Quaternion.identity;
            }
            
            ReactorEvents.RaiseAgitatorStateChanged(currentState);
            ReactorEvents.RaiseAgitatorRPMChanged(currentRPM);
        }

        // ====================================================================
        // RPM TRANSITION COROUTINE
        // ====================================================================
        
        private IEnumerator TransitionRPM(float target, float duration)
        {
            bool isSpinningUp = target > currentRPM;
            currentState = isSpinningUp ? AgitatorState.Starting : AgitatorState.Stopping;
            ReactorEvents.RaiseAgitatorStateChanged(currentState);

            float startRPM = currentRPM;
            float elapsed = 0f;
            
            // Scale duration based on actual RPM difference
            float rpmDifference = Mathf.Abs(target - startRPM);
            float scaledDuration = duration * (rpmDifference / maxRPM);
            scaledDuration = Mathf.Max(scaledDuration, 0.1f);

            while (elapsed < scaledDuration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / scaledDuration);
                
                // Use smooth curve for natural acceleration/deceleration
                if (isSpinningUp)
                {
                    t = 1f - Mathf.Pow(1f - t, 2f); // Ease out
                }
                else
                {
                    t = Mathf.Pow(t, 2f); // Ease in
                }
                
                currentRPM = Mathf.Lerp(startRPM, target, t);
                ReactorEvents.RaiseAgitatorRPMChanged(currentRPM);
                
                yield return null;
            }

            currentRPM = target;
            currentState = target > 0f ? AgitatorState.Running : AgitatorState.Stopped;
            
            ReactorEvents.RaiseAgitatorStateChanged(currentState);
            ReactorEvents.RaiseAgitatorRPMChanged(currentRPM);
            
            Debug.Log($"[AgitatorController] Agitator {currentState} at {currentRPM} RPM");
            
            rpmTransitionCoroutine = null;
        }



        // ====================================================================
        // AUDIO
        // ====================================================================
        
        private void UpdateAudio()
        {
            if (motorAudio == null) return;
            
            if (currentRPM > 0f)
            {
                if (!motorAudio.isPlaying)
                {
                    motorAudio.Play();
                }
                
                // Adjust pitch based on RPM
                float normalizedRPM = currentRPM / maxRPM;
                motorAudio.pitch = Mathf.Lerp(0.5f, maxAudioPitch, normalizedRPM);
                motorAudio.volume = Mathf.Lerp(0.1f, 1f, normalizedRPM);
            }
            else
            {
                if (motorAudio.isPlaying)
                {
                    motorAudio.Stop();
                }
            }
        }

        // ====================================================================
        // EDITOR HELPERS
        // ====================================================================
        
        #if UNITY_EDITOR
        private void OnDrawGizmosSelected()
        {
            Transform target = agitatorTransform != null ? agitatorTransform : transform;
            
            // Draw rotation axis
            Gizmos.color = Color.cyan;
            Vector3 worldAxis = target.TransformDirection(rotationAxis);
            Gizmos.DrawRay(target.position, worldAxis * 0.5f);
            
            // Draw rotation indicator
            Gizmos.color = Color.yellow;
            UnityEditor.Handles.color = new Color(1f, 1f, 0f, 0.3f);
            UnityEditor.Handles.DrawSolidDisc(target.position, worldAxis, 0.2f);
        }
        #endif
    }
}
