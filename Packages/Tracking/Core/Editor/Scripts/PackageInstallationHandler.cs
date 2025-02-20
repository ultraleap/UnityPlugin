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
                _materialUpdater = Resources.Load<AutomaticRenderPipelineMaterialShaderUpdater>("AutomaticRenderPipelineMaterialShaderUpdater");
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
