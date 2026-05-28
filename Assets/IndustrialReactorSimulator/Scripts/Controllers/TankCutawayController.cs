// ============================================================================
// Industrial Reactor Simulator - Tank Cutaway Controller
// Version: 1.0.0
// Description: Controls tank cutaway view for interior visibility
// ============================================================================

using UnityEngine;
using System.Collections;
using IndustrialReactorSimulator.Core;

namespace IndustrialReactorSimulator.Controllers
{
    /// <summary>
    /// Controls the tank cutaway effect for visualizing interior.
    /// Can use shader-based clipping or mesh hiding approach.
    /// </summary>
    [AddComponentMenu("Industrial Reactor/Tank Cutaway Controller")]
    public class TankCutawayController : MonoBehaviour
    {
        // ====================================================================
        // CONFIGURATION
        // ====================================================================
        
        [Header("Cutaway Mode")]
        [Tooltip("Method for creating cutaway effect")]
        [SerializeField] private CutawayMode cutawayMode = CutawayMode.ShaderClipping;

        [Header("Shader Clipping Settings")]
        [Tooltip("Renderer with cutaway shader")]
        [SerializeField] private Renderer tankRenderer;
        
        [Tooltip("Cutaway plane position in world space")]
        [SerializeField] private Transform cutawayPlane;
        
        [Tooltip("Clipping direction (local space)")]
        [SerializeField] private Vector3 clipDirection = Vector3.right;
        
        [Tooltip("Shader property for clip plane position")]
        [SerializeField] private string clipPlaneProperty = "_ClipPlane";
        
        [Tooltip("Shader property for clip direction")]
        [SerializeField] private string clipDirProperty = "_ClipDir";


        [Header("Mesh Hiding Settings")]
        [Tooltip("Front half mesh to hide for cutaway")]
        [SerializeField] private GameObject frontHalfMesh;
        
        [Tooltip("Interior mesh to show when cutaway active")]
        [SerializeField] private GameObject interiorMesh;

        [Header("Transition Settings")]
        [Tooltip("Time for cutaway transition")]
        [Range(0.1f, 2f)]
        [SerializeField] private float transitionTime = 0.5f;
        
        [Tooltip("Use smooth transition animation")]
        [SerializeField] private bool smoothTransition = true;

        // ====================================================================
        // RUNTIME STATE
        // ====================================================================
        
        [Header("Runtime State (Read-Only)")]
        [SerializeField] private bool isCutawayActive = false;
        [SerializeField] [Range(0f, 1f)] private float cutawayAmount = 0f;
        
        private MaterialPropertyBlock propertyBlock;
        private Coroutine transitionCoroutine;
        private Vector4 clipPlaneVector;

        // ====================================================================
        // ENUMS
        // ====================================================================
        
        public enum CutawayMode
        {
            ShaderClipping,     // Use shader to clip geometry
            MeshHiding,         // Hide front mesh, show interior
            Combined            // Both methods
        }

        // ====================================================================
        // PROPERTIES
        // ====================================================================
        
        public bool IsCutawayActive => isCutawayActive;
        public float CutawayAmount => cutawayAmount;



        // ====================================================================
        // UNITY LIFECYCLE
        // ====================================================================
        
        private void Awake()
        {
            propertyBlock = new MaterialPropertyBlock();
            
            // Calculate initial clip plane
            UpdateClipPlane();
        }

        private void Start()
        {
            // Ensure initial state
            if (!isCutawayActive)
            {
                SetCutawayImmediate(false);
            }
        }

        // ====================================================================
        // PUBLIC METHODS
        // ====================================================================
        
        /// <summary>
        /// Activate cutaway view with transition
        /// </summary>
        public void ActivateCutaway()
        {
            if (isCutawayActive) return;
            
            Debug.Log("[TankCutaway] Activating cutaway view...");
            
            if (transitionCoroutine != null)
            {
                StopCoroutine(transitionCoroutine);
            }
            
            if (smoothTransition)
            {
                transitionCoroutine = StartCoroutine(TransitionCutaway(true));
            }
            else
            {
                SetCutawayImmediate(true);
            }
        }

