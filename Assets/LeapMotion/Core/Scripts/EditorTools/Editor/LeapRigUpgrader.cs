using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Leap.Unity {

  [InitializeOnLoad]
  public static class LeapRigUpgrader {

    private static LeapRigUpgraderWindow _upgraderWindow = null;
    private enum SceneScanStatus { NotScanned, ContainsOldRigs, NoOldRigs }
    private static SceneScanStatus _currentSceneScanStatus = SceneScanStatus.NotScanned;

    static LeapRigUpgrader() {
      SceneManager.activeSceneChanged += onActiveSceneChanged;

      _currentSceneScanStatus = SceneScanStatus.NotScanned;
    }

    private static void onActiveSceneChanged(Scene current, Scene next) {
      _currentSceneScanStatus = SceneScanStatus.NotScanned;
    }

    public static GUIStyle boldLabel {
      get { return EditorStyles.boldLabel; }
    }

    private static GUIStyle _backingWrapLabel;
    public static GUIStyle wrapLabel {
      get {
        if (_backingWrapLabel == null) {
          _backingWrapLabel = new GUIStyle(EditorStyles.label);
          _backingWrapLabel.wordWrap = true;
        }
        return _backingWrapLabel;
      }
    }

    [LeapProjectCheck("Core", 0)]
    private static bool drawRigUpgraderCheckGUI() {
      EditorGUILayout.LabelField("Leap Rig Upgrader", boldLabel);
      
      EditorGUILayout.LabelField(
        "If you have upgraded from Core version 4.3.4 or earlier to Core version 4.4 or "
      + "later, you can check the currently-open scene for out-of-date rigs. Instances of "
      + "older Leap Rig prefabs can be automatically upgraded to match the updated rig, "
      + "which is much simpler.",
        wrapLabel);

      EditorGUILayout.Space();

      using (new EditorGUILayout.HorizontalScope()) {
        if (GUILayout.Button(new GUIContent("Scan Current Scene",
          "Scan the currently open scene for old Leap rigs."), GUILayout.MaxWidth(200f))) {
          if (_upgraderWindow == null) {
            _upgraderWindow = LeapRigUpgraderWindow.OpenRigUpgraderWindow();
          }
          if (_upgraderWindow != null) {
            var oldRigsDetected = _upgraderWindow.ScanCurrentSceneForOldLeapRigs();
            if (oldRigsDetected) {
              _currentSceneScanStatus = SceneScanStatus.ContainsOldRigs;
              _upgraderWindow.Focus();
            }
            else {
              _currentSceneScanStatus = SceneScanStatus.NoOldRigs;
              _upgraderWindow.Close();
            }
          }
        }

        string scanText;
        switch (_currentSceneScanStatus) {
          case SceneScanStatus.ContainsOldRigs:
            scanText = "Scene contained old Leap rigs as of the last scan.";
            break;
          case SceneScanStatus.NoOldRigs:
            scanText = "Scene contained no old Leap rigs as of the last scan.";
            break;
          case SceneScanStatus.NotScanned: default:
            scanText = "Scene not yet scanned.";
            break;
        }
        EditorGUILayout.LabelField(scanText);
      }

      EditorGUILayout.Space();

      return true;
    }

  }

  public class LeapRigUpgraderWindow : EditorWindow {

    private const string WINDOW_TITLE = "Leap Rig Upgrader";
    private static readonly Vector2 WINDOW_MIN_SIZE = new Vector2(500f, 500f);

    public static LeapRigUpgraderWindow OpenRigUpgraderWindow() {
      var window = (LeapRigUpgraderWindow)GetWindow(typeof(LeapRigUpgraderWindow),
        utility: true, title: WINDOW_TITLE, focus: true);
      window.name = "Leap Motion Unity Modules Window";
      window.minSize = WINDOW_MIN_SIZE;
      return window;
    }

    #region Memory

    private List<Transform> _backingTransformsBuffer = null;
    private List<Transform> _transformsBuffer {
    get {
        if (_backingTransformsBuffer == null)
          _backingTransformsBuffer = new List<Transform>();
        return _backingTransformsBuffer;
      }
    }

    private List<OldRigHierarchy> _backingOldRigs = null;
    private List<OldRigHierarchy> _oldRigs {
      get {
        if (_backingOldRigs == null) {
          _backingOldRigs = new List<OldRigHierarchy>();
        }
        return _backingOldRigs;
      }
    }

    #endregion

    /// <summary>
    /// Returns true if old rigs were (potentially) found. Returns false if none were
    /// found. If this method returns true, you can Show() the window instance to display
    /// scanned details and auto-upgrade options to the user.
    /// </summary>
    public bool ScanCurrentSceneForOldLeapRigs() {
      _oldRigs.Clear();

      var currentScene = SceneManager.GetActiveScene();
      var rootObjs = currentScene.GetRootGameObjects();
      foreach (var rootObj in rootObjs) {
        rootObj.transform.GetComponentsInChildren(_transformsBuffer);
        foreach (var transform in _transformsBuffer) {
          var oldRig = OldRigHierarchy.DetectFor(transform);
          if (oldRig != null) {
            _oldRigs.Add(oldRig);
          }
        }
        _transformsBuffer.Clear();
      }

      return _oldRigs.Count > 0;
    }

    public class OldRigHierarchy {
      private const string OLD_RIG_ROOT_NAME = "LMHeadMountedRig";

      public Transform rigTransform;             // -> rig root; no change necessary
      public Transform cameraTransform;          // -> lose some components, gain one
      public Transform leapSpaceTransform;       // -> remove
      public Transform lhcTransform;             // -> remove
      public Transform handModelParentTransform; // -> HandModelManager

      /// <summary>
      /// Detects if the argument Transform is the _rig root_ of an old Leap rig, in
      /// which case an OldRigHierarchy description is returned, otherwise this method
      /// returns null.
      /// </summary>
      public static OldRigHierarchy DetectFor(Transform transform) {
        var didDetectOldRig = false;
        var scannedRig = new OldRigHierarchy();

        var name = transform.name;
        if (name.Equals(OLD_RIG_ROOT_NAME)) {
          scannedRig.rigTransform = transform;
          didDetectOldRig = true;
        }

        if (didDetectOldRig) {
          return scannedRig;
        }
        else {
          return null;
        }
      }
    }

    private void OnGUI() {
      var boldLabel = LeapRigUpgrader.boldLabel;
      var wrapLabel = LeapRigUpgrader.wrapLabel;

      EditorGUILayout.Space();

      var singleRig = _oldRigs.Count == 1;
      EditorGUILayout.LabelField(
        "Detected " + _oldRigs.Count + " old rig" + (singleRig ? "" : "s") + ".", wrapLabel);

      EditorGUILayout.Space();
      
      foreach (var oldRig in _oldRigs) {
        using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox)) {
          EditorGUILayout.LabelField(getName(oldRig.rigTransform), boldLabel,
            GUILayout.ExpandWidth(false));

          using (new EditorGUILayout.HorizontalScope()) {
            EditorGUILayout.LabelField("Rig Transform: ",
              GUILayout.ExpandWidth(false));
            EditorGUI.BeginDisabledGroup(true);
            EditorGUILayout.ObjectField(oldRig.rigTransform, typeof(Transform), true,
              GUILayout.ExpandWidth(true));
            EditorGUI.EndDisabledGroup();
          }
        }
        EditorGUILayout.Space();
      }
    }

    private string getName(Transform t) {
      if (t == null) return "";
      return t.name;
    }
    
  }

}