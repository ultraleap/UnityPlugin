using System.Collections;
using System.Collections.Generic;
using UnityEngine;
//#if UNITY_EDITOR
using UnityEditor;
using UnityEngine.SocialPlatforms.Impl;
using UnityEngine.PlayerLoop;
using System.IO;
using UnityEditor.PackageManager.UI;
using UnityEditor.PackageManager;
using static UnityEditor.Progress;
//#endif


namespace Leap.Unity
{
    [InitializeOnLoad]
    public static class RunOnStart
    {
        static RunOnStart()
        {
            EditorApplication.delayCall += DoOnce;
        }

        static void DoOnce()
        {
            ///////////////////// Remove This! ////////////////////
            EditorPrefs.SetBool("ShowExamplePopup", true);
            ////////////////////////////////////////////////////

            

            if (!PluginSettingsPopupWindow.TrackingExamplesExist() && PluginSettingsPopupWindow.GetPackageInfo("com.ultraleap.tracking") != null 
                || !PluginSettingsPopupWindow.TrackingPreviewExamplesExist() && PluginSettingsPopupWindow.GetPackageInfo("com.ultraleap.tracking.preview") != null
                 )
            {
                PluginSettingsPopupWindow window = new PluginSettingsPopupWindow();
                window.name = "Ultraleap Examples";

                if (!SessionState.GetBool("FirstInitDone", false))
                {
                    SessionState.SetBool("FirstInitDone", true);

                    if (EditorPrefs.GetBool("ShowExamplePopup"))
                    {
                        window.position = new Rect(Screen.width / 2, Screen.height / 2, 250, 150);
                        window.ShowUtility();
                    }
                }
            }
        }

        private static bool TrackingExamplesExist()
        {
            return Directory.Exists("Assets/Samples/Ultraleap Tracking");
        }

    }


    
    public class PluginSettingsPopupWindow : EditorWindow
    {
        void OnGUI()
        {
            EditorGUILayout.LabelField("We've noticed you dont have our examples inported in this project, would you like to import them now?", EditorStyles.wordWrappedLabel);
            GUILayout.Space(20);

            bool showAgain = !GUILayout.Toggle(EditorPrefs.GetBool("ShowExamplePopup"), "Do not show this again?");

            EditorPrefs.SetBool("ShowExamplePopup", showAgain);

            GUILayout.BeginHorizontal();
            if (!TrackingExamplesExist() && GetPackageInfo("com.ultraleap.tracking") != null)
            {
                if (GUILayout.Button("Import Examples"))
                {
                    ImportPackageExamples("com.ultraleap.tracking");
                }
            }
            if (!TrackingPreviewExamplesExist() && GetPackageInfo("com.ultraleap.tracking.preview") != null)
            {
                if (GUILayout.Button("Import Preview Examples"))
                {
                    ImportPackageExamples("com.ultraleap.tracking.preview");
                }
            }
            GUILayout.EndHorizontal();

            if (TrackingPreviewExamplesExist() && TrackingExamplesExist())
            {
                if (GUILayout.Button("All Examples Imported. Close this window."))
                {
                    this.Close();
                }
            }
            else
            {
                if (GUILayout.Button("No Thanks"))
                {
                    this.Close();
                }
            }

            
        }


        public static bool TrackingExamplesExist()
        {
            return Directory.Exists("Assets/Samples/Ultraleap Tracking");
        }
        public static bool TrackingPreviewExamplesExist()
        {
            return Directory.Exists("Assets/Samples/Ultraleap Tracking Preview");
        }

        private bool ImportPackageExamples(string packageName)
        {
            UnityEditor.PackageManager.PackageInfo packageInfo = GetPackageInfo(packageName);

            if (packageInfo != null)
            {
                IEnumerable<Sample> samples = Sample.FindByPackage(packageName, packageInfo.version);

                foreach (var sample in samples)
                {
                    return sample.Import();
                }
            }
            return false;
        }

        public static UnityEditor.PackageManager.PackageInfo GetPackageInfo(string packageName) 
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
        
        static string GetPackageFullPath(string packageName)
        {
            // Check for potential UPM package
            string packagePath = Path.GetFullPath("Packages/" + packageName);
            if (Directory.Exists(packagePath))
            {
                return packagePath;
            }

            return null;
        }
    }


}
