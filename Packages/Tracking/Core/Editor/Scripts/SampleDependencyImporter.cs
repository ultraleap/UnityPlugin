#if UNITY_EDITOR
using Leap;

using System.Collections.Generic;

using UnityEditor;
using UnityEditor.PackageManager.UI;

using UnityEngine;

namespace Leap.Examples
{
    public static class SampleDependencyImporter
    {
        /// <summary>
        /// Find if there are any missing sample dependencies and import them if they do not exist
        ///  Note: Only use this tool to import samples from the same package.
        /// </summary>
        /// <param name="packageName">The package that has samples</param>
        /// <param name="sampleName">The sample that has dependencies on other samples</param>
        /// <param name="dependencyNames">The names of each of the samples that sampleName is dependent on</param>
        public static void FindAndImportSampleDependencies(string packageName, string sampleName, string[] dependencyNames)
        {
            if (IsPackageAvailable(packageName, out var packageInfo)) // Check the package exists so we can use package manage wizardry
            {
                if (CheckForAndImportDependecies(packageName, sampleName, dependencyNames, packageInfo)) // Check if the dependencies have been imported already - if not, import them and tell the user
                {
                    AssetDatabase.Refresh(); // refresh the assets to make sure they imported completely
                }
            }
            else // Probably not using Package Manager (so using .unitypackage or worse)
            {
                // Check if we need to warn that some dependency names are not found as directories
                FindDependenciesInAssets(sampleName, dependencyNames);
            }
        }

        static bool CheckForAndImportDependecies(string packageName, string sampleName, string[] dependencyNames, UnityEditor.PackageManager.PackageInfo packageInfo)
        {
            bool sampleImported = false;

            IEnumerable<Sample> samples = Sample.FindByPackage(packageName, packageInfo.version);

            foreach (var sample in samples)
            {
                if (dependencyNames.Contains(sample.displayName) && !sample.isImported)
                {
                    sample.Import();
                    Debug.Log("Ultraleap: " + sampleName + " has a dependency on " + sample.displayName + ". This dependency has been imported to " + sample.importPath);
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
        static void FindDependenciesInAssets(string sampleName, string[] dependencyNames)
        {
            var folders = AssetDatabase.GetSubFolders("Assets");

            foreach (var dependencyName in dependencyNames)
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
                    Debug.LogWarning("Ultraleap: " + sampleName + " has a dependency on " + dependencyName + ". No directory name matches this. Ensure you have this imported as a dependency when using " + sampleName);
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