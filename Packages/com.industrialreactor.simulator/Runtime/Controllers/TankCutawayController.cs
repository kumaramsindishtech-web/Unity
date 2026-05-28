// ============================================================================
// Industrial Reactor Simulator - Tank Cutaway Controller
// Copyright (c) 2024 Industrial Reactor Simulator. MIT License.
// ============================================================================

using UnityEngine;
using System.Collections;

namespace IndustrialReactorSimulator
{
    /// <summary>
    /// Controls the tank cutaway effect for visualizing interior.
    /// </summary>
    [AddComponentMenu("Industrial Reactor/Tank Cutaway Controller")]
    public class TankCutawayController : MonoBehaviour
    {
        [Header("Cutaway Mode")]
        [SerializeField] private CutawayMode cutawayMode = CutawayMode.ShaderClipping;

        [Header("Shader Clipping Settings")]
        [SerializeField] private Renderer tankRenderer;
        [SerializeField] private Transform cutawayPlane;
        [SerializeField] private Vector3 clipDirection = Vector3.right;
        [SerializeField] private string clipPlaneProperty = "_ClipPlane";
        [SerializeField] private string clipDirProperty = "_ClipDir";

        [Header("Mesh Hiding Settings")]
        [SerializeField] private GameObject frontHalfMesh;
        [SerializeField] private GameObject interiorMesh;

        [Header("Transition Settings")]
        [Range(0.1f, 2f)][SerializeField] private float transitionTime = 0.5f;
        [SerializeField] private bool smoothTransition = true;

        [Header("Runtime State")]
        [SerializeField] private bool isCutawayActive = false;
        [SerializeField][Range(0f, 1f)] private float cutawayAmount = 0f;

        private MaterialPropertyBlock propertyBlock;
        private Coroutine transitionCoroutine;
        private Vector4 clipPlaneVector;

        public bool IsCutawayActive => isCutawayActive;
        public float CutawayAmount => cutawayAmount;

        private void Awake() { propertyBlock = new MaterialPropertyBlock(); UpdateClipPlane(); }
        private void Start() { if (!isCutawayActive) SetCutawayImmediate(false); }

        public void ActivateCutaway()
        {
            if (isCutawayActive) return;
            if (transitionCoroutine != null) StopCoroutine(transitionCoroutine);
            if (smoothTransition) transitionCoroutine = StartCoroutine(TransitionCutaway(true));
            else SetCutawayImmediate(true);
        }

        public void DeactivateCutaway()
        {
            if (!isCutawayActive) return;
            if (transitionCoroutine != null) StopCoroutine(transitionCoroutine);
            if (smoothTransition) transitionCoroutine = StartCoroutine(TransitionCutaway(false));
            else SetCutawayImmediate(false);
        }

        public void ToggleCutaway() { if (isCutawayActive) DeactivateCutaway(); else ActivateCutaway(); }

        private IEnumerator TransitionCutaway(bool activate)
        {
            float startAmount = cutawayAmount;
            float targetAmount = activate ? 1f : 0f;
            float elapsed = 0f;

            while (elapsed < transitionTime)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / transitionTime);
                t = t * t * (3f - 2f * t);
                cutawayAmount = Mathf.Lerp(startAmount, targetAmount, t);
                ApplyCutaway();
                yield return null;
            }

            cutawayAmount = targetAmount;
            isCutawayActive = activate;
            ApplyCutaway();
            transitionCoroutine = null;
        }

        private void SetCutawayImmediate(bool active) { isCutawayActive = active; cutawayAmount = active ? 1f : 0f; ApplyCutaway(); }

        private void ApplyCutaway()
        {
            switch (cutawayMode)
            {
                case CutawayMode.ShaderClipping: ApplyShaderClipping(); break;
                case CutawayMode.MeshHiding: ApplyMeshHiding(); break;
                case CutawayMode.Combined: ApplyShaderClipping(); ApplyMeshHiding(); break;
            }
        }

        private void ApplyShaderClipping()
        {
            if (tankRenderer == null) return;
            UpdateClipPlane();
            tankRenderer.GetPropertyBlock(propertyBlock);
            Vector4 animatedPlane = clipPlaneVector;
            animatedPlane.w = Mathf.Lerp(1000f, clipPlaneVector.w, cutawayAmount);
            propertyBlock.SetVector(clipPlaneProperty, animatedPlane);
            propertyBlock.SetVector(clipDirProperty, new Vector4(clipDirection.x, clipDirection.y, clipDirection.z, 0f));
            tankRenderer.SetPropertyBlock(propertyBlock);
        }

        private void ApplyMeshHiding()
        {
            if (frontHalfMesh != null) frontHalfMesh.SetActive(cutawayAmount < 0.5f);
            if (interiorMesh != null) interiorMesh.SetActive(cutawayAmount > 0.5f);
        }

        private void UpdateClipPlane()
        {
            if (cutawayPlane != null)
            {
                Vector3 planePos = cutawayPlane.position;
                Vector3 planeNormal = cutawayPlane.TransformDirection(clipDirection).normalized;
                clipPlaneVector = new Vector4(planeNormal.x, planeNormal.y, planeNormal.z, -Vector3.Dot(planeNormal, planePos));
            }
            else
            {
                Vector3 center = transform.position;
                clipPlaneVector = new Vector4(clipDirection.x, clipDirection.y, clipDirection.z, -Vector3.Dot(clipDirection, center));
            }
        }
    }
}
