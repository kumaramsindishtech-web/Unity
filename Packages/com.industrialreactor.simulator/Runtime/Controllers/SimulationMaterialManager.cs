// ============================================================================
// Industrial Reactor Simulator - Simulation Material Manager
// Copyright (c) 2024 Industrial Reactor Simulator. MIT License.
// ============================================================================

using UnityEngine;
using System;
using System.Collections.Generic;

namespace IndustrialReactorSimulator
{
    /// <summary>
    /// Manages material switching between Editor mode (original metal materials) 
    /// and Play mode (simulation shaders).
    /// Attach to any object that needs material switching during simulation.
    /// </summary>
    [AddComponentMenu("Industrial Reactor/Simulation Material Manager")]
    [ExecuteInEditMode]
    public class SimulationMaterialManager : MonoBehaviour
    {
        [Serializable]
        public class MaterialSlotConfig
        {
            [Tooltip("The renderer to manage")]
            public Renderer targetRenderer;
            
            [Tooltip("Material slot index (0 = first material)")]
            public int materialSlot = 0;
            
            [Tooltip("Original material shown in Editor mode (e.g., metal material)")]
            public Material editorMaterial;
            
            [Tooltip("Simulation material shown in Play mode (e.g., cutaway/flow shader)")]
            public Material simulationMaterial;
            
            [Tooltip("Description for this slot")]
            public string description = "";
        }

        [Header("Material Configurations")]
        [SerializeField] private List<MaterialSlotConfig> materialConfigs = new List<MaterialSlotConfig>();

        [Header("Runtime State")]
        [SerializeField] private bool isSimulationMode = false;
        [SerializeField] private bool autoSwitchOnPlay = true;

        private Dictionary<Renderer, Material[]> originalMaterials = new Dictionary<Renderer, Material[]>();
        private bool materialsBackedUp = false;

        public bool IsSimulationMode => isSimulationMode;

        private void OnEnable()
        {
            // In Editor mode, ensure original materials are shown
            if (!Application.isPlaying)
            {
                ApplyEditorMaterials();
            }
        }

        private void Start()
        {
            if (Application.isPlaying)
            {
                BackupOriginalMaterials();
                
                if (autoSwitchOnPlay)
                {
                    // Don't switch immediately - wait for simulation to start
                    // Materials will be switched when ActivateSimulationMaterials() is called
                    ApplyEditorMaterials();
                }
            }
        }

        private void OnDestroy()
        {
            // Restore original materials when destroyed
            if (Application.isPlaying && materialsBackedUp)
            {
                RestoreOriginalMaterials();
            }
        }

        private void OnApplicationQuit()
        {
            // Ensure materials are restored on quit
            RestoreOriginalMaterials();
        }

        /// <summary>
        /// Backup original materials before any changes
        /// </summary>
        private void BackupOriginalMaterials()
        {
            if (materialsBackedUp) return;

            originalMaterials.Clear();
            foreach (var config in materialConfigs)
            {
                if (config.targetRenderer != null && !originalMaterials.ContainsKey(config.targetRenderer))
                {
                    // Clone the materials array
                    originalMaterials[config.targetRenderer] = config.targetRenderer.sharedMaterials.Clone() as Material[];
                }
            }
            materialsBackedUp = true;
        }

        /// <summary>
        /// Restore original materials from backup
        /// </summary>
        private void RestoreOriginalMaterials()
        {
            foreach (var kvp in originalMaterials)
            {
                if (kvp.Key != null)
                {
                    kvp.Key.sharedMaterials = kvp.Value;
                }
            }
        }

        /// <summary>
        /// Apply editor materials (original metal materials)
        /// Called in Editor mode and at start of Play mode
        /// </summary>
        public void ApplyEditorMaterials()
        {
            foreach (var config in materialConfigs)
            {
                if (config.targetRenderer == null || config.editorMaterial == null) continue;

                Material[] materials = config.targetRenderer.sharedMaterials;
                if (config.materialSlot < materials.Length)
                {
                    materials[config.materialSlot] = config.editorMaterial;
                    config.targetRenderer.sharedMaterials = materials;
                }
            }
            isSimulationMode = false;
        }

