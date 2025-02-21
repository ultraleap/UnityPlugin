using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Class to perform any plugin initialization logic when a Ultraleap Unity Package is first loaded
/// </summary>
[InitializeOnLoad]
[ExecuteInEditMode]
public static class PackageInstallationHandler 
{
    private static AutomaticRenderPipelineMaterialShaderUpdater _materialUpdater;
    private static bool _isInitialized = false;

    [InitializeOnLoadMethod]
    public static void RegisterForPackageLoad()
    {   
        if (!_isInitialized)
        {
            UnityEditor.PackageManager.Events.registeredPackages += Events_registeredPackages;
            _isInitialized = true;  
        }
    }

    private static void Events_registeredPackages(UnityEditor.PackageManager.PackageRegistrationEventArgs obj)
    {
        if (obj != null)
        {
            if (obj.added.Any(pi => pi.displayName == "Ultraleap Tracking" || pi.displayName == "Ultraleap Tracking Preview"))
            {
                Debug.Log($"An Ultraleap Package was just installed");
                UpgradeMaterialsInProject();
            }     
        }
    }

    public static void UpgradeMaterialsInProject()
    {
        if (_materialUpdater == null)
        {
            try
            {
                // Note, loading the instance will cause Enable to be called, if the AutomaticRenderPipelineMaterialShaderUpdater scriptable object instance
                // has been serialized with the PromptUserToConfirmConversion flag set to false (e.g. in the plugin release or source) then the
                // user won't be prompted about the render pipeline conversion, that's not the behaviour we want. The plugin source should be fixed.
                _materialUpdater = Resources.Load<AutomaticRenderPipelineMaterialShaderUpdater>("AutomaticRenderPipelineMaterialShaderUpdater");

                if (_materialUpdater?.PromptUserToConfirmConversion == false)
                {
                    Debug.LogWarning($"The Ultraleap Unity plugin Resources contains a serialized instance of the AutomaticRenderPipelineMaterialShaderUpdater ScriptableObject " +
                        $"where PromptUserToConfirmConversion has been set to false. This is undesirable (considered a bug) and the asset should be recommitted with the flag set to true. " +
                        $"The setting is visible in the Inspector properties.");
                }
            }
            catch (Exception e)
            {
                Debug.LogException(e);
            }
        }

        if (_materialUpdater != null)
        {
            // Reset some settings related to upgrade of materials based on the render pipeline
            _materialUpdater.PromptUserToConfirmConversion = true;
            _materialUpdater.NumberOfTimesUserRejectedPrompt = 0;
            _materialUpdater.AutomaticConversionIsOffForPluginInProject = false;

            _materialUpdater.AutoRefreshMaterialShadersForPipeline();
        }
    }
}
