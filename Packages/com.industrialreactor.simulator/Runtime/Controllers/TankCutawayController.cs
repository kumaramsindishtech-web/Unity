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
    /// Works with the Tank Cutaway shader to clip part of the tank mesh.
    /// </summary>
    [AddComponentMenu("Industrial Reactor/Tank Cutaway Controller")]
    public class TankCutawayController : MonoBehaviour
    {
        [Header("Cutaway Mode")]
        [SerializeField] private CutawayMode cutawayMode = CutawayMode.ShaderClipping;

        [Header("Renderer Settings")]
        [SerializeField] private Renderer tankRenderer;
        [Tooltip("Material index for the cutaway material. Set to -1 to apply to all materials.")]
        [SerializeField] private int materialIndex = 0;

        [Header("Shader Clipping Settings")]
        [SerializeField] private Transform cutawayPlane;
        [SerializeField] private Vector3 clipDirection = Vector3.right;
        
        [Header("Shader Property Names")]
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
        private Coroutine transitionCoroutine;
        private Vector4 clipPlaneVector;
        private bool initialized = false;

        public bool IsCutawayActive => isCutawayActive;
        public float CutawayAmount => cutawayAmount;

        private void Awake()
        {
            Initialize();
        }
        
        private void Initialize()
        {
            if (initialized) return;
            propertyBlock = new MaterialPropertyBlock();
            UpdateClipPlane();
            initialized = true;
        }

        private void Start()
        {
            Initialize();
            // Always start with cutaway DISABLED - full tank visible
            DisableCutawayImmediate();
        }

        private void OnEnable()
        {
            Initialize();
            // Always disable cutaway when enabled - show full tank
            DisableCutawayImmediate();
        }

        /// <summary>
        /// Immediately disable cutaway - show full tank
        /// </summary>
        private void DisableCutawayImmediate()
        {
            isCutawayActive = false;
            cutawayAmount = 0f;
            
            // Disable clipping in shader
            if (tankRenderer != null && propertyBlock != null)
            {
                int matCount = tankRenderer.sharedMaterials.Length;
                for (int i = 0; i < matCount; i++)
                {
                    tankRenderer.GetPropertyBlock(propertyBlock, i);
                    propertyBlock.SetFloat(enableClipProperty, 0f);
                    propertyBlock.SetVector(clipPlaneProperty, new Vector4(0, 0, 0, 1000f)); // Far plane
                    tankRenderer.SetPropertyBlock(propertyBlock, i);
                }
            }
            
            // Hide interior, show front
            if (frontHalfMesh != null) frontHalfMesh.SetActive(true);
            if (interiorMesh != null) interiorMesh.SetActive(false);
        }

        public void ActivateCutaway()
        {
            if (!Application.isPlaying) return;
            if (isCutawayActive) return;
            
            if (transitionCoroutine != null) StopCoroutine(transitionCoroutine);
            
            if (smoothTransition) 
                transitionCoroutine = StartCoroutine(TransitionCutaway(true));
            else 
                SetCutawayImmediate(true);
        }

        public void DeactivateCutaway()
        {
            if (!Application.isPlaying) return;
            if (!isCutawayActive) return;
            
            if (transitionCoroutine != null) StopCoroutine(transitionCoroutine);
            
            if (smoothTransition) 
                transitionCoroutine = StartCoroutine(TransitionCutaway(false));
            else 
                SetCutawayImmediate(false);
        }

        public void ToggleCutaway()
        {
            if (isCutawayActive) 
                DeactivateCutaway();
            else 
                ActivateCutaway();
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
            if (tankRenderer == null || propertyBlock == null) return;

            int materialCount = tankRenderer.sharedMaterials.Length;
            if (materialCount == 0) return;

            UpdateClipPlane();

            // Animate the clip plane - when cutaway is off, push plane far away
            Vector4 animatedPlane = clipPlaneVector;
            animatedPlane.w = Mathf.Lerp(1000f, clipPlaneVector.w, cutawayAmount);
            
            Vector4 clipDirVector = new Vector4(clipDirection.x, clipDirection.y, clipDirection.z, 0f);
            float enableClip = cutawayAmount > 0.01f ? 1f : 0f;

            // Determine which materials to apply to
            int startIdx = 0;
            int endIdx = materialCount;
            
            if (materialIndex >= 0 && materialIndex < materialCount)
            {
                startIdx = materialIndex;
                endIdx = materialIndex + 1;
            }

            // Apply to materials
            for (int i = startIdx; i < endIdx; i++)
            {
                tankRenderer.GetPropertyBlock(propertyBlock, i);
                propertyBlock.SetVector(clipPlaneProperty, animatedPlane);
                propertyBlock.SetVector(clipDirProperty, clipDirVector);
                propertyBlock.SetFloat(enableClipProperty, enableClip);
                tankRenderer.SetPropertyBlock(propertyBlock, i);
            }
        }

        private void ApplyMeshHiding()
        {
            if (frontHalfMesh != null) 
                frontHalfMesh.SetActive(cutawayAmount < 0.5f);
            if (interiorMesh != null) 
                interiorMesh.SetActive(cutawayAmount > 0.5f);
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
                Vector3 worldClipDir = transform.TransformDirection(clipDirection).normalized;
                clipPlaneVector = new Vector4(worldClipDir.x, worldClipDir.y, worldClipDir.z, -Vector3.Dot(worldClipDir, center));
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
        private void OnValidate()
        {
            // Don't run in edit mode - causes issues
            if (!Application.isPlaying) return;
            
            if (tankRenderer != null)
            {
                Initialize();
                UpdateClipPlane();
                ApplyCutaway();
            }
        }

        [ContextMenu("Test Cutaway Toggle")]
        private void TestCutawayToggle()
        {
            if (Application.isPlaying)
            {
                ToggleCutaway();
            }
            else
            {
                Debug.Log("[TankCutaway] Cutaway toggle only works in Play Mode");
            }
        }

        [ContextMenu("Force Activate Cutaway (Play Mode Only)")]
        private void ForceActivate()
        {
            if (Application.isPlaying)
            {
                Initialize();
                isCutawayActive = true;
                cutawayAmount = 1f;
                UpdateClipPlane();
                ApplyCutaway();
            }
        }

        [ContextMenu("Force Deactivate Cutaway")]
        private void ForceDeactivate()
        {
            Initialize();
            DisableCutawayImmediate();
        }
        
        [ContextMenu("Log Material Info")]
        private void LogMaterialInfo()
        {
            if (tankRenderer == null)
            {
                Debug.Log("[TankCutaway] No renderer assigned.");
                return;
            }
            
            var materials = tankRenderer.sharedMaterials;
            Debug.Log($"[TankCutaway] {gameObject.name}: Renderer has {materials.Length} material(s). Material Index setting: {materialIndex}");
            for (int i = 0; i < materials.Length; i++)
            {
                Debug.Log($"  [{i}] {(materials[i] != null ? materials[i].name : "NULL")}");
            }
        }
#endif
    }
}