        /// <summary>
        /// Apply simulation materials (cutaway/flow shaders)
        /// Called when simulation starts
        /// </summary>
        public void ApplySimulationMaterials()
        {
            if (!Application.isPlaying)
            {
                Debug.LogWarning("[SimulationMaterialManager] Simulation materials can only be applied in Play mode.");
                return;
            }

            foreach (var config in materialConfigs)
            {
                if (config.targetRenderer == null || config.simulationMaterial == null) continue;

                Material[] materials = config.targetRenderer.sharedMaterials;
                if (config.materialSlot < materials.Length)
                {
                    materials[config.materialSlot] = config.simulationMaterial;
                    config.targetRenderer.sharedMaterials = materials;
                }
            }
            isSimulationMode = true;
        }

        /// <summary>
        /// Activate simulation mode - switches to simulation materials
        /// </summary>
        public void ActivateSimulationMode()
        {
            if (!Application.isPlaying) return;
            ApplySimulationMaterials();
        }

        /// <summary>
        /// Deactivate simulation mode - switches back to editor materials
        /// </summary>
        public void DeactivateSimulationMode()
        {
            if (!Application.isPlaying) return;
            ApplyEditorMaterials();
        }

        /// <summary>
        /// Toggle between editor and simulation materials
        /// </summary>
        public void ToggleMaterials()
        {
            if (isSimulationMode)
                DeactivateSimulationMode();
            else
                ActivateSimulationMode();
        }

        /// <summary>
        /// Get the current material for a specific renderer and slot
        /// </summary>
        public Material GetCurrentMaterial(Renderer renderer, int slot)
        {
            if (renderer == null) return null;
            Material[] materials = renderer.sharedMaterials;
            return slot < materials.Length ? materials[slot] : null;
        }

        /// <summary>
        /// Add a new material configuration at runtime
        /// </summary>
        public void AddMaterialConfig(Renderer renderer, int slot, Material editorMat, Material simMat, string desc = "")
        {
            var config = new MaterialSlotConfig
            {
                targetRenderer = renderer,
                materialSlot = slot,
                editorMaterial = editorMat,
                simulationMaterial = simMat,
                description = desc
            };
            materialConfigs.Add(config);

            // Backup if in play mode
            if (Application.isPlaying && !originalMaterials.ContainsKey(renderer))
            {
                originalMaterials[renderer] = renderer.sharedMaterials.Clone() as Material[];
            }
        }

#if UNITY_EDITOR
        [ContextMenu("Apply Editor Materials")]
        private void EditorApplyEditorMaterials() => ApplyEditorMaterials();

        [ContextMenu("Preview Simulation Materials")]
        private void EditorPreviewSimulationMaterials()
        {
            // Temporary preview in editor - will revert on selection change
            foreach (var config in materialConfigs)
            {
                if (config.targetRenderer == null || config.simulationMaterial == null) continue;
                Material[] materials = config.targetRenderer.sharedMaterials;
                if (config.materialSlot < materials.Length)
                {
                    materials[config.materialSlot] = config.simulationMaterial;
                    config.targetRenderer.sharedMaterials = materials;
                }
            }
        }

        [ContextMenu("Auto-Configure from Children")]
        private void AutoConfigureFromChildren()
        {
            materialConfigs.Clear();
            Renderer[] renderers = GetComponentsInChildren<Renderer>();
            
            foreach (var renderer in renderers)
            {
                Material[] mats = renderer.sharedMaterials;
                for (int i = 0; i < mats.Length; i++)
                {
                    var config = new MaterialSlotConfig
                    {
                        targetRenderer = renderer,
                        materialSlot = i,
                        editorMaterial = mats[i],
                        simulationMaterial = null, // User needs to assign
                        description = $"{renderer.gameObject.name} - Slot {i}"
                    };
                    materialConfigs.Add(config);
                }
            }
            UnityEditor.EditorUtility.SetDirty(this);
        }
#endif
    }
}
