#if UNITY_EDITOR
using Leap.Unity;

using System.Collections.Generic;

using UnityEditor;
using UnityEditor.PackageManager.UI;

using UnityEngine;

namespace Leap.Examples.URP
{
    [InitializeOnLoad]
    public static class SampleDependencies
    {
        const string SAMPLE_NAME = "URP Examples";                                      // The name of the sample this script relates to
        const string PACKAGE_NAME = "com.ultraleap.tracking";                           // The name of the package this sample is dependent on
        static readonly string[] DEPENDENCIES = new string[] { "Shared Example Assets [Required]" }; // The samples SAMPLE_NAME is dependent on

        static SampleDependencies()
        {
            if (SessionState.GetBool("editorStartupDelayedCalled", false) == false)
            {
                // Runs a delayed delegate which is fired when the editor finishes fully loading
                //      this allows for thecode to run on editor start, and when recompiling
                //      We use editorStartupDelayedCalled to only fire this on editor start, to avoid recompiling annoyance
                EditorApplication.delayCall += () =>
                {
                    SessionState.SetBool("editorStartupDelayedCalled", true);
                    CheckForSampleDependencies(true);
                };
            }
        }

        [UnityEditor.Callbacks.DidReloadScripts]
        private static void CreateAssetWhenReady()
        {
            EditorApplication.delayCall += () =>
            {
                CheckForSampleDependencies();
            };
        }

        static void CheckForSampleDependencies(bool isInitialSessionCall = false)
        {
            if (IsPackageAvailable(PACKAGE_NAME, out var packageInfo)) // Check the package exists so we can use package manage wizardry
            {
                if (CheckForAndImportDependecies(PACKAGE_NAME, packageInfo)) // Check if the dependencies have been imported already - if not, import them and tell the user
                {
                    AssetDatabase.Refresh(); // refresh the assets to make sure they imported completely
                }
            }
            else // Probably not using Package Manager (so using .unitypackage or worse)
            {
                if (isInitialSessionCall)
                {
                    // Once per session, check if we need to warn that some dependency names are not found as directories
                    FindDependenciesInAssets();
                }
            }
        }

        static bool CheckForAndImportDependecies(string packageName, UnityEditor.PackageManager.PackageInfo packageInfo)
        {
            bool sampleImported = false;

            IEnumerable<Sample> samples = Sample.FindByPackage(packageName, packageInfo.version);

            foreach (var sample in samples)
            {
                if (DEPENDENCIES.Contains(sample.displayName) && !sample.isImported)
                {
                    sample.Import();
                    Debug.Log("Ultraleap: " + sample.displayName + " was imported to " + sample.importPath + " as a dependency of " + SAMPLE_NAME);
                    sampleImported = true;
                }
            }

            return sampleImported;
        }

        static bool IsPackageAvailable(string packageName, out UnityEditor.PackageManager.PackageInfo packageInfo)
        {
            packageInfo = GetPackageInfo(packageName);

            if (packageInfo != null)
            {
                return true;
            }

            return false;
        }

        static UnityEditor.PackageManager.PackageInfo GetPackageInfo(string packageName)
        {
            var allPackages = UnityEditor.PackageManager.PackageInfo.GetAllRegisteredPackages();
            foreach (var package in allPackages)
            {
                if (package.name == packageName)
                {
                    return package;
                }
            }
            return null;
        }

        /// <summary>
        /// Check through the directory names of the Assets folder, looking for matches to our dependencies.
        /// If they don't match, warn the user that they may need to import them
        /// </summary>
        static void FindDependenciesInAssets()
        {
            var folders = AssetDatabase.GetSubFolders("Assets");

            foreach (var dependencyName in DEPENDENCIES)
            {
                bool found = false;

                foreach (var folder in folders)
                {
                    if (FindDependenciesInFolder(folder, dependencyName))
                    {
                        found = true;
                        break;
                    }
                }

                if (!found)
                {
                    Debug.LogWarning("Ultraleap: " + SAMPLE_NAME + " has a dependency on " + dependencyName + ". No directory name matches this. Ensure you have this imported as a dependency when using " + SAMPLE_NAME);
                }
            }
        }

        static bool FindDependenciesInFolder(string folder, string dependencyName)
        {
            string[] split = folder.Split('/');

            if (split[split.Length - 1] == dependencyName)
            {
                return true;
            }

            var folders = AssetDatabase.GetSubFolders(folder);
            foreach (var fld in folders)
            {
                if (FindDependenciesInFolder(fld, dependencyName))
                {
                    return true;
                }
            }

            return false;
        }
    }
}
#endif