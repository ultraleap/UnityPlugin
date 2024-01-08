using UnityEngine;
using UnityEngine.SceneManagement;
using System;
#if UNITY_EDITOR
using System.IO;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.PackageManager.UI;
using Leap.Unity.PhysicalHands;
using UnityEditor.SearchService;
using UnityEditor.SceneManagement;
using System.Linq;
#endif


namespace Leap.Unity.PhysicalHands
{
#if UNITY_EDITOR
    [InitializeOnLoad]
    public class PhysicalHandsSettingsPopup
    {
        static PhysicalHandsSettingsPopupWindow window;

        static PhysicalHandsSettingsPopup()
        {
            UnityEditor.SceneManagement.EditorSceneManager.sceneOpened += OnAfterSceneLoad;
        }

        private static void OnAfterSceneLoad(UnityEngine.SceneManagement.Scene scene, OpenSceneMode mode)
        {
            PhysicsSettingsForPhysicalHands();
        }



        static void PhysicsSettingsForPhysicalHands()
        {

            if (GameObject.FindFirstObjectByType<PhysicalHandsManager>() != null
                && UltraleapSettings.Instance.showPhysicalHandsPhysicsSettingsWarning == true
                && !PhysicalHandsSettings.AllSettingsApplied()
                && EditorWindow.GetWindow<PhysicalHandsSettingsPopupWindow>() == null)
            {
                window = EditorWindow.GetWindow<PhysicalHandsSettingsPopupWindow>();
                if (window == null)
                {
                    window = new PhysicalHandsSettingsPopupWindow();
                }

                window.name = "Physical Hands Settings";
                window.titleContent = new GUIContent("Physical Hands Settings");
                window.position = new Rect(Screen.width / 2, Screen.height / 2, 400, 400);
                window.ShowUtility();
            }
        }
    }


    public class PhysicalHandsSettingsPopupWindow : EditorWindow
    {
        private void OnGUI()
        {
            Texture _handTex = Resources.Load<Texture2D>("Ultraleap_Logo");
            GUI.DrawTexture(new Rect(0, 0, EditorGUIUtility.currentViewWidth, EditorGUIUtility.currentViewWidth * ((float)_handTex.height / (float)_handTex.width)), _handTex, ScaleMode.ScaleToFit);

            GUILayout.Space(EditorGUIUtility.currentViewWidth * ((float)_handTex.height / (float)_handTex.width));
            GUILayout.Space(20);

            EditorGUILayout.LabelField("We notice you dont have our recommended physics settings for using physical hands. \n \n" +
                "These will give you the best experience when using Physical hands. \n \n" +
                "Would you like to go to the settings page now?.", EditorStyles.wordWrappedLabel);


            GUILayout.Space(20);


            bool showAgain = !GUILayout.Toggle(UltraleapSettings.Instance.showPhysicalHandsPhysicsSettingsWarning, "Do not show this again?");
            UltraleapSettings.Instance.showPhysicalHandsPhysicsSettingsWarning = showAgain;


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