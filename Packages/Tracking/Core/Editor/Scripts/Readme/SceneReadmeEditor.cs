using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Leap.Unity.Readme
{
    [CustomEditor(typeof(SceneReadme))]
    [InitializeOnLoad]
    public class SceneReadmeEditor : Editor
    {

        private static string kReadmeScene = "ReadmeEditor.latestScenePath";

        private static float kSpace = 14f;

        private static float miniSpace = 3f;

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
                SelectSceneReadme(true);
            }
        }

        private static void FirstLoad()
        {
            // Unload so we don't keep trying do it over and over
            EditorApplication.delayCall -= FirstLoad;
            ChangedScene(EditorSceneManager.GetActiveScene(), OpenSceneMode.Single);
        }

        [MenuItem("Ultraleap/Show Readme For Scene")]
        public static void SelectSceneReadmeDropdown()
        {
            SelectSceneReadme();
        }

        public static bool SelectSceneReadme(bool silent = false)
        {
            var ids = AssetDatabase.FindAssets("t:SceneReadme");
            bool found = false;
            if (ids.Length > 0)
            {
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
                            found = true;
                            Selection.objects = new UnityEngine.Object[] { readmeObject };
                            break;
                        }
                    }
                    catch
                    {
                        continue;
                    }
                }
            }
            if (!silent && !found)
            {
                EditorUtility.DisplayDialog("No Readme for Scene", "This scene does not currently have a readme associated to it.", "Ok");
            }
            return found;
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
                GUILayout.Space(miniSpace);
                foreach (var section in readme.sections)
                {
                    if (!string.IsNullOrEmpty(section.heading))
                    {
                        GUILayout.Label(section.heading, HeadingStyle);
                        GUILayout.Space(miniSpace);
                    }
                    if (section.image != null)
                    {
                        GUILayout.Box(section.image, GUILayout.Height(section.imageHeight == 0 ? (section.image.height < 200 ? section.image.height : 200) : section.imageHeight), GUILayout.Width(EditorGUIUtility.currentViewWidth - 38));
                    }
                    if (!string.IsNullOrEmpty(section.text))
                    {
                        string[] lines = section.text.Split(
                            new string[] { "\\r\\n", "\\r", "\\n" },
                            System.StringSplitOptions.None
                        );
                        for (int i = 0; i < lines.Length; i++)
                        {
                            if (i > 0)
                            {
                                GUILayout.Space(miniSpace);
                            }
                            GUILayout.Label(lines[i], BodyStyle);
                        }
                    }
                    if (!string.IsNullOrEmpty(section.pingSceneElement))
                    {
                        GUILayout.Space(miniSpace);
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
                    if (section.assetLink != null)
                    {
                        GUILayout.Space(miniSpace);
                        string pingText;
                        if (!string.IsNullOrEmpty(section.assetLinkText))
                        {
                            pingText = section.assetLinkText;
                        }
                        else
                        {
                            if (section.assetLinkOpens)
                            {
                                pingText = $"Open {section.assetLink.name} from your Assets";
                            }
                            else
                            {
                                pingText = $"Ping {section.assetLink.name} in your Assets";
                            }
                        }
                        if (GUILayout.Button(pingText))
                        {
                            if (section.assetLinkOpens)
                            {
                                AssetDatabase.OpenAsset(section.assetLink);
                            }
                            else
                            {
                                EditorGUIUtility.PingObject(section.assetLink);
                            }
                        }
                    }
                    if (!string.IsNullOrEmpty(section.settingsPage))
                    {
                        GUILayout.Space(miniSpace);
                        int pos = section.settingsPage.LastIndexOf("/") + 1;
                        if (GUILayout.Button($"Open {section.settingsPage.Substring(pos, section.settingsPage.Length - pos)} Settings"))
                        {
                            SettingsService.OpenProjectSettings(section.settingsPage);
                        }
                    }
                    if (!string.IsNullOrEmpty(section.linkText))
                    {
                        GUILayout.Space(miniSpace);
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