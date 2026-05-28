// ============================================================================
// Industrial Reactor Simulator - Valve Controller
// Version: 1.0.0
// Description: Controls valve open/close state with smooth transitions
// ============================================================================

using UnityEngine;
using System.Collections;
using IndustrialReactorSimulator.Core;

namespace IndustrialReactorSimulator.Controllers
{
    /// <summary>
    /// Controls a valve component with smooth open/close transitions.
    /// Handles valve state, rotation animation, and flow amount.
    /// </summary>
    [AddComponentMenu("Industrial Reactor/Valve Controller")]
    public class ValveController : MonoBehaviour
    {
        // ====================================================================
        // CONFIGURATION
        // ====================================================================
        
        [Header("Valve Configuration")]
        [Tooltip("Type of valve (Inlet or Outlet)")]
        [SerializeField] private ValveType valveType = ValveType.Inlet;
        
        [Tooltip("Transform to rotate when valve opens/closes")]
        [SerializeField] private Transform valveHandle;
        
        [Tooltip("Rotation axis for valve handle")]
        [SerializeField] private Vector3 rotationAxis = Vector3.up;
        
        [Tooltip("Rotation angle when fully open (degrees)")]
        [SerializeField] private float openRotation = 90f;
        
        [Tooltip("Time to fully open/close in seconds")]
        [Range(0.1f, 5f)]
        [SerializeField] private float transitionTime = 0.5f;

        [Header("Visual Feedback")]
        [Tooltip("Material to change color when valve state changes")]
        [SerializeField] private Renderer valveRenderer;
        
        [Tooltip("Material property name for color")]
        [SerializeField] private string colorPropertyName = "_BaseColor";
        
        [Tooltip("Color when valve is closed")]
        [SerializeField] private Color closedColor = Color.red;
        
        [Tooltip("Color when valve is open")]
        [SerializeField] private Color openColor = Color.green;

        // ====================================================================
        // RUNTIME STATE
        // ====================================================================
        
        [Header("Runtime State (Read-Only)")]
        [SerializeField] private ValveState currentState = ValveState.Closed;
        [SerializeField] [Range(0f, 1f)] private float openAmount = 0f;
        
        private Quaternion closedRotation;
        private Quaternion openRotationQuat;
        private Coroutine transitionCoroutine;
        private MaterialPropertyBlock propertyBlock;

        // ====================================================================
        // PROPERTIES
        // ====================================================================
        
        public ValveType ValveType => valveType;
        public ValveState CurrentState => currentState;
        public float OpenAmount => openAmount;
        public bool IsOpen => currentState == ValveState.Open;
        public bool IsClosed => currentState == ValveState.Closed;
        public bool IsTransitioning => currentState == ValveState.Opening || currentState == ValveState.Closing;

        // ====================================================================
        // UNITY LIFECYCLE
        // ====================================================================
        
        private void Awake()
        {
            propertyBlock = new MaterialPropertyBlock();
            
            // Store initial rotation as closed state
            if (valveHandle != null)
            {
                closedRotation = valveHandle.localRotation;
                openRotationQuat = closedRotation * Quaternion.AngleAxis(openRotation, rotationAxis);
            }
        }

        private void Start()
        {
            // Initialize visual state
            UpdateVisuals();
        }

        private void OnValidate()
        {
            // Update in editor when values change
            if (valveHandle != null && !Application.isPlaying)
            {
                closedRotation = valveHandle.localRotation;
                openRotationQuat = closedRotation * Quaternion.AngleAxis(openRotation, rotationAxis);
            }
        }

        // ====================================================================
        // PUBLIC METHODS
        // ====================================================================
        
        /// <summary>
        /// Open the valve with smooth transition
        /// </summary>
        public void OpenValve()
        {
            if (currentState == ValveState.Open || currentState == ValveState.Opening)
            {
                return;
            }

            Debug.Log($"[ValveController] Opening {valveType} valve...");
            
            if (transitionCoroutine != null)
            {
                StopCoroutine(transitionCoroutine);
            }
            
            transitionCoroutine = StartCoroutine(TransitionValve(true));
        }

