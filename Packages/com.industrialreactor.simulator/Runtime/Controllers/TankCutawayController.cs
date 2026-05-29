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
    /// Supports dual-material setup: Slot 0 = Metal (always visible on remaining half), Slot 1 = Cutaway shader
    /// </summary>
    [AddComponentMenu("Industrial Reactor/Tank Cutaway Controller")]
    public class TankCutawayController : MonoBehaviour
    {
        [Header("Cutaway Mode")]
        [SerializeField] private CutawayMode cutawayMode = CutawayMode.ShaderClipping;

        [Header("Renderer Settings")]
        [SerializeField] private Renderer tankRenderer;
        [Tooltip("Material slot for metal material (remains visible on cut half)")]
        [SerializeField] private int metalMaterialSlot = 0;
        [Tooltip("Material slot for cutaway shader")]
        [SerializeField] private int cutawayMaterialSlot = 1;

        [Header("Shader Clipping Settings")]
        [SerializeField] private Transform cutawayPlane;
        [SerializeField] private Vector3 clipDirection = Vector3.right;
        [SerializeField] private string clipPlaneProperty = "_ClipPlane";
        [SerializeField] private string clipDirProperty = "_ClipDir";
        [SerializeField] private string enableClipProperty = "_EnableClip";

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
        private MaterialPropertyBlock metalPropertyBlock;
        private Coroutine transitionCoroutine;
        private Vector4 clipPlaneVector;

        public bool IsCutawayActive => isCutawayActive;
        public float CutawayAmount => cutawayAmount;

        private void Awake()
        {
            propertyBlock = new MaterialPropertyBlock();
            metalPropertyBlock = new MaterialPropertyBlock();
            UpdateClipPlane();
        }

        private void Start()
        {
            if (!isCutawayActive) SetCutawayImmediate(false);
        }

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

        public void ToggleCutaway()
        {
            if (isCutawayActive) DeactivateCutaway();
            else ActivateCutaway();
        }

        private IEnumerator TransitionCutaway(bool activate)
        {
            float startAmount = cutawayAmount;
            float targetAmount = activate ? 1f : 0f;
            float elapsed = 0f;

            while (elapsed < transitionTime)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / transitionTime);
                t = t * t * (3f - 2f * t); // Smooth step
                cutawayAmount = Mathf.Lerp(startAmount, targetAmount, t);
                ApplyCutaway();
                yield return null;
            }

            cutawayAmount = targetAmount;
            isCutawayActive = activate;
            ApplyCutaway();
            transitionCoroutine = null;
        }

        private void SetCutawayImmediate(bool active)
        {
            isCutawayActive = active;
            cutawayAmount = active ? 1f : 0f;
            ApplyCutaway();
        }

        private void ApplyCutaway()
        {
            switch (cutawayMode)
            {
                case CutawayMode.ShaderClipping:
                    ApplyShaderClipping();
                    break;
                case CutawayMode.MeshHiding:
                    ApplyMeshHiding();
                    break;
                case CutawayMode.Combined:
                    ApplyShaderClipping();
                    ApplyMeshHiding();
                    break;
            }
        }

        private void ApplyShaderClipping()
        {
            if (tankRenderer == null) return;

            UpdateClipPlane();

            // Apply to cutaway material (slot 1) - this does the clipping
            tankRenderer.GetPropertyBlock(propertyBlock, cutawayMaterialSlot);
            
            Vector4 animatedPlane = clipPlaneVector;
            // When cutaway is off, push clip plane far away; when on, use actual position
            animatedPlane.w = Mathf.Lerp(1000f, clipPlaneVector.w, cutawayAmount);
            
            propertyBlock.SetVector(clipPlaneProperty, animatedPlane);
            propertyBlock.SetVector(clipDirProperty, new Vector4(clipDirection.x, clipDirection.y, clipDirection.z, 0f));
            propertyBlock.SetFloat(enableClipProperty, cutawayAmount > 0.01f ? 1f : 0f);
            
            tankRenderer.SetPropertyBlock(propertyBlock, cutawayMaterialSlot);

            // Apply INVERTED clip plane to metal material (slot 0)
            // This keeps the metal visible on the REMAINING (non-cut) half
            tankRenderer.GetPropertyBlock(metalPropertyBlock, metalMaterialSlot);
            
            // Invert the clip plane for metal - show what cutaway hides
            Vector4 invertedPlane = new Vector4(-animatedPlane.x, -animatedPlane.y, -animatedPlane.z, -animatedPlane.w);
            
            // For metal material, we want it to show on the side that ISN'T cut
            // When cutaway is active, metal shows on the remaining half
            metalPropertyBlock.SetVector(clipPlaneProperty, invertedPlane);
            metalPropertyBlock.SetVector(clipDirProperty, new Vector4(-clipDirection.x, -clipDirection.y, -clipDirection.z, 0f));
            metalPropertyBlock.SetFloat(enableClipProperty, cutawayAmount > 0.01f ? 1f : 0f);
            
            tankRenderer.SetPropertyBlock(metalPropertyBlock, metalMaterialSlot);
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

        /// <summary>
        /// Set clip direction at runtime
        /// </summary>
        public void SetClipDirection(Vector3 direction)
        {
            clipDirection = direction.normalized;
            UpdateClipPlane();
            if (isCutawayActive) ApplyCutaway();
        }

        /// <summary>
        /// Set cutaway plane transform at runtime
        /// </summary>
        public void SetCutawayPlane(Transform plane)
        {
            cutawayPlane = plane;
            UpdateClipPlane();
            if (isCutawayActive) ApplyCutaway();
        }

#if UNITY_EDITOR
        [ContextMenu("Test Cutaway Toggle")]
        private void TestCutawayToggle() => ToggleCutaway();

        [ContextMenu("Force Update Clip Plane")]
        private void ForceUpdateClipPlane()
        {
            UpdateClipPlane();
            ApplyCutaway();
        }
#endif
    }
}
