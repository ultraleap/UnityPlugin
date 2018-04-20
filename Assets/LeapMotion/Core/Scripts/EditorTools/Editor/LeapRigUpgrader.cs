using Leap.Unity.Query;
using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Leap.Unity {

  using UnityObject = UnityEngine.Object;

  [InitializeOnLoad]
  public static class LeapRigUpgrader {

    private static LeapRigUpgraderWindow _upgraderWindow = null;
    public enum SceneScanStatus { NotScanned, ContainsOldRigs, NoOldRigsFound }
    private static SceneScanStatus _currentSceneScanStatus = SceneScanStatus.NotScanned;
    public static SceneScanStatus currentSceneScanStatus {
      get {
        return _currentSceneScanStatus;
      }
    }

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
              _currentSceneScanStatus = SceneScanStatus.NoOldRigsFound;
              //_upgraderWindow.Close();
            }
          }
        }

        string scanText;
        switch (_currentSceneScanStatus) {
          case SceneScanStatus.ContainsOldRigs:
            scanText = "Scene contained old Leap rigs as of the last scan.";
            break;
          case SceneScanStatus.NoOldRigsFound:
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

    public static GUIStyle wrapLabel { get { return LeapRigUpgrader.wrapLabel; } }

    private bool _camera_removeMissingScripts = true;

    private void OnGUI() {
      var boldLabel = LeapRigUpgrader.boldLabel;
      var wrapLabel = LeapRigUpgrader.wrapLabel;

      EditorGUILayout.Space();

      if (LeapRigUpgrader.currentSceneScanStatus
            == LeapRigUpgrader.SceneScanStatus.NotScanned) {
        EditorGUILayout.LabelField("Scene has not yet been scanned. Visit the Leap "
          + "Motion SDK window from Window->Leap Motion to begin a scan.",
          LeapRigUpgrader.wrapLabel);
      }

      var singleRig = _oldRigs.Count == 1;
      EditorGUILayout.LabelField("Detected " + _oldRigs.Count + " old rig"
        + (singleRig ? "" : "s") + ".", wrapLabel);

      EditorGUILayout.Space();
      
      foreach (var oldRig in _oldRigs) {
        using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox)) {
          EditorGUILayout.LabelField(getName(oldRig.rigTransform), boldLabel,
            GUILayout.ExpandWidth(false));
          EditorGUILayout.Space();
          
          // Rig Transform.
          drawRigItem("Rig Transform", oldRig.rigTransform, typeof(Transform));
          EditorGUILayout.LabelField(
            "This transform is the root of the detected rig and does not need to be "
          + "modified.", wrapLabel);
          EditorGUILayout.Space();

          // Rig Camera.
          /* TODO:
           *    if (camera_enableDepthBuffer != null) {
                  The EnableDepthBuffer script used to be a part of Leap cameras by default but can cause shader issues in certain situations. If you're not sure you need this component, you should remove it.
                  [x] Remove the EnableDepthBuffer script from the camera.
                  [ ] Don't remove the EnableDepthBuffer script, I'm using it for something.
                } */
          drawRigItem("Rig Camera", oldRig.cameraData.cameraComponent, typeof(Camera));
          var camera_missingScripts = oldRig.cameraData.missingComponentIndices;
          if (camera_missingScripts.Count > 0) {
            drawMissingScriptsSetting(camera_missingScripts.Count,
              ref _camera_removeMissingScripts);
          }

          drawRigItem("(old) LeapSpace: ",
            oldRig.leapSpaceData.leapSpaceTransform, typeof(Transform));
          drawRigItem("(old) LeapHandController",
            oldRig.lhcData.leapServiceProvider, typeof(Transform));

        }
        EditorGUILayout.Space();
      }
    }

    private static void drawRigItem(string label, UnityObject obj, Type objType) {
      using (new EditorGUILayout.HorizontalScope()) {
        EditorGUILayout.LabelField(label, GUILayout.ExpandWidth(false));
        EditorGUI.BeginDisabledGroup(true);
        EditorGUILayout.ObjectField(obj, objType, true, GUILayout.ExpandWidth(true));
        EditorGUI.EndDisabledGroup();
      }
    }

    private static void drawMissingScriptsSetting(int numMissing,
                                                  ref bool removeMissingScriptsFlag) {
      var isPlural = numMissing != 1;
      EditorGUILayout.LabelField(
        numMissing + (isPlural ? " missing scripts were found on this rig. Some of them "
                               : " missing script was found on this rig. It "
        + "may correspond to " + (isPlural ? " scripts that have " : " a script that has ")
        + "been deprecated since Core 4.4."),
        wrapLabel);
      using (new EditorGUILayout.HorizontalScope()) {
        removeMissingScriptsFlag = EditorGUILayout.Toggle(removeMissingScriptsFlag);
        EditorGUILayout.LabelField("Remove missing scripts on this object.",
          wrapLabel);
      }
      using (new EditorGUILayout.HorizontalScope()) {
        bool temp = EditorGUILayout.Toggle(!removeMissingScriptsFlag);
        removeMissingScriptsFlag = !temp;
        EditorGUILayout.LabelField("Don't remove missing scripts on this object.",
          wrapLabel);
      }
    }

    private static string getName(Transform t) {
      if (t == null) return "";
      return t.name;
    }

  }

  /// <summary>
  /// Description of an old Leap VR rig.
  /// </summary>
  /// <remarks>
  /// This is essentially the hierarchy that we're matching to build this object:
  /// 
  /// Standard VR rig (old)
  /// "LMHeadMountedRig" - soft-contain VRHeightOffset -> XRHeightOffset (automatic)
  ///   |
  ///   |- "CenterEyeAnchor" - soft-contain LeapVRCameraControl -> LeapEyeDislocator,
  ///   |    |                 soft-contain Camera,
  ///   |    |                 soft-contain EnableDepthBuffer -> Missing Script
  ///   |    |
  ///   |    `- "LeapSpace" - soft-contain LeapVRTemporalWarping -> Missing Script
  ///   |         |
  ///   |         `- "LeapHandController" - contain LeapHandController -> Missing Script,
  ///   |                                   contain LeapServiceProvider,
  ///   |                                   soft-contain Hand Model Manager
  ///   `- "HandModels"
  ///        |
  ///        `- X handModelTransforms, unmodified.
  ///        
  /// Alternatively, the old rig could be an old pass-through camera rig, which is
  /// more complicated because it has _two_ separate eye cameras:
  /// 
  /// Standard pass-through rig (old)
  /// "LMHeadMountedRig" - soft-contain VRHeightOffset -> XRHeightOffset (automatic)
  ///   |
  ///   |- "LeftEyeAnchor" - soft-contain LeapVRCameraControl -> LeapEyeDislocator,
  ///   |    |               soft-contain Camera,
  ///   |    |               soft-contain EnableDepthBuffer -> Missing Script,
  ///   |    |               contain LeapImageRetriever
  ///   |    |
  ///   |    `- "LeapSpace" - soft-contain TempWarp -> Missing Script
  ///   |         |
  ///   |         |- "LeapHandController" - contain LeapHandController -> Missing Script,
  ///   |         |                         contain LeapServiceProvider
  ///   |         |                         soft-contain Hand Pool
  ///   |         `- "QuadBackground" -> as 'extra' Transform, moved to child of Camera.
  ///   |
  ///   |- "RightEyeAnchor" - contain LeapVRCameraControl -> LeapEyeDislocator
  ///   |                     ^- this on a sibling of the Leap Image Retriever
  ///   |                        indicates extremely high certainty of Image Rig.
  ///   |                     soft-contain Camera,
  ///   |                     soft-contain Enable Depth Buffer -> Missing Script
  ///   |
  ///   `- "HandModels"
  ///        |
  ///        `- X handModelTransforms, unmodified.
  /// 
  /// </remarks>
  public class OldRigHierarchy {

    /// <summary>
    /// Whether this rig can be upgraded. Set after a DetectFor(transform) call.
    /// </summary>
    public bool isUpgradeableOldRig = false;

    /// <summary> 
    /// Whether the scan data indicates the scanned transform is potentially an old
    /// Leap rig. Set after a DetectFor(transform) call.
    /// </summary>
    public bool detectedAsPotentialRig = false;

    /// <summary>
    /// If the rig was detected as a potential rig but not upgradeable, this is the
    /// reason the rig cannot be upgraded. Set after a DetectFor(transform) call.
    /// </summary>
    public string reasonRigCannotBeUpgraded
        = "Rig data has not been collected. Call OldRigHierarchy.DetectFor(transform) to "
        + "scan.";

    /// <summary> The root transform of the old rig hierarchy. </summary>
    public Transform rigTransform;

    /// <summary>
    /// The parent of hand models in this rig that will receive the HandModelManager.
    /// </summary>
    public Transform handModelParentTransform;

    #region Leap Image Rig Data (commented out for now)

    ///// <summary>
    ///// Whether this rig is an image-passthrough rig hierarchy.
    ///// </summary>
    //public bool isImageRigHierarchy = false;

    ///// <summary>
    ///// Only non-null if isImageRigHierarchy is true; returns the cameraTransform.
    ///// </summary>
    //public Transform imageRig_leftCamTransform {
    //  get {
    //    if (!isImageRigHierarchy) return null;
    //    return cameraTransform;
    //  }
    //}
    ///// <summary>
    ///// For use with Image rigs; alias for cameraComponents.
    ///// </summary>
    //public OldCameraData imageRig_leftCamComponents {
    //  get {
    //    return cameraComponents;
    //  }
    //  set {
    //    cameraComponents = value;
    //  }
    //}

    ///// <summary>
    ///// Only expected to have a value if isImageRigHierarchy is true. The right-eye
    ///// Camera transform of an old image rig.
    ///// </summary>
    //public Transform imageRig_rightCamTransform = null;
    ///// <summary>
    ///// Only expected to have a vlaue if isImageRigHierarchy is true. The right-eye
    ///// Camera component data of an old image rig.
    ///// </summary>
    //public OldCameraData imageRig_rightCamComponents = null;

    #endregion

    #region Rig Camera Data & Detection

    [ThreadStatic]
    private static List<OldCameraData> s_cameraDataBuffer = new List<OldCameraData>();

    /// <summary>
    /// Old camera objects that weren't modified at all would have had a 
    /// LeapEyeDislocator (formerly LeapVRCameraControl), a Camera component, and at
    /// least one missing component for the now-removed EnableDepthBuffer script.
    /// </summary>
    public class OldCameraData {
      public Transform cameraTransform;
      public LeapEyeDislocator leapEyeDislocator;
      public Camera cameraComponent;
      public List<int> missingComponentIndices;

      public bool containsCameraComponent { get { return cameraComponent != null; } }
    }
    /// <summary>
    /// The driven camera transform and component info of the rig hierarchy, or that of
    /// the LEFT eye if this rig was an image pass-through rig specifically.
    /// </summary>
    public OldCameraData cameraData;

    /// <summary>
    /// Detects **potential** old rig cameras among rigTransform's direct children and
    /// fills the detectedCameras list with data for each possible rig camera detected.
    /// </summary>
    private static void detectPotentialOldRigCameras(Transform rigTransform,
                                            List<OldCameraData> detectedCameras) {
      foreach (var child in rigTransform.GetChildren()) {
        var eyeDislocator = child.GetComponent<LeapEyeDislocator>();
        var camera = child.GetComponent<Camera>();
        var leapXRProvider = child.GetComponent<LeapXRServiceProvider>();

        if (camera == null) {
          // Definitely no camera detected for this child.
          continue;
        }
        else if (leapXRProvider != null) {
          // A camera with a LeapXRServiceProvider on it is definitely not an old rig!
          // This is probably an up-to-date rig.
          continue;
        }
        else {
          // OK, this could be an old rig camera. Get as much data as possible about it.
          var oldCamData = new OldCameraData();
          oldCamData.cameraTransform = child;
          oldCamData.cameraComponent = camera;
          oldCamData.leapEyeDislocator = eyeDislocator;
          oldCamData.missingComponentIndices = new List<int>();
          // Fill missingComponentIndices.
          // If there's at least one, we'll assume it's the missing EnableDepthBuffer
          // script, but it could be other scripts, so deletion of these is given a
          // checkbox.
          fillMissingComponentIndices(child, oldCamData.missingComponentIndices);

          // Add it to the detected cameras list. We'll process it more later.
          detectedCameras.Add(oldCamData);
        }
      }
    }

    #endregion

    #region Other Rig Data

    public class OldLeapSpaceData {
      public Transform leapSpaceTransform;
      public List<int> missingComponentIndices;

      /// <summary>
      /// Some rigs, like the old image rig or modifications to the old standard rig,
      /// may have non-LeapHandController transforms as children of the LeapSpace
      /// object. These objects should be moved to be children of the Camera directly.
      /// </summary>
      public List<Transform> nonLHCChildTransforms;
    }
    public OldLeapSpaceData leapSpaceData;

    public class OldLeapHandControllerData {
      public Transform lhcTransform;
      public List<int> missingComponentIndices;
      public LeapServiceProvider leapServiceProvider;
      public HandModelManager handModelManager;
      public List<Transform> extraChildTransforms;
    }
    public OldLeapHandControllerData lhcData;

    public class ImageRigData {
      public OldCameraData rightEyeCamData;
    }
    public ImageRigData imageRigData;
    public bool isImageRig { get { return imageRigData != null; } }

    #endregion

    /// <summary>
    /// Detects if the argument Transform is the _rig root_ of an old Leap rig,
    /// modifying the passed-in old rig hierarchy description. Returns true if the
    /// rig was detected as an old rig, false otherwise.
    /// </summary>
    public static bool DetectFor(Transform transform, OldRigHierarchy toFill) {
      if (transform == null) return false;

      var didDetectOldRig = false;
      var scannedRig = toFill;
      scannedRig.rigTransform = transform;

      // Scan for the camera transform in immediate children.
      s_cameraDataBuffer.Clear();
      detectPotentialOldRigCameras(transform, s_cameraDataBuffer);

      if (s_cameraDataBuffer.Count == 0) {
        // No cameras found; this is not an upgradeable old rig.
        return false;
      }
      else {
        // We have a list of children that contain Camera components and potentially
        // other Leap rig-related data, but the check was intentionally loose.

        // Some practical cases to consider, if there was only one camera found:
        // (1) It could be a standard non-image rig, and so is upgradeable!
        // (2) It could be an image rig but with one eye removed (weird).
        //     (2a) If the left eye was removed, the rig is non-upgradeable.
        //     (2b) If the right eye was removed, this rig will look identical to a
        //          non-image rig as in (1) and be upgradeable.
        // (3) It could just be an object with a Camera component beneath it, and be
        //     unrelated to a Leap VR rig. (Looks similar to 2a but with fewer
        //     Leap-related or Missing components.)
        //
        // To confirm it is NOT (3) or (2a), we need to check its children to
        // find the LeapSpace and LeapHandController.

        // Alternatively, if there were two potential cameras found:
        // (1) Optimistically, this could indicate it's an old image rig with a left
        //     and right eye.
        // (2) It could be a stranger construction, like the rig for a custom VR
        //     integration.
        // (3) It could be a standard single-camera non-image rig with another Camera
        //     utilized for some other, distinct purpose.
        // (4) It could just be a transform with two cameras as children and not be
        //     Leap-related!

        // To be an upgradeable Leap Rig, image or otherwise, one of the cameras we've
        // found must have a LeapServiceProvider two transforms down (we don't actually
        // care what transforms are named, since someone could change them without
        // really affecting the structure of the rig):
        //
        // (Camera)
        //   |
        //   `- LeapSpace - Missing Component // optional, formerly LeapVRTemporalWarping
        //        |
        //        `- LeapHandController - LeapServiceProvider // required
        //                                HandModelManager    // optional
        //
        // If there is no LeapServiceProvider two-transforms-down, the rig has been
        // modified too heavily and is no longer auto-upgradeable.
        //
        // Importantly, this condition will hold true if the rig is a non-image rig OR
        // an image rig, and false if we are going to refuse the upgrade. What's more,
        // if this condition holds true, we will be able to collect all the information
        // we need from the rig after scanning through each camera.

        var primaryCamData     = (OldCameraData)null;
        var firstLeapSpaceData = (OldLeapSpaceData)null;
        var firstLHCData       = (OldLeapHandControllerData)null;

        // "Secondary" cameras in the rig are the cameras that don't have LeapSpace or
        // LeapHandController objects (or weren't searched for these if we already
        // found a primary camera).
        // These need to be searched after we find the primary camera to find the
        // right -eye camera in the image rig case.
        var secondaryCamDatas = new List<OldCameraData>();

        for (int i = 0; i < s_cameraDataBuffer.Count; i++) {
          var camData = s_cameraDataBuffer[i];
          var expectingFirstLeapSpaceData = firstLeapSpaceData == null;

          var camTransform = camData.cameraTransform;
          foreach (var child in camTransform.GetChildren()) {
            var expectingFirstLHCData = firstLHCData == null;

            // We're looking for the child that is the LeapSpace transform, only
            // known implicitly by looking at _its_ children.
            foreach (var grandchild in child.GetChildren()) {
              var leapServiceProvider = grandchild.GetComponent<LeapServiceProvider>();

              // We're going to assume the first LeapServiceProvider we find is
              // indicative of an old, upgradeable rig, and construct out the
              // definition from there.
              if (leapServiceProvider != null) {
                firstLHCData = new OldLeapHandControllerData();
                firstLHCData.lhcTransform = grandchild;
                firstLHCData.leapServiceProvider = leapServiceProvider;
                firstLHCData.missingComponentIndices = new List<int>();
                fillMissingComponentIndices(firstLHCData.lhcTransform,
                  firstLHCData.missingComponentIndices);

                firstLHCData.extraChildTransforms = new List<Transform>();
                foreach (var greatGrandchild in grandchild.GetChildren()) {
                  firstLHCData.extraChildTransforms.Add(greatGrandchild);
                }

                firstLHCData.handModelManager
                  = grandchild.GetComponent<HandModelManager>();
                break;
              }
            }

            if (firstLHCData != null && expectingFirstLHCData) {
              firstLeapSpaceData = new OldLeapSpaceData();
              firstLeapSpaceData.leapSpaceTransform = child;
              firstLeapSpaceData.nonLHCChildTransforms = new List<Transform>();
              foreach (var leapSpaceChild in child.GetChildren()) {
                if (leapSpaceChild == firstLHCData.lhcTransform) continue;
                firstLeapSpaceData.nonLHCChildTransforms.Add(leapSpaceChild);
              }
              firstLeapSpaceData.missingComponentIndices = new List<int>();
              fillMissingComponentIndices(child,
                firstLeapSpaceData.missingComponentIndices);
              break;
            }
          }

          if (firstLeapSpaceData != null && expectingFirstLeapSpaceData) {
            // This is our primary camera.
            primaryCamData = camData;
          }
          else {
            // Either we haven't found a LeapSpace yet or we've already found our first.
            // So this is a secondary camera.
            secondaryCamDatas.Add(camData);
          }
        }

        // At the end of this process, we must have all three of these things in order
        // to be an upgradeable rig:
        // (1) a Primary camera, containing
        // (2) a LeapSpace transform, containing
        // (3) a LeapHandController transform

        // If we don't have these things, we can't upgrade
        // the rig! Not having these things, in fact, indicates it's probably not an
        // old Leap rig at all.
        if (primaryCamData == null) {
          scannedRig.isUpgradeableOldRig = false;
          scannedRig.cameraData = null;
          scannedRig.detectedAsPotentialRig = false;
          scannedRig.leapSpaceData = null;
          scannedRig.lhcData = null;
          scannedRig.handModelParentTransform = null;
          scannedRig.reasonRigCannotBeUpgraded = "Scanned transform does not look like "
            + "an old Leap rig.";
          return false;
        }
        else {
          // OK, we can upgrade this rig. Fill it with any remaining data we need.
          didDetectOldRig = true;

          scannedRig.cameraData = primaryCamData;
          scannedRig.detectedAsPotentialRig = true;
          scannedRig.leapSpaceData = firstLeapSpaceData;
          scannedRig.lhcData = firstLHCData;
          scannedRig.isUpgradeableOldRig = true;
          scannedRig.reasonRigCannotBeUpgraded = "";

          // Since we've come this far, we need to attempt to find the hand model
          // parent transform. This will be the first child transform of the rig that
          // contains one or more HandModelBase components.
          // Not having one of these doesn't preclude updating the rig, since we can
          // just make a new transform for this purpose.
          var firstReasonableHandModelTransform = scannedRig.rigTransform.GetChildren()
              .Query().Where(child =>
                child.GetChildren().Query()
                  .Where(gc => gc.GetComponent<HandModelBase>() != null).Count() > 0)
              .FirstOrDefault();
          scannedRig.handModelParentTransform = firstReasonableHandModelTransform;

          // If there is a LeapImageRetriever on the camera, this could well be an
          // old Leap image rig, with two eyes.
          var leapImageRetriever = primaryCamData.cameraTransform
              .GetComponent<LeapImageRetriever>();
          var rightEyeCamData = (OldCameraData)null;
          if (leapImageRetriever != null) {
            // If there is a secondary camera with a LeapEyeDislocator component, we'll
            // assume that's the other (right) eye.
            rightEyeCamData = secondaryCamDatas.Query()
              .Where(cd => cd.cameraTransform.GetComponent<LeapEyeDislocator>() != null)
              .FirstOrDefault();

            if (rightEyeCamData != null) {
              scannedRig.imageRigData = new ImageRigData();
              scannedRig.imageRigData.rightEyeCamData = rightEyeCamData;
            }
          }
        }
      }

      return didDetectOldRig;
    }

    /// <summary>
    /// Detects if the argument Transform is the _rig root_ of an old Leap rig, in
    /// which case an OldRigHierarchy description is returned, otherwise this method
    /// returns null.
    /// </summary>
    public static OldRigHierarchy DetectFor(Transform transform) {
      var scannedRig = new OldRigHierarchy();
      var didDetectOldRig = DetectFor(transform, scannedRig);
      if (!didDetectOldRig) return null;
      return scannedRig;
    }
    
    private static void fillMissingComponentIndices(Transform transform, List<int> toFill) {
      toFill.Clear();
      var tempList = new List<Component>();
      transform.GetComponents(tempList);
      for (int i = 0; i < tempList.Count; i++) {
        var component = tempList[i];
        if (component == null) {
          toFill.Add(i);
        }
      }
    }

  }

}
 