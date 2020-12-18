using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System.Linq;

namespace Leap.Unity
{
    [InitializeOnLoad]
    public class HandBinder_DocumentationWindow : EditorWindow
    {
        //Store the current page
        int currentPage = 0;
        //Store which pages the user has already gone too
        List<int> previousPages = new List<int>();
        //The scroll position of the window
        Vector2 scrollPosition;
        //Store the color of the gui before changes where made
        static Color beforeColor;
        //Styles to be used for the content
        public static GUIStyle headerStyle, contentStyle, textureStyle, buttonStyle, autoRigButton;
        Texture2D labelImageBG;

        static UnityEngine.Object leapController;

        /// <summary>
        /// The documentation window will pop up the first time the user imports this module
        /// </summary>
        static HandBinder_DocumentationWindow()
        {
            EditorApplication.update += Open;
        }
        static void Open()
        {
            //Allow it to pop up when the user first installs the module
            if (PlayerPrefs.GetInt("Rigging_Documentation_PopUp") == 0)
            {
                //Stop it popping up when the project is loaded again
                PlayerPrefs.SetInt("Rigging_Documentation_PopUp", 1);
                Init();
            }
        }

        /// <summary>
        /// Makes the documentation window appear in its own menu at the top
        /// </summary>
        [MenuItem("Ultraleap/Hand Rigging Documentation")]
        static void Init()
        {
            leapController = AssetDatabase.LoadAssetAtPath("Assets/Plugins/LeapMotion/Core/Prefabs/LeapHandController.prefab", typeof(Leap.Unity.HandModelManager));
            // Get existing open window or if none, make a new one:
            var window = (HandBinder_DocumentationWindow)EditorWindow.GetWindow(typeof(HandBinder_DocumentationWindow));
            window.Show();
            window.minSize = new Vector2(650, 700);
        }

        void OnGUI()
        {
            SetUp();
            DrawPages();
        }

        /// <summary>
        /// Set up the GUIStyle for the varius elements that will be rendered
        /// </summary>
        void SetUp()
        {
            //Set up the GUI Styles
            headerStyle = new GUIStyle()
            {
                alignment = TextAnchor.MiddleCenter,
                fontStyle = FontStyle.Bold,
                fontSize = 30,
                normal = new GUIStyleState()
                {
                    textColor = Color.white,
                }
            };

            contentStyle = new GUIStyle()
            {
                alignment = TextAnchor.MiddleCenter,
                margin = new RectOffset(10, 0, 0, 10),
                wordWrap = true,
                fontStyle = FontStyle.Normal,
                fontSize = 13,
                normal = new GUIStyleState()
                {
                    textColor = Color.white,
                }
            };

            buttonStyle = new GUIStyle(GUI.skin.button)
            {
                alignment = TextAnchor.MiddleCenter,
                wordWrap = true,
                fontStyle = FontStyle.Bold,
                fontSize = 20,
                normal = new GUIStyleState()
                {
                    textColor = Color.black,
                    background = (Texture2D)Resources.Load<Texture>("Editor_Documentation_Green_Upstate")
                },
            };

            autoRigButton = new GUIStyle(GUI.skin.button)
            {
                alignment = TextAnchor.MiddleCenter,
                wordWrap = true,
                fontStyle = FontStyle.Bold,
                fontSize = 20,
                normal = new GUIStyleState()
                {
                    textColor = Color.black,
                    background = (Texture2D)Resources.Load<Texture>("Editor_Documentation_Yellow_Upstate")
                },
            };

            textureStyle = new GUIStyle()
            {
                alignment = TextAnchor.MiddleCenter,
                imagePosition = ImagePosition.ImageOnly,
            };

            beforeColor = GUI.color;

        }

        void Header(string header)
        {
            GUILayout.Space(20);
            GUILayout.Label(header, headerStyle);
            GUILayout.Space(20);
        }
        void DrawTexture(Texture texture)
        {
            GUILayout.Space(20);
            GUILayout.Label(texture, textureStyle);

            GUILayout.Space(20);
        }

        void PageButtons(int index, bool includePrevious = true, string buttonOverride = "Next Step")
        {
            GUILayout.Space(20);
            GUILayout.BeginHorizontal();
            GUILayout.Space(20);

            if (includePrevious)
            {
                if (GUILayout.Button("Previous Step", buttonStyle))
                {
                    PreviousPage();
                }
            }

            if (GUILayout.Button(buttonOverride, buttonStyle))
            {
                MovePage(index);
            }
            GUILayout.Space(20);

            GUILayout.EndHorizontal();
        }

        /// <summary>
        /// Depending on which page the user has selected, draw that page
        /// </summary>
        void DrawPages()
        {
            beforeColor = GUI.color;

            switch (currentPage)
            {
                case 0:
                    previousPages.Clear();
                    StartPage();
                    break;
                case 1:
                    Page1();
                    break;
                case 2:
                    Page2();
                    break;
                case 3:
                    Page3();
                    break;
                case 4:
                    Page4();
                    break;
                case 5:
                    Page5();
                    break;
            }
        }

        void MovePage(int specific)
        {
            //Store the last page
            previousPages.Add(currentPage);
            if (specific == -1)
                currentPage = (currentPage + 1) % 6;
            else
            {
                currentPage = specific;
            }

            scrollPosition = Vector2.zero;
        }
        void PreviousPage()
        {
            var last = previousPages.LastOrDefault();
            previousPages.RemoveAt(previousPages.Count - 1);
            currentPage = last;
        }


