// ============================================================================
// Industrial Reactor Simulator - Valve Controller
// Copyright (c) 2024 Industrial Reactor Simulator. MIT License.
// ============================================================================

using UnityEngine;
using System.Collections;

namespace IndustrialReactorSimulator
{
    /// <summary>
    /// Controls a valve component with smooth open/close transitions.
    /// </summary>
    [AddComponentMenu("Industrial Reactor/Valve Controller")]
    public class ValveController : MonoBehaviour
    {
        [Header("Valve Configuration")]
        [SerializeField] private ValveType valveType = ValveType.Inlet;
        [SerializeField] private Transform valveHandle;
        [SerializeField] private Vector3 rotationAxis = Vector3.up;
        [SerializeField] private float openRotation = 90f;
        [Range(0.1f, 5f)][SerializeField] private float transitionTime = 0.5f;

        [Header("Visual Feedback")]
        [SerializeField] private Renderer valveRenderer;
        [SerializeField] private string colorPropertyName = "_BaseColor";
        [SerializeField] private Color closedColor = Color.red;
        [SerializeField] private Color openColor = Color.green;

        [Header("Runtime State")]
        [SerializeField] private ValveState currentState = ValveState.Closed;
        [SerializeField][Range(0f, 1f)] private float openAmount = 0f;

        private Quaternion closedRotation;
        private Quaternion openRotationQuat;
        private Coroutine transitionCoroutine;
        private MaterialPropertyBlock propertyBlock;

        public ValveType ValveType => valveType;
        public ValveState CurrentState => currentState;
        public float OpenAmount => openAmount;
        public bool IsOpen => currentState == ValveState.Open;
        public bool IsClosed => currentState == ValveState.Closed;
        public bool IsTransitioning => currentState == ValveState.Opening || currentState == ValveState.Closing;

        private void Awake()
        {
            propertyBlock = new MaterialPropertyBlock();
            if (valveHandle != null)
            {
                closedRotation = valveHandle.localRotation;
                openRotationQuat = closedRotation * Quaternion.AngleAxis(openRotation, rotationAxis);
            }
        }

        private void Start() => UpdateVisuals();

        public void OpenValve()
        {
            if (currentState == ValveState.Open || currentState == ValveState.Opening) return;
            if (transitionCoroutine != null) StopCoroutine(transitionCoroutine);
            transitionCoroutine = StartCoroutine(TransitionValve(true));
        }

        public void CloseValve()
        {
            if (currentState == ValveState.Closed || currentState == ValveState.Closing) return;
            if (transitionCoroutine != null) StopCoroutine(transitionCoroutine);
            transitionCoroutine = StartCoroutine(TransitionValve(false));
        }

        public void ToggleValve() { if (IsClosed || currentState == ValveState.Closing) OpenValve(); else CloseValve(); }

        public void ResetValve()
        {
            if (transitionCoroutine != null) { StopCoroutine(transitionCoroutine); transitionCoroutine = null; }
            openAmount = 0f;
            currentState = ValveState.Closed;
            if (valveHandle != null) valveHandle.localRotation = closedRotation;
            UpdateVisuals();
            ReactorEvents.RaiseValveStateChanged(valveType, currentState);
        }

        public void SetOpenAmount(float amount)
        {
            openAmount = Mathf.Clamp01(amount);
            if (valveHandle != null) valveHandle.localRotation = Quaternion.Slerp(closedRotation, openRotationQuat, openAmount);
            UpdateVisuals();
        }

        private IEnumerator TransitionValve(bool opening)
        {
            currentState = opening ? ValveState.Opening : ValveState.Closing;
            ReactorEvents.RaiseValveStateChanged(valveType, currentState);

            float startAmount = openAmount;
            float targetAmount = opening ? 1f : 0f;
            float elapsed = 0f;
            float duration = transitionTime * Mathf.Abs(targetAmount - startAmount);

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                t = t * t * (3f - 2f * t);
                openAmount = Mathf.Lerp(startAmount, targetAmount, t);
                if (valveHandle != null) valveHandle.localRotation = Quaternion.Slerp(closedRotation, openRotationQuat, openAmount);
                UpdateVisuals();
                yield return null;
            }

            openAmount = targetAmount;
            if (valveHandle != null) valveHandle.localRotation = opening ? openRotationQuat : closedRotation;
            currentState = opening ? ValveState.Open : ValveState.Closed;
            UpdateVisuals();
            ReactorEvents.RaiseValveStateChanged(valveType, currentState);
            transitionCoroutine = null;
        }

        private void UpdateVisuals()
        {
            if (valveRenderer == null) return;
            Color currentColor = Color.Lerp(closedColor, openColor, openAmount);
            valveRenderer.GetPropertyBlock(propertyBlock);
            propertyBlock.SetColor(colorPropertyName, currentColor);
            valveRenderer.SetPropertyBlock(propertyBlock);
        }
    }
}
