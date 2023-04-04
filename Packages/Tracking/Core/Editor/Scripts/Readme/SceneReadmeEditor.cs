using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;

namespace Leap.Unity.Readme
{
    [CustomEditor(typeof(SceneReadme))]
    [InitializeOnLoad]
    public class SceneReadmeEditor : Editor
    {

        private static string kReadmeScene = "ReadmeEditor.latestScenePath";

        private static float kSpace = 16f;

        private static SceneReadme sceneReadme = null;

        static SceneReadmeEditor()
        {
            EditorApplication.delayCall += FirstLoad;
            EditorSceneManager.sceneOpened += ChangedScene;
        }

        private static void ChangedScene(UnityEngine.SceneManagement.Scene scene, OpenSceneMode mode)
        {
            if (SessionState.GetString(kReadmeScene, null) != EditorSceneManager.GetActiveScene().path)
            {
                SessionState.SetString(kReadmeScene, EditorSceneManager.GetActiveScene().path);
                SelectSceneReadme();
                if (sceneReadme != null)
                {
                    ActiveEditorTracker.sharedTracker.isLocked = true;
                }
            }
        }

        private static void FirstLoad()
        {
            // Unload so we don't keep trying do it over and over
            EditorApplication.delayCall -= FirstLoad;
            ChangedScene(EditorSceneManager.GetActiveScene(), OpenSceneMode.Single);
        }

        [MenuItem("Window/Ultraleap/Show Readme For Scene")]
        private static void SelectSceneReadme()
        {
            var ids = AssetDatabase.FindAssets("t:SceneReadme");
            if (ids.Length > 0)
            {
                sceneReadme = null;
                SceneAsset currentSceneFile = AssetDatabase.LoadAssetAtPath<SceneAsset>(EditorSceneManager.GetActiveScene().path);
                for (int i = 0; i < ids.Length; i++)
                {
                    try
                    {
                        SceneReadme readmeObject = (SceneReadme)AssetDatabase.LoadMainAssetAtPath(AssetDatabase.GUIDToAssetPath(ids[i]));

                        if (readmeObject.scene == null)
                        {
                            continue;
                        }

                        if (currentSceneFile == readmeObject.scene)
                        {
                            Selection.objects = new UnityEngine.Object[] { readmeObject };
                            sceneReadme = readmeObject;
                            break;
                        }
                    }
                    catch
                    {
                        continue;
                    }
                }
            }
        }

        protected override void OnHeaderGUI()
        {
            var readme = (SceneReadme)target;
            Init();

            var iconWidth = Mathf.Min(EditorGUIUtility.currentViewWidth / 3f - 20f, 128f);

            GUILayout.BeginHorizontal("In BigTitle");
            {
                GUILayout.Label(readme.icon, ImageStyle, GUILayout.Width(iconWidth), GUILayout.Height(iconWidth));

                GUILayout.BeginVertical();
                {
                    GUILayout.Label(readme.title, TitleStyle);

                    GUILayout.BeginVertical(EditorStyles.helpBox);
                    {
                        if (ActiveEditorTracker.sharedTracker.isLocked)
                        {
                            GUILayout.Label("This readme may have locked your inspector." +
                            "\nYou can unlock it by clicking the padlock above the readme title.", InfoStyle);
                            EditorGUILayout.Space();
                        }

                        GUILayout.Label("You can return to this readme at any time by going to" +
                            "\nWindow -> Ultraleap -> Show Readme For Scene", InfoStyle);
                    }
                    GUILayout.EndVertical();
                }
                GUILayout.EndVertical();
            }
            GUILayout.EndHorizontal();
        }

