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

            if (PluginSettingsPopupWindow.TrackingPackageInstalled() ||
            PluginSettingsPopupWindow.TrackingPreviewPackageInstalled())
            {
                if (PluginSettingsPopupWindow.TrackingPackageInstalledExamplesDontExist()
                    || PluginSettingsPopupWindow.TrackingPreviewPackageInstalledExamplesDontExist())
                {
                    if (!SessionState.GetBool("FirstInitDone", false) && (EditorPrefs.GetBool("ShowExamplePopup")))
                    {
                        PluginSettingsPopupWindow window = EditorWindow.GetWindow<PluginSettingsPopupWindow>();
                        if(window == null ) 
                        {
                            window = new PluginSettingsPopupWindow();
                        }

                        window.name = "Ultraleap Examples";
                        window.position = new Rect(Screen.width / 2, Screen.height / 2, 250, 150);
                        window.ShowUtility();

                        SessionState.SetBool("FirstInitDone", true);
                    }
                }
            }
        }
    }

    public class PluginSettingsPopupWindow : EditorWindow
    {
        private void OnGUI()
        {
            if (TrackingPackageInstalledAndExamplesExist() && TrackingPreviewPackageInstalledAndExamplesExist()) // both packages installed and both examples exist
            {
                EditorGUILayout.LabelField("All Examples are now imported, you can find them in 'Assets/Samples'", EditorStyles.wordWrappedLabel);
            }
            else if(TrackingPackageInstalledAndExamplesExist() && GetPackageInfo("com.ultraleap.tracking.preview") == null)
            {
                EditorGUILayout.LabelField("All Examples are now imported, you can find them in 'Assets/Samples'", EditorStyles.wordWrappedLabel);
            }
            else if (TrackingPreviewPackageInstalledAndExamplesExist() && GetPackageInfo("com.ultraleap.tracking") == null)
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

        public static bool TrackingPackageInstalledAndExamplesExist()
        {
            return PluginSettingsPopupWindow.TrackingExamplesExist() && PluginSettingsPopupWindow.GetPackageInfo("com.ultraleap.tracking") != null;
        }
        public static bool TrackingPreviewPackageInstalledAndExamplesExist()
        {
            return PluginSettingsPopupWindow.TrackingPreviewExamplesExist() && PluginSettingsPopupWindow.GetPackageInfo("com.ultraleap.tracking.preview") != null;
        }

        public static bool TrackingPackageInstalled()
        {
            return PluginSettingsPopupWindow.GetPackageInfo("com.ultraleap.tracking") != null;
        }

        public static bool TrackingPreviewPackageInstalled()
        {
            return PluginSettingsPopupWindow.GetPackageInfo("com.ultraleap.tracking.preview") != null;
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

