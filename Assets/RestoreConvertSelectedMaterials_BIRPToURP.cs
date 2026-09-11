// Restore "Convert Selected Built-in Materials to URP" MenuItem.
// By Andy Miira (Andrei Müller, Twin Koryuu), March 2026.
//
//
// Note: This script was tested to work correctly in:
// - Unity 6.3 LTS (6000.3.11f1)
// - Unity 6.4     (6000.4.0f1)
//
// The script should also work correctly with other Unity versions, at least ones closer to the tested versions above.
// Note: I will NOT provide support to Unity versions that I don't use.
//
//
// [Instructions]
// - Initial setup: Add this script inside an "Editor" folder in your project.
// You must create this folder if it doesn't already exist.
//
//
// - Method 1: Select ONE OR MORE materials in the Project View, then click on
// "Edit -> Rendering -> Materials -> Convert Selected Built-in Materials to URP" at the top menu bar. That's it!
//
// Note: This method also works if you select folders containing materials.
//
//
// - Method 2: Select ONE OR MORE materials in the Project View, right-click to open the context menu, then click on
// "Tools -> Materials -> Convert Selected Built-in Materials to URP". That's it!
//
// Note: This method also works if you select folders containing materials.
//
//
// - Method 3: Select ONE OR MORE materials in the Project View, then click on
// the "three dots" icon in the top-right corner of the material Inspector.
// Or simply right-click the top area of the material Inspector
// (where it displays the material's name or "X Materials", the shader selection dropdown, etc.).
//
// In the context menu, there should be a new option, "Convert Selected Built-in Materials to URP".
// Click this option to convert the currently selected materials. That's it!
//
//
// Check out more Unity scripts and utilities at:
// https://gist.github.com/andreiagmu

using System.Collections.Generic;
using UnityEditor;
using UnityEditor.Rendering;
using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace TwinKoryuu.Rendering.Universal
{
    public static class RestoreConvertSelectedMaterials_BIRPToURP
    {
        private static List<MaterialUpgrader> m_Upgraders;

        // Prevent multiple context invocations (one per selected material) from running repeatedly.
        private static bool _contextQueued;

        private const string MenuItemName = "Convert Selected Built-in Materials to URP";
        private const string MenuPath = "Edit/Rendering/Materials/";
        private const string ProjectContextPath = "Assets/Tools/Materials/";
        private const string MaterialContextPath = "CONTEXT/Material/";

        static RestoreConvertSelectedMaterials_BIRPToURP()
        {
            m_Upgraders = MaterialUpgrader.FetchAllUpgradersForPipeline(typeof(UniversalRenderPipelineAsset));
        }

        // Top menu.
        [MenuItem(MenuPath + MenuItemName, priority = 200)]
        private static void ConvertSelectedMaterials_Menu()
        {
            ConvertSelectedMaterials_Internal();
        }

        // Project View context menu.
        [MenuItem(ProjectContextPath + MenuItemName, priority = 1500)]
        private static void ConvertSelectedMaterials_ProjectContext()
        {
            ConvertSelectedMaterials_Internal();
        }

        // Inspector context menu. Note: Unity calls this once PER selected material.
        [MenuItem(MaterialContextPath + MenuItemName, priority = 550)]
        private static void ConvertSelectedMaterials_MaterialContext(MenuCommand command)
        {
            if (_contextQueued)
                return;

            _contextQueued = true;

            // Run once on the next editor tick; all per-object context calls will happen before this.
            EditorApplication.delayCall += () =>
            {
                try
                {
                    ConvertSelectedMaterials_Internal();
                }
                finally
                {
                    _contextQueued = false;
                }
            };
        }

        private static void ConvertSelectedMaterials_Internal()
        {
            var selectedMaterials = Selection.GetFiltered<Material>(SelectionMode.DeepAssets);

            if (selectedMaterials.Length == 0)
            {
                Debug.LogWarning("No materials selected for conversion.");
                return;
            }

            var proceed = EditorUtility.DisplayDialog("Convert Materials",
                $"Do you want to convert {selectedMaterials.Length} selected materials to URP?\n\n" +
                "This action is irreversible.", "OK", "Cancel");

            if (!proceed)
                return;

            foreach (var mat in selectedMaterials)
            {
                if (mat == null)
                    continue;

                MaterialUpgrader.Upgrade(mat, m_Upgraders, MaterialUpgrader.UpgradeFlags.LogMessageWhenNoUpgraderFound);
                AssetDatabase.SaveAssetIfDirty(mat);

                Debug.Log($"Material Upgrade: {mat.name} converted from BIRP to URP.");
            }

            Debug.Log($"Material Upgrade: {selectedMaterials.Length} materials converted from BIRP to URP.");
        }

        // Validate for top menu.
        [MenuItem(MenuPath + MenuItemName, validate = true)]
        private static bool ConvertSelectedMaterials_MenuValidate()
        {
            return IsAnyMaterialSelected();
        }

        // Validate for Project View context menu.
        [MenuItem(ProjectContextPath + MenuItemName, validate = true)]
        private static bool ConvertSelectedMaterials_ProjectContextValidate()
        {
            return IsAnyMaterialSelected();
        }

        // Validate for Inspector context menu.
        [MenuItem(MaterialContextPath + MenuItemName, validate = true)]
        private static bool ConvertSelectedMaterials_MaterialContextValidate(MenuCommand command)
        {
            return IsAnyMaterialSelected();
        }

        private static bool IsAnyMaterialSelected()
        {
            return Selection.GetFiltered<Material>(SelectionMode.DeepAssets).Length > 0;
        }
    }
}