        public override void OnInspectorGUI()
        {
            var readme = (SceneReadme)target;
            Init();

            if (readme.sections != null)
            {
                foreach (var section in readme.sections)
                {
                    if (!string.IsNullOrEmpty(section.heading))
                    {
                        GUILayout.Label(section.heading, HeadingStyle);
                    }
                    if (section.image != null)
                    {
                        GUILayout.Box(section.image, GUILayout.Height(section.imageHeight == 0 ? (section.image.height < 200 ? section.image.height : 200) : section.imageHeight), GUILayout.Width(EditorGUIUtility.currentViewWidth - 38));
                    }
                    if (!string.IsNullOrEmpty(section.text))
                    {
                        GUILayout.Label(section.text, BodyStyle);
                    }
                    if (!string.IsNullOrEmpty(section.pingSceneElement))
                    {
                        int pos = section.pingSceneElement.LastIndexOf("/") + 1;
                        if (GUILayout.Button($"Ping {section.pingSceneElement.Substring(pos, section.pingSceneElement.Length - pos)} in the Scene"))
                        {
                            GameObject ping = GameObject.Find(section.pingSceneElement);
                            if (ping != null)
                            {
                                EditorGUIUtility.PingObject(ping);
                            }
                        }
                    }
                    if (!string.IsNullOrEmpty(section.settingsPage))
                    {
                        int pos = section.settingsPage.LastIndexOf("/") + 1;
                        if (GUILayout.Button($"Open {section.settingsPage.Substring(pos, section.settingsPage.Length - pos)} Settings"))
                        {
                            SettingsService.OpenProjectSettings(section.settingsPage);
                        }
                    }
                    if (!string.IsNullOrEmpty(section.linkText))
                    {
                        if (LinkLabel(new GUIContent(section.linkText)))
                        {
                            Application.OpenURL(section.url);
                        }
                    }
                    GUILayout.Space(kSpace);
                }
            }
        }

        private bool m_Initialized;

        private GUIStyle LinkStyle { get { return m_LinkStyle; } }
        [SerializeField] GUIStyle m_LinkStyle;

        private GUIStyle ImageStyle { get { return m_ImageStyle; } }
        [SerializeField] GUIStyle m_ImageStyle;

        private GUIStyle TitleStyle { get { return m_TitleStyle; } }
        [SerializeField] GUIStyle m_TitleStyle;

        private GUIStyle HeadingStyle { get { return m_HeadingStyle; } }
        [SerializeField] GUIStyle m_HeadingStyle;

        private GUIStyle BodyStyle { get { return m_BodyStyle; } }
        [SerializeField] GUIStyle m_BodyStyle;

        private GUIStyle InfoStyle { get { return m_InfoStyle; } }
        [SerializeField] GUIStyle m_InfoStyle;

        void Init()
        {
            if (m_Initialized)
                return;
            m_BodyStyle = new GUIStyle(EditorStyles.label);
            m_BodyStyle.wordWrap = true;
            m_BodyStyle.richText = true;
            m_BodyStyle.fontSize = 14;

            m_InfoStyle = new GUIStyle(m_BodyStyle);
            m_InfoStyle.fontStyle = FontStyle.Italic;
            m_InfoStyle.fontSize = 12;

            m_ImageStyle = new GUIStyle(EditorStyles.label);
            m_ImageStyle.alignment = TextAnchor.MiddleCenter;

            m_TitleStyle = new GUIStyle(m_BodyStyle);
            m_TitleStyle.fontSize = 26;

            m_HeadingStyle = new GUIStyle(m_BodyStyle);
            m_HeadingStyle.fontSize = 18;

            m_LinkStyle = new GUIStyle(m_BodyStyle);
            m_LinkStyle.wordWrap = false;
            // Match selection color which works nicely for both light and dark skins
            m_LinkStyle.normal.textColor = new Color(0x00 / 255f, 0x78 / 255f, 0xDA / 255f, 1f);
            m_LinkStyle.stretchWidth = false;

            m_Initialized = true;
        }

        bool LinkLabel(GUIContent label, params GUILayoutOption[] options)
        {
            var position = GUILayoutUtility.GetRect(label, LinkStyle, options);

            Handles.BeginGUI();
            Handles.color = LinkStyle.normal.textColor;
            Handles.DrawLine(new Vector3(position.xMin, position.yMax), new Vector3(position.xMax, position.yMax));
            Handles.color = Color.white;
            Handles.EndGUI();

            EditorGUIUtility.AddCursorRect(position, MouseCursor.Link);

            return GUI.Button(position, label, LinkStyle);
        }
    }
}