        /// <summary>
        /// Deactivate cutaway view with transition
        /// </summary>
        public void DeactivateCutaway()
        {
            if (!isCutawayActive) return;
            
            Debug.Log("[TankCutaway] Deactivating cutaway view...");
            
            if (transitionCoroutine != null)
            {
                StopCoroutine(transitionCoroutine);
            }
            
            if (smoothTransition)
            {
                transitionCoroutine = StartCoroutine(TransitionCutaway(false));
            }
            else
            {
                SetCutawayImmediate(false);
            }
        }

        /// <summary>
        /// Toggle cutaway view
        /// </summary>
        public void ToggleCutaway()
        {
            if (isCutawayActive)
                DeactivateCutaway();
            else
                ActivateCutaway();
        }



        // ====================================================================
        // TRANSITION
        // ====================================================================
        
        private IEnumerator TransitionCutaway(bool activate)
        {
            float startAmount = cutawayAmount;
            float targetAmount = activate ? 1f : 0f;
            float elapsed = 0f;

            while (elapsed < transitionTime)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / transitionTime);
                t = t * t * (3f - 2f * t); // Smoothstep
                
                cutawayAmount = Mathf.Lerp(startAmount, targetAmount, t);
                ApplyCutaway();
                
                yield return null;
            }

            cutawayAmount = targetAmount;
            isCutawayActive = activate;
            ApplyCutaway();
            
            transitionCoroutine = null;
            
            Debug.Log($"[TankCutaway] Cutaway {(activate ? "activated" : "deactivated")}");
        }

        private void SetCutawayImmediate(bool active)
        {
            isCutawayActive = active;
            cutawayAmount = active ? 1f : 0f;
            ApplyCutaway();
        }

        // ====================================================================
        // CUTAWAY APPLICATION
        // ====================================================================
        
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
            
            tankRenderer.GetPropertyBlock(propertyBlock);
            
            // Animate clip plane position based on cutaway amount
            Vector4 animatedPlane = clipPlaneVector;
            animatedPlane.w = Mathf.Lerp(1000f, clipPlaneVector.w, cutawayAmount);
            
            propertyBlock.SetVector(clipPlaneProperty, animatedPlane);
            propertyBlock.SetVector(clipDirProperty, new Vector4(clipDirection.x, clipDirection.y, clipDirection.z, 0f));
            
            tankRenderer.SetPropertyBlock(propertyBlock);
        }



        private void ApplyMeshHiding()
        {
            // Hide front mesh when cutaway is active
            if (frontHalfMesh != null)
            {
                frontHalfMesh.SetActive(cutawayAmount < 0.5f);
            }
            
            // Show interior when cutaway is active
            if (interiorMesh != null)
            {
                interiorMesh.SetActive(cutawayAmount > 0.5f);
            }
        }

        private void UpdateClipPlane()
        {
            if (cutawayPlane != null)
            {
                Vector3 planePos = cutawayPlane.position;
                Vector3 planeNormal = cutawayPlane.TransformDirection(clipDirection).normalized;
                
                clipPlaneVector = new Vector4(
                    planeNormal.x,
                    planeNormal.y,
                    planeNormal.z,
                    -Vector3.Dot(planeNormal, planePos)
                );
            }
            else
            {
                // Default clip plane at object center
                Vector3 center = transform.position;
                clipPlaneVector = new Vector4(
                    clipDirection.x,
                    clipDirection.y,
                    clipDirection.z,
                    -Vector3.Dot(clipDirection, center)
                );
            }
        }

        // ====================================================================
        // EDITOR HELPERS
        // ====================================================================
        
        #if UNITY_EDITOR
        [ContextMenu("Toggle Cutaway")]
        private void EditorToggleCutaway()
        {
            ToggleCutaway();
        }

        private void OnDrawGizmosSelected()
        {
            // Draw clip plane
            Gizmos.color = new Color(1f, 0.5f, 0f, 0.5f);
            
            Vector3 planePos = cutawayPlane != null ? cutawayPlane.position : transform.position;
            Vector3 planeNormal = cutawayPlane != null 
                ? cutawayPlane.TransformDirection(clipDirection) 
                : clipDirection;
            
            // Draw plane normal
            Gizmos.DrawRay(planePos, planeNormal * 0.5f);
            
            // Draw plane outline
            UnityEditor.Handles.color = new Color(1f, 0.5f, 0f, 0.3f);
            UnityEditor.Handles.DrawSolidDisc(planePos, planeNormal, 0.5f);
        }
        #endif
    }
}
