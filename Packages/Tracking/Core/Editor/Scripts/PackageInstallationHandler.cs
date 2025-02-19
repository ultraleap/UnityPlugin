using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

[ExecuteInEditMode]
[CreateAssetMenu(fileName = "Package Installation Handler", menuName = "Ultraleap/PackageInstallationHandler", order = 1)]
public class PackageInstallationHandler : ScriptableObject
{
    [SerializeField]
    private AutomaticRenderPipelineMaterialShaderUpdater _materialUpdater;

    private void Awake()
    {
        UnityEditor.PackageManager.Events.registeredPackages += Events_registeredPackages;    
    }

    private void Events_registeredPackages(UnityEditor.PackageManager.PackageRegistrationEventArgs obj)
    {
        if (obj != null)
        {
            if (obj.added.Any(pi => pi.displayName == "Ultraleap Tracking" || pi.displayName == "Ultraleap Tracking Preview"))
            {
                // Reset some settings related to upgrade of materials based on the render pipeline
                _materialUpdater.PromptUserToConfirmConversion = true;
                _materialUpdater.NumberOfTimesUserRejectedPrompt = 0;
                _materialUpdater.AutomaticConversionIsOffForPluginInProject = false;
            }     
        }
    }
}