        void StartPage()
        {
            DrawTexture((Texture)Resources.Load("Editor_Ultraleap_logo"));
            GUILayout.Space(-50);
            Header("Hand Rigging Module");
            GUILayout.Space(100);
            GUILayout.Label("<size=20>Set up your own rigged hands with the Rigging Module</size>", contentStyle);
            GUILayout.Label("<size=20> Press Next Step to follow the step by step guide</size>", contentStyle);
            GUILayout.Space(100);

            GUILayout.BeginHorizontal();
            GUILayout.Space(50);
            if (GUILayout.Button("Next Step", buttonStyle))
            {
                MovePage(-1);
            }
            GUILayout.Space(50);
            GUILayout.EndHorizontal();
        }

        void Page1()
        {
            Header("Step 1");

            GUILayout.BeginHorizontal();
            GUILayout.Space(20);

            GUILayout.BeginVertical();
            GUILayout.Label("To get the leap motion data into the scene we need two components:", contentStyle);
            GUILayout.Label("<color=cyan><b>leap Service Provider</b></color>", contentStyle);
            GUILayout.Label("<color=cyan><b>Hand Model Manager</b></color>", contentStyle);

            GUILayout.Space(20);
            GUILayout.Label("Locate the Leap Hand Controller prefab in the 'Core Modules Prefabs' folder then place this into the scene", contentStyle);
            if (leapController != null)
            {
                leapController = (Leap.Unity.HandModelManager)EditorGUILayout.ObjectField(leapController, typeof(Leap.Unity.HandModelManager), false);
                GUILayout.Space(20);
            }

            GUILayout.Label("Create a new entry in the Hand Model Manager Script and name appropriately", contentStyle);
            GUILayout.Label("Remember to press 'is Enabled' on the new group you just made", contentStyle);
            GUILayout.Label("Remember to change the 'Edit Time Pose' to the desired setting", contentStyle);

            GUILayout.EndVertical();
            DrawTexture((Texture)Resources.Load("Editor_Step1_Inspector"));
            GUILayout.Space(20);

            GUILayout.EndHorizontal();
            PageButtons(-1);

        }

        void Page2()
        {
            Header("Step 2");

            GUILayout.BeginHorizontal();
            GUILayout.Space(20);

            GUILayout.BeginVertical();
            GUILayout.Label("Drag your hand model under the Leap Hand Controller that you dragged into the scene earlier.", contentStyle);
            GUILayout.Label("Assign the Leap Hand Binder script to the base of the hand model, the Autorigging function will search all children of the gameobject you assign it to.", contentStyle);
            GUILayout.Label("Set the hand to the desired left or right handedness on each script.", contentStyle);
            GUILayout.Label("Drag the Hand Rigging Scripts under the left and right slots of the Hand Model Manager.", contentStyle);
            GUILayout.Label("If you wish to use two hand models, repeat this step for the other hand", contentStyle);
            GUILayout.EndVertical();
            DrawTexture((Texture)Resources.Load("Editor_Step_2_Hand_Rig"));
            //DrawTexture((Texture)Resources.Load("Step_2_Assigned_HandModelManager"));
            GUILayout.Space(20);

            GUILayout.EndHorizontal();


            PageButtons(-1);
        }

        void Page3()
        {
            Header("Step 3");
            GUILayout.BeginHorizontal();
            GUILayout.BeginVertical();
            GUILayout.Label("Press the Auto Rig Button", contentStyle);
            GUILayout.Label("The Auto Rig button will try to find and assign transforms of the model hand into the slots of the hand graphic in the inspector for you.", contentStyle);
            GUILayout.EndVertical();
            DrawTexture((Texture)Resources.Load("Editor_Step_3_Hand_Rig"));
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();

            GUILayout.Space(100);
            GUILayout.EndHorizontal();

            PageButtons(5);
            EditorGUILayout.Space();
            if (GUILayout.Button("If auto rig failed - Click here!", autoRigButton))
            {
                MovePage(4);
            }
        }

        void Page4()
        {
            Header("Step 3 - Auto Rig Failed");

            GUILayout.BeginHorizontal();
            GUILayout.BeginVertical();
            GUILayout.Label("If the Autorig failed, slots on the hand will remain <color=red>RED</color>.", contentStyle);
            GUILayout.Label("You are still able to continue if you do not wish to include every finger bone, simply drag and drop the transform you wish to use into the slots on the hand graphic.", contentStyle);
            GUILayout.Label("Slots that are <color=green>GREEN</color> will be updated using leap data, any in <color=red>RED</color> will be skipped", contentStyle);
            GUILayout.EndVertical();
            DrawTexture((Texture)Resources.Load("Editor_AutoRig_Failed"));
            GUILayout.EndHorizontal();
            PageButtons(-1);
        }

        void Page5()
        {
            Header("Step 4");
            GUILayout.BeginHorizontal();
            GUILayout.BeginVertical();
            GUILayout.Label("Once the desired hands are assigned into the hand model manager, the hand model manager group is enabled and you have assigned the transforms of the hand you wish to use <b>PRESS PLAY</b>", contentStyle);

            GUILayout.Label("Make sure you have set each hand to either Left or Right Handedness", contentStyle);

            GUILayout.Label("If you notice the hands are flipped or are facing the wrong direction when in play mode? - Use the Fine Tuning tab to adjust the rotation offsets", contentStyle);

            GUILayout.Label("The fine tuning options allow you to add rotation offsets to all the transforms of the hand you have assigned.", contentStyle);

            GUILayout.Label("If you require further adjustments, use the Finger offsets to add extra offsets to individual fingers of the hand.", contentStyle);
            GUILayout.EndVertical();
            DrawTexture((Texture)Resources.Load("Editor_Final Step"));
            GUILayout.EndHorizontal();

            PageButtons(0, true, "Finish Set Up");
        }
    }
}

