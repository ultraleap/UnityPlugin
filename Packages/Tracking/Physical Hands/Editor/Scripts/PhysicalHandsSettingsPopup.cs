using UnityEngine;
using UnityEngine.SceneManagement;

#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
#endif

namespace Leap.Unity.PhysicalHands
{
#if UNITY_EDITOR
    [InitializeOnLoad]
    internal static class PhysicalHandsSettingsPopup
    {
        static PhysicalHandsSettingsPopupWindow window;

        static PhysicalHandsSettingsPopup()
        {
            EditorSceneManager.sceneOpened -= OnAfterSceneLoad;
            EditorSceneManager.sceneOpened += OnAfterSceneLoad;

            if (SessionState.GetBool("editorStartupDelayedCalled", false) == false)
            {
                // Runs a delayed delegate which is fired when the editor finishes fully loading
                //      this allows for the popup to appear on editor start, and when recompiling
                //      We use editorStartupDelayedCalled to only fire this on editor start, to avoid recompiling annoyance
                EditorApplication.delayCall += () =>
                {
                    SessionState.SetBool("editorStartupDelayedCalled", true);
                    ShowPopupIfRequired();
                };
            }
        }

        private static void OnAfterSceneLoad(Scene scene, OpenSceneMode mode)
        {
            ShowPopupIfRequired();
        }

        private static void ShowPopupIfRequired()
        {
            if (GameObject.FindObjectOfType<PhysicalHandsManager>() != null
                && UltraleapSettings.Instance.showPhysicalHandsPhysicsSettingsWarning == true
                && !PhysicalHandsSettings.AllSettingsApplied())
            {
                window = EditorWindow.GetWindow<PhysicalHandsSettingsPopupWindow>();

                window.name = "Physical Hands Settings Warning";
                window.titleContent = new GUIContent("Physical Hands Settings Warning");
                window.ShowUtility();
            }
        }
    }

    internal class PhysicalHandsSettingsPopupWindow : EditorWindow
    {
        private void OnGUI()
        {
            GUILayout.Space(10);

            Texture logoTexture = Resources.Load<Texture2D>("Ultraleap_Logo");

            float imgWidth = EditorGUIUtility.currentViewWidth / 2;
            float imgHeight = imgWidth * ((float)logoTexture.height / (float)logoTexture.width);

            GUI.DrawTexture(new Rect((EditorGUIUtility.currentViewWidth / 2) - (imgWidth / 2), 0, imgWidth, imgHeight), logoTexture, ScaleMode.ScaleToFit);

            GUILayout.Space(imgHeight);
            GUILayout.Space(10);

            EditorGUILayout.LabelField("You are using Ultraleap Physical Hands but do not have the recommended physics settings for: \n \n" +
                "- Reducing physics issues \n" +
                "- Improving interaction capabilities \n \n" +
                "Would you like to open the settings panel?.", EditorStyles.wordWrappedLabel);

            GUILayout.Space(20);

            bool showAgain = UltraleapSettings.Instance.showPhysicalHandsPhysicsSettingsWarning;
            showAgain = !GUILayout.Toggle(!showAgain, "Do not show this again?"); // convert to "do not show" for display, then convert back to show for the settings UI

            if (showAgain != UltraleapSettings.Instance.showPhysicalHandsPhysicsSettingsWarning)
            {
                if(!showAgain)
                {
                    Debug.Log("You have chosen to not show the Physical Hands Recommended Settings warning, you can enable it via Ultraleap Settings in the Project Settings panel");
                }

                UltraleapSettings.Instance.showPhysicalHandsPhysicsSettingsWarning = showAgain;
                UltraleapSettings.GetSerializedSettings().ApplyModifiedPropertiesWithoutUndo();
            }

            GUILayout.Space(20);
            GUILayout.BeginHorizontal();

            if (GUILayout.Button("Go To Settings"))
            {
                SettingsService.OpenProjectSettings("Project/Ultraleap/Physical Hands");
                this.Close();
            }

            if (GUILayout.Button("No Thanks"))
            {
                this.Close();
            }

            GUILayout.EndHorizontal();
        }
    }
#endif
}