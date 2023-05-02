using UnityEngine;
//#if UNITY_EDITOR
using System.IO;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.PackageManager.UI;
using System.Linq;
//#endif

namespace Leap.Unity
{
    //#if UNITY_EDITOR
    [InitializeOnLoad]
    public class RunOnStart
    {
        

        static RunOnStart()
        {
            EditorApplication.delayCall += DoOnce;
        }

        static void DoOnce()
        {
            EditorPrefs.SetBool("ShowExamplePopup", true);

            PluginSettingsPopupWindow window = EditorWindow.GetWindow<PluginSettingsPopupWindow>();
            if(window == null ) 
            {
                window = new PluginSettingsPopupWindow();
            }
            bool trackingButNoExamples = PluginSettingsPopupWindow.TrackingPackageInstalledExamplesDontExist();
            bool trackingPreviewButNoExamples = PluginSettingsPopupWindow.TrackingPreviewPackageInstalledExamplesDontExist();

            if (trackingButNoExamples != null && trackingPreviewButNoExamples != null)
            {
                if (PluginSettingsPopupWindow.TrackingPackageInstalledExamplesDontExist()
                    || PluginSettingsPopupWindow.TrackingPreviewPackageInstalledExamplesDontExist()
                     )
                {
                    window.name = "Ultraleap Examples";

                    if (!SessionState.GetBool("FirstInitDone", false) && (EditorPrefs.GetBool("ShowExamplePopup")))
                    {
                        SessionState.SetBool("FirstInitDone", true);

                        window.position = new Rect(Screen.width / 2, Screen.height / 2, 250, 150);
                        window.ShowUtility();
                    }
                }
            }
            
        }
    }




    public class PluginSettingsPopupWindow : EditorWindow
    {
        private void OnGUI()
        {
            if (TrackingPackageInstalledExamplesExist() && TrackingPreviewPackageInstalledExamplesExist()) // both packages installed and both examples exist
            {
                EditorGUILayout.LabelField("All Examples are now imported, you can find them in 'Assets/Samples'", EditorStyles.wordWrappedLabel);
            }
            else if(TrackingPackageInstalledExamplesExist() && GetPackageInfo("com.ultraleap.tracking.preview") == null)
            {
                EditorGUILayout.LabelField("All Examples are now imported, you can find them in 'Assets/Samples'", EditorStyles.wordWrappedLabel);
            }
            else if (TrackingPreviewPackageInstalledExamplesExist() && GetPackageInfo("com.ultraleap.tracking") == null)
            {
                EditorGUILayout.LabelField("All Examples are now imported, you can find them in 'Assets/Samples'", EditorStyles.wordWrappedLabel);
            }
            else
            {
                EditorGUILayout.LabelField("We've noticed you dont have our examples imported in this project, would you like to import them now?", EditorStyles.wordWrappedLabel);
            }
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
                if (GUILayout.Button("Close this window."))
                {
                    this.Close();
                }
            }
            else
            {
                //contentExists = true;
                if (GUILayout.Button("No Thanks"))
                {
                    this.Close();
                }
            }

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
            List<UnityEditor.PackageManager.PackageInfo> packageJsons = AssetDatabase.FindAssets("package")
                .Select(AssetDatabase.GUIDToAssetPath).Where(x => AssetDatabase.LoadAssetAtPath<TextAsset>(x) != null)
                .Select(UnityEditor.PackageManager.PackageInfo.FindForAssetPath).ToList();

            return packageJsons.FirstOrDefault(x => x.name == packageName);
        }

        public static bool TrackingPackageInstalledExamplesDontExist()
        {
            return !PluginSettingsPopupWindow.TrackingExamplesExist() && PluginSettingsPopupWindow.GetPackageInfo("com.ultraleap.tracking") != null;
        }
        public static bool TrackingPreviewPackageInstalledExamplesDontExist()
        {
            return !PluginSettingsPopupWindow.TrackingPreviewExamplesExist() && PluginSettingsPopupWindow.GetPackageInfo("com.ultraleap.tracking.preview") != null;
        }

        public static bool TrackingPackageInstalledExamplesExist()
        {
            return PluginSettingsPopupWindow.TrackingExamplesExist() && PluginSettingsPopupWindow.GetPackageInfo("com.ultraleap.tracking") != null;
        }
        public static bool TrackingPreviewPackageInstalledExamplesExist()
        {
            return PluginSettingsPopupWindow.TrackingPreviewExamplesExist() && PluginSettingsPopupWindow.GetPackageInfo("com.ultraleap.tracking.preview") != null;
        }

        public static bool TrackingExamplesExist()
        {
            return Directory.Exists("Assets/Samples/Ultraleap Tracking");
        }
        public static bool TrackingPreviewExamplesExist()
        {
            return Directory.Exists("Assets/Samples/Ultraleap Tracking Preview");
        }
    }
    //#endif


}