        /// <summary>
        /// Close the valve with smooth transition
        /// </summary>
        public void CloseValve()
        {
            if (currentState == ValveState.Closed || currentState == ValveState.Closing)
            {
                return;
            }

            Debug.Log($"[ValveController] Closing {valveType} valve...");
            
            if (transitionCoroutine != null)
            {
                StopCoroutine(transitionCoroutine);
            }
            
            transitionCoroutine = StartCoroutine(TransitionValve(false));
        }

        /// <summary>
        /// Toggle valve state
        /// </summary>
        public void ToggleValve()
        {
            if (IsClosed || currentState == ValveState.Closing)
            {
                OpenValve();
            }
            else
            {
                CloseValve();
            }
        }

        /// <summary>
        /// Reset valve to closed state immediately
        /// </summary>
        public void ResetValve()
        {
            if (transitionCoroutine != null)
            {
                StopCoroutine(transitionCoroutine);
                transitionCoroutine = null;
            }
            
            openAmount = 0f;
            currentState = ValveState.Closed;
            
            if (valveHandle != null)
            {
                valveHandle.localRotation = closedRotation;
            }
            
            UpdateVisuals();
            ReactorEvents.RaiseValveStateChanged(valveType, currentState);
        }

        /// <summary>
        /// Set valve open amount directly (0-1)
        /// </summary>
        public void SetOpenAmount(float amount)
        {
            openAmount = Mathf.Clamp01(amount);
            
            if (valveHandle != null)
            {
                valveHandle.localRotation = Quaternion.Slerp(closedRotation, openRotationQuat, openAmount);
            }
            
            UpdateVisuals();
        }

        // ====================================================================
        // TRANSITION COROUTINE
        // ====================================================================
        
        private IEnumerator TransitionValve(bool opening)
        {
            currentState = opening ? ValveState.Opening : ValveState.Closing;
            ReactorEvents.RaiseValveStateChanged(valveType, currentState);

            float startAmount = openAmount;
            float targetAmount = opening ? 1f : 0f;
            float elapsed = 0f;
            
            // Calculate remaining time based on current position
            float remainingTransition = Mathf.Abs(targetAmount - startAmount);
            float duration = transitionTime * remainingTransition;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                
                // Smooth step for natural feel
                t = t * t * (3f - 2f * t);
                
                openAmount = Mathf.Lerp(startAmount, targetAmount, t);
                
                // Update rotation
                if (valveHandle != null)
                {
                    valveHandle.localRotation = Quaternion.Slerp(closedRotation, openRotationQuat, openAmount);
                }
                
                UpdateVisuals();
                
                yield return null;
            }

            // Ensure final state
            openAmount = targetAmount;
            
            if (valveHandle != null)
            {
                valveHandle.localRotation = opening ? openRotationQuat : closedRotation;
            }
            
            currentState = opening ? ValveState.Open : ValveState.Closed;
            UpdateVisuals();
            
            ReactorEvents.RaiseValveStateChanged(valveType, currentState);
            
            Debug.Log($"[ValveController] {valveType} valve is now {currentState}");
            
            transitionCoroutine = null;
        }

        // ====================================================================
        // VISUAL UPDATES
        // ====================================================================
        
        private void UpdateVisuals()
        {
            if (valveRenderer == null) return;
            
            // Lerp color based on open amount
            Color currentColor = Color.Lerp(closedColor, openColor, openAmount);
            
            valveRenderer.GetPropertyBlock(propertyBlock);
            propertyBlock.SetColor(colorPropertyName, currentColor);
            valveRenderer.SetPropertyBlock(propertyBlock);
        }

        // ====================================================================
        // EDITOR HELPERS
        // ====================================================================
        
        #if UNITY_EDITOR
        private void OnDrawGizmosSelected()
        {
            if (valveHandle == null) return;
            
            // Draw rotation arc
            Gizmos.color = Color.yellow;
            Vector3 worldAxis = valveHandle.TransformDirection(rotationAxis);
            
            Gizmos.DrawRay(valveHandle.position, worldAxis * 0.5f);
            
            // Draw closed and open positions
            Gizmos.color = closedColor;
            Gizmos.DrawWireSphere(valveHandle.position, 0.05f);
            
            if (!Application.isPlaying)
            {
                Gizmos.color = openColor;
                Vector3 openPos = valveHandle.position + valveHandle.TransformDirection(Vector3.forward) * 0.1f;
                Gizmos.DrawWireSphere(openPos, 0.03f);
            }
        }
        #endif
    }
}
