using UnityEditor;
using UnityEngine;
using Leap.Unity;

namespace Leap.Unity {

  public class LeapUnityWindow : EditorWindow {

    #region Settings & Init

    private const string WINDOW_TITLE = "Leap Motion Unity Modules";
    private static readonly Vector2 WINDOW_MIN_SIZE = new Vector2(600f, 600f);

    [MenuItem("Window/Leap Motion")]
    private static void Init() {
      var window = (LeapUnityWindow)GetWindow(typeof(LeapUnityWindow),
        utility: true, title: WINDOW_TITLE, focus: true);
      window.name = "Leap Motion Unity Modules Window";
      window.minSize = WINDOW_MIN_SIZE;
    }

    #endregion

    #region Resources

    private string leapLogoResourceName {
      get {
        if (EditorGUIUtility.isProSkin) return "LM_Logo_White";
        else return "LM_Logo_Black";
      }
    }

    private Texture2D _backingLeapTex = null;
    private Texture2D leapTex {
      get {
        if (_backingLeapTex == null) {
          _backingLeapTex = EditorResources.Load<Texture2D>(leapLogoResourceName);
        }
        return _backingLeapTex;
      }
    }

    private GUISkin _backingWindowSkin;
    private GUISkin _windowSkin {
      get {
        if (_backingWindowSkin == null) {
          _backingWindowSkin = EditorGUIUtility.GetBuiltinSkin(EditorSkin.Scene);
        }
        return _backingWindowSkin;
      }
    }

    #endregion

    #region Window State

    private int _tab = 0;
    private Vector2 _scrollPosition = Vector2.zero;

    #endregion

    #region OnGUI

    private void OnGUI() {
      var origSkin = GUI.skin;
      try {
        GUI.skin = _windowSkin;

        drawGUI();
      }
      finally {
        GUI.skin = origSkin;
      }
    }

    private void drawGUI() {
      var boxStyle = _windowSkin.box;
      GUILayout.BeginVertical();

      // Logo.
      var logoStyle = _windowSkin.box;
      logoStyle.fixedHeight = 150;
      logoStyle.stretchWidth = true;
      logoStyle.alignment = TextAnchor.MiddleCenter;
      logoStyle.margin = new RectOffset(0, 0, top: 20, bottom: 20);
      GUILayout.Box(new GUIContent(leapTex), logoStyle, GUILayout.ExpandWidth(true),
        GUILayout.MaxHeight(150f));

      // Title.
      //var titleStyle = new GUIStyle(_windowSkin.label);
      //titleStyle.fontSize = 20;
      //titleStyle.margin = new RectOffset(0, 0, 0, 10);
      ////titleStyle.stretchHeight = false;
      //var titleContent = new GUIContent("Leap Motion Unity Modules");
      //GUILayout.Label(titleContent, titleStyle);

      // Window tabs.
      _tab = GUILayout.Toolbar(_tab, new string[] { "Project Checks", "Preferences" });
      _scrollPosition = GUILayout.BeginScrollView(_scrollPosition,
        GUILayout.ExpandWidth(true), GUILayout.ExpandHeight(true));
      switch (_tab) {
        case 0:
          LeapProjectChecks.DrawProjectChecksGUI();
          break;
        case 1:
          LeapPreferences.DrawPreferencesGUI();
          break;
        default:
          _tab = 0;
          break;
      }
      GUILayout.EndScrollView();

      GUILayout.EndVertical();
    }

    #endregion

  }

}
