using UnityEngine;
#if UNITY_EDITOR
using System.IO;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.PackageManager.UI;
using System.Linq;
#endif

namespace Leap.Unity
{
    #if UNITY_EDITOR
    [InitializeOnLoad]
    public class RunOnStart
    {
        static bool popupDone = false;

        static RunOnStart()
        {
            EditorApplication.delayCall += ExamplesDontExistDoOnce;
            EditorApplication.delayCall += UpdateExamplesDoOnce;
        }

        static void ExamplesDontExistDoOnce()
        {
            EditorPrefs.SetBool("ShowImportPopup", true);
            EditorPrefs.SetBool("ShowUpdatePopup", true);
            SessionState.SetBool("FirstInitDone", false);

            if (ExampleImportHelper.TrackingPackageInstalled() ||
            ExampleImportHelper.TrackingPreviewPackageInstalled()) // If either package exists
            {
                if (ExampleImportHelper.TrackingPackageInstalledExamplesDontExist()
                    || ExampleImportHelper.TrackingPreviewPackageInstalledExamplesDontExist()) // If either package exists and does not have examples
                {
                    if (!SessionState.GetBool("FirstInitDone", false) && (EditorPrefs.GetBool("ShowImportPopup")))
                    {
                        ImportExamplesPopupWindow window = EditorWindow.GetWindow<ImportExamplesPopupWindow>();
                        if(window == null) 
                        {
                            window = new ImportExamplesPopupWindow();
                        }

                        window.name = "Ultraleap Examples";
                        window.titleContent = new GUIContent("Ultraleap Examples");
                        window.position = new Rect(Screen.width / 2, Screen.height / 2, 400, 400); 
                        window.ShowUtility();
                        popupDone = true;
                    }
                }
            }
        }

        static void UpdateExamplesDoOnce()
        {
            if (!popupDone)
            {
                if (ExampleImportHelper.TrackingPackageInstalled() ||
                ExampleImportHelper.TrackingPreviewPackageInstalled())
                {
                    var trackingPackageInfo = ExampleImportHelper.GetPackageInfo("com.ultraleap.tracking");
                    var trackingPreviewPackageInfo = ExampleImportHelper.GetPackageInfo("com.ultraleap.tracking.preview");

                    if (trackingPackageInfo != null && !ExampleImportHelper.TrackingExamplesUpToDate()
                        || trackingPreviewPackageInfo != null && !ExampleImportHelper.TrackingPreviewExamplesUpToDate())
                    {
                        if (!SessionState.GetBool("FirstInitDone", false) && (EditorPrefs.GetBool("ShowUpdatePopup")))
                        {
                            UpdateExamplesPopupWindow window = EditorWindow.GetWindow<UpdateExamplesPopupWindow>();
                            if (window == null)
                            {
                                window = new UpdateExamplesPopupWindow();
                            }

                            window.name = "Ultraleap Examples Update";
                            window.titleContent = new GUIContent("Ultraleap Examples");
                            window.position = new Rect(Screen.width / 2, Screen.height / 2, 400, 400);
                            window.ShowUtility();
                        }
                    }
                }
            }
            SessionState.SetBool("FirstInitDone", true);
        }
    }

    public class UpdateExamplesPopupWindow : EditorWindow
    {
        private void OnGUI()
        {
            Texture _handTex = Resources.Load<Texture2D>("Ultraleap_Logo");
            GUI.DrawTexture(new Rect(0, 0, EditorGUIUtility.currentViewWidth, EditorGUIUtility.currentViewWidth * ((float)_handTex.height / (float)_handTex.width)), _handTex, ScaleMode.ScaleToFit);

            GUILayout.Space(EditorGUIUtility.currentViewWidth * ((float)_handTex.height / (float)_handTex.width));
            GUILayout.Space(20);

            EditorGUILayout.LabelField("It looks like you have older ultraleap examples than your package version, would you like to update them? \n " +
                "WARNING! This will overwrite any changes you have made to the example scripts and scenes.", EditorStyles.wordWrappedLabel);

            GUILayout.Space(20);
            GUILayout.BeginHorizontal();
            if (!ExampleImportHelper.TrackingExamplesUpToDate())
            {
                if (GUILayout.Button("Update Examples"))
                {
                    ExampleImportHelper.UpdatePackageExamples("com.ultraleap.tracking", "Assets/Samples/Ultraleap Tracking");
                }
            }
            else
            {
                GUI.enabled = false;
                if (GUILayout.Button("Update Examples"))
                {
                }
                GUI.enabled = true;
            }

            if (!ExampleImportHelper.TrackingPreviewExamplesUpToDate())
            {
                if (GUILayout.Button("Update Preview Examples"))
                {
                    ExampleImportHelper.UpdatePackageExamples("com.ultraleap.tracking.preview", "Assets/Samples/Ultraleap Tracking Preview");
                }
            }
            else
            {
                GUI.enabled = false;
                if (GUILayout.Button("Update Preview Examples"))
                {
                }
                GUI.enabled = true;
            }
            GUILayout.EndHorizontal();
            GUILayout.Space(20);
            if (ExampleImportHelper.TrackingPreviewExamplesExist() && ExampleImportHelper.TrackingExamplesExist())
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
            bool showAgain = !GUILayout.Toggle(EditorPrefs.GetBool("ShowUpdatePopup"), "Do not show this again?");
            EditorPrefs.SetBool("ShowUpdatePopup", showAgain);
        }
    }

    public class ImportExamplesPopupWindow : EditorWindow
    {
        private void OnGUI()
        {
            Texture _handTex = Resources.Load<Texture2D>("Ultraleap_Logo");
            GUI.DrawTexture(new Rect(0, 0, EditorGUIUtility.currentViewWidth, EditorGUIUtility.currentViewWidth * ((float)_handTex.height / (float)_handTex.width)), _handTex, ScaleMode.ScaleToFit);

            GUILayout.Space(EditorGUIUtility.currentViewWidth * ((float)_handTex.height / (float)_handTex.width));
            GUILayout.Space(20);

            if (ExampleImportHelper.TrackingPackageInstalledAndExamplesExist() && ExampleImportHelper.TrackingPreviewPackageInstalledAndExamplesExist()) // both packages installed and both examples exist
            {
                EditorGUILayout.LabelField("All Examples are now imported, you can find them in 'Assets/Samples'", EditorStyles.wordWrappedLabel);
            }
            else if(ExampleImportHelper.TrackingPackageInstalledAndExamplesExist() && ExampleImportHelper.GetPackageInfo("com.ultraleap.tracking.preview") == null)
            {
                EditorGUILayout.LabelField("All Examples are now imported, you can find them in 'Assets/Samples'", EditorStyles.wordWrappedLabel);
            }
            else if (ExampleImportHelper.TrackingPreviewPackageInstalledAndExamplesExist() && ExampleImportHelper.GetPackageInfo("com.ultraleap.tracking") == null)
            {
                EditorGUILayout.LabelField("All Examples are now imported, you can find them in 'Assets/Samples'", EditorStyles.wordWrappedLabel);
            }
            else
            {
                EditorGUILayout.LabelField("We've noticed you dont have our examples imported in this project, would you like to import them now?", EditorStyles.wordWrappedLabel);
            }
            GUILayout.Space(20);
            GUILayout.BeginHorizontal();
            if (!ExampleImportHelper.TrackingExamplesExist() && ExampleImportHelper.GetPackageInfo("com.ultraleap.tracking") != null)
            {
                if (GUILayout.Button("Import Examples"))
                {
                    ExampleImportHelper.ImportPackageExamples("com.ultraleap.tracking");
                }
            }
            else if(ExampleImportHelper.TrackingPackageInstalledAndExamplesExist())
            {
                GUI.enabled = false;
                if (GUILayout.Button("Import Examples"))
                {
                }
                GUI.enabled = true;
            }
            if (!ExampleImportHelper.TrackingPreviewExamplesExist() && ExampleImportHelper.GetPackageInfo("com.ultraleap.tracking.preview") != null)
            {
                
                if (GUILayout.Button("Import Preview Examples"))
                {
                    ExampleImportHelper.ImportPackageExamples("com.ultraleap.tracking.preview");
                }
            }
            else if(ExampleImportHelper.TrackingPreviewPackageInstalledAndExamplesExist())
            {
                GUI.enabled = false;
                if (GUILayout.Button("Import Preview Examples"))
                {
                }
                GUI.enabled = true;
            }
            GUILayout.EndHorizontal();

            GUILayout.Space(20);
            if (ExampleImportHelper.TrackingPreviewExamplesExist() && ExampleImportHelper.TrackingExamplesExist())
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
            bool showAgain = !GUILayout.Toggle(EditorPrefs.GetBool("ShowUpdatePopup"), "Do not show this again?");
            EditorPrefs.SetBool("ShowUpdatePopup", showAgain);
        }
    }
    #endif

    public static class ExampleImportHelper
    {
        public static bool TrackingPackageInstalledExamplesDontExist()
        {
            return !ExampleImportHelper.TrackingExamplesExist() && ExampleImportHelper.GetPackageInfo("com.ultraleap.tracking") != null;
        }
        public static bool TrackingPreviewPackageInstalledExamplesDontExist()
        {
            return !ExampleImportHelper.TrackingPreviewExamplesExist() && ExampleImportHelper.GetPackageInfo("com.ultraleap.tracking.preview") != null;
        }

        public static bool TrackingPackageInstalledAndExamplesExist()
        {
            return ExampleImportHelper.TrackingExamplesExist() && ExampleImportHelper.GetPackageInfo("com.ultraleap.tracking") != null;
        }
        public static bool TrackingPreviewPackageInstalledAndExamplesExist()
        {
            return ExampleImportHelper.TrackingPreviewExamplesExist() && ExampleImportHelper.GetPackageInfo("com.ultraleap.tracking.preview") != null;
        }

        public static bool TrackingPackageInstalled()
        {
            return ExampleImportHelper.GetPackageInfo("com.ultraleap.tracking") != null;
        }

        public static bool TrackingPreviewPackageInstalled()
        {
            return ExampleImportHelper.GetPackageInfo("com.ultraleap.tracking.preview") != null;
        }

        public static bool TrackingExamplesExist()
        {
            return Directory.Exists("Assets/Samples/Ultraleap Tracking");
        }
        public static bool TrackingPreviewExamplesExist()
        {
            return Directory.Exists("Assets/Samples/Ultraleap Tracking Preview");
        }

        public static bool TrackingExamplesUpToDate()
        {
            var trackingPackageInfo = ExampleImportHelper.GetPackageInfo("com.ultraleap.tracking");
            var assetFolderVer = new DirectoryInfo(AssetDatabase.GetSubFolders("Assets/Samples/Ultraleap Tracking").First()).Name;
            var sameVersion = trackingPackageInfo.version == assetFolderVer;
            return sameVersion;
        }
        public static bool TrackingPreviewExamplesUpToDate()
        {
            var trackingPreviewPackageInfo = ExampleImportHelper.GetPackageInfo("com.ultraleap.tracking.preview");
            var assetFolderVer = new DirectoryInfo(AssetDatabase.GetSubFolders("Assets/Samples/Ultraleap Tracking Preview").First()).Name;
            var sameVersion = trackingPreviewPackageInfo.version == assetFolderVer;
            return sameVersion;
        }

        public static UnityEditor.PackageManager.PackageInfo GetPackageInfo(string packageName)
        {
            List<UnityEditor.PackageManager.PackageInfo> packageJsons = AssetDatabase.FindAssets("package")
                .Select(AssetDatabase.GUIDToAssetPath).Where(x => AssetDatabase.LoadAssetAtPath<TextAsset>(x) != null)
                .Select(UnityEditor.PackageManager.PackageInfo.FindForAssetPath).ToList();

            if(packageJsons == null) 
                return null;

            return packageJsons.FirstOrDefault(x => x?.name == packageName); 
        }

        public static bool ImportPackageExamples(string packageName)
        {
            UnityEditor.PackageManager.PackageInfo packageInfo = ExampleImportHelper.GetPackageInfo(packageName);

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
        public static bool UpdatePackageExamples(string packageName, string samplesFolder)
        {
            UnityEditor.PackageManager.PackageInfo packageInfo = ExampleImportHelper.GetPackageInfo(packageName);

            if (packageInfo != null)
            {
                IEnumerable<Sample> samples = Sample.FindByPackage(packageName, packageInfo.version);

                foreach (var sample in samples)
                {
                    
                    AssetDatabase.DeleteAsset(samplesFolder);
                    return sample.Import();
                }
            }
            return false;
        }
    }
}