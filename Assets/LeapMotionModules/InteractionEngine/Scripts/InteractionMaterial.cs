#if UNITY_EDITOR
using UnityEditor;
#endif
using System.IO;
using UnityEngine;

namespace Leap.Unity.Interaction {

  public class InteractionMaterial : ScriptableObject {

    public enum GraspMethodEnum {
      Velocity,
      Kinematic
    }

    [Header("Contact Settings")]
    [Tooltip("Should a hand be able to impart pushing forces to this object.")]
    [SerializeField]
    protected bool _contactEnabled = true;

    [Tooltip("Depth before brushes are disabled.")]
    [SerializeField]
    protected float _brushDisableDistance = 0.017f;

    [Header("Grasp Settings")]
    [Tooltip("Should a hand be able to grasp objects using this material.")]
    [SerializeField]
    protected bool _graspingEnabled = true;

    [Tooltip("What method should be used to move the object when it is grasped.")]
    [SerializeField]
    protected GraspMethodEnum _graspMethod = GraspMethodEnum.Velocity;
    

    [Tooltip("How far the object can get from the hand before it is released.")]
    [SerializeField]
    protected float _releaseDistance = 0.15f;

    [Tooltip("How fast the object can move to try to get to the hand.")]
    [SerializeField]
    protected float _maxVelocity = 3;

    [Tooltip("How strong the attraction is from the hand to the object when being held.  At strength 1 the object " +
             "will try to move 100% of the way to the hand every frame.")]
    [SerializeField]
    protected AnimationCurve _strengthByDistance = new AnimationCurve(new Keyframe(0.0f, 1.0f, 0.0f, 0.0f),
                                                                      new Keyframe(0.02f, 0.2f, 0.0f, 0.0f));

    [Tooltip("A curve used to calculate a multiplier of the throwing velocity.  Maps original velocity to multiplier.")]
    [SerializeField]
    protected AnimationCurve _throwingVelocityCurve = new AnimationCurve(new Keyframe(0.0f, 1.0f, 0.0f, 0.0f),
                                                                         new Keyframe(1.0f, 1.0f, 0.0f, 0.0f),
                                                                         new Keyframe(2.0f, 1.5f, 0.0f, 0.0f));

    [Header("Suspension Settings")]
    [Tooltip("Can objects using this material be suspended when a grasping hand becomes untracked.")]
    [SerializeField]
    protected bool _suspensionEnabled = true;

    [Tooltip("How long can objects using this material be suspended.  If a hand remains untracked for mor than this time then " +
             "the object will end suspension.")]
    [SerializeField]
    protected float _maxSuspensionTime = 1;

    [Tooltip("Whether or not objects using this material have their graphical anchor disabled when they become suspended.")]
    [SerializeField]
    protected bool _hideObjectOnSuspend = true;

    [Header("Warp Settings")]
    [Tooltip("Can objects using this material warp the graphical anchor through time to reduce percieved latency.")]
    [SerializeField]
    protected bool _warpingEnabled = true;

    [Tooltip("The amount of warping to perform based on the distance between the actual position and the graphical position.")]
    [SerializeField]
    protected AnimationCurve _warpCurve = new AnimationCurve(new Keyframe(0.0f, 1.0f, 0.0f, 0.0f),
                                                             new Keyframe(0.02f, 0.0f, 0.0f, 0.0f));

    [Tooltip("How long it takes for the graphical anchor to return to the origin after a release.")]
    [SerializeField]
    protected float _graphicalReturnTime = 0.25f;

    [Header("Layer Settings")]
    [Tooltip("Should objects using this material use custom layers instead of the ones provided by the Interaction Manager.")]
    [SerializeField]
    protected bool _useCustomLayers = false;

    [SerializeField]
    protected SingleLayer _interactionLayer = 0;

    [SerializeField]
    protected SingleLayer _interactionNoClipLayer = 0;

    protected virtual void OnValidate() {
      _brushDisableDistance = Mathf.Max(0, _brushDisableDistance);
      _releaseDistance = Mathf.Max(0, _releaseDistance);
      _maxVelocity = Mathf.Max(0, _maxVelocity);
      _maxSuspensionTime = Mathf.Max(0, _maxSuspensionTime);
      _graphicalReturnTime = Mathf.Max(0, _graphicalReturnTime);
    }

    public bool ContactEnabled {
      get {
        return _contactEnabled;
      }
    }

    public float BrushDisableDistance {
      get {
        return _brushDisableDistance;
      }
    }

    public bool GraspingEnabled {
      get {
        return _graspingEnabled;
      }
    }

    public GraspMethodEnum GraspMethod {
      get {
        return _graspMethod;
      }
    }

    public float ReleaseDistance {
      get {
        return _releaseDistance;
      }
    }

    public float MaxVelocity {
      get {
        return _maxVelocity;
      }
    }

    public AnimationCurve StrengthByDistance {
      get {
        return _strengthByDistance;
      }
    }

    public AnimationCurve ThrowingVelocityCurve {
      get {
        return _throwingVelocityCurve;
      }
    }

    public bool SuspensionEnabled {
      get {
        return _suspensionEnabled;
      }
    }

    public float MaxSuspensionTime {
      get {
        return _maxSuspensionTime;
      }
    }

    public bool HideObjectOnSuspend {
      get {
        return _hideObjectOnSuspend;
      }
    }

    public bool WarpingEnabled {
      get {
        return _warpingEnabled;
      }
    }

    public AnimationCurve WarpCurve {
      get {
        return _warpCurve;
      }
    }

    public float GraphicalReturnTime {
      get {
        return _graphicalReturnTime;
      }
    }

    public bool UseCustomLayers {
      get {
        return _useCustomLayers;
      }
    }

    public int InteractionLayer {
      get {
        return _interactionLayer;
      }
    }

    public int InteractionNoClipLayer {
      get {
        return _interactionNoClipLayer;
      }
    }

#if UNITY_EDITOR
    private const string DEFAULT_ASSET_NAME = "InteractionMaterial.asset";

    [MenuItem("Assets/Create/Interaction Material", priority = 510)]
    private static void createNewBuildSetup() {
      string path = "Assets";

      foreach (Object obj in Selection.GetFiltered(typeof(Object), SelectionMode.Assets)) {
        path = AssetDatabase.GetAssetPath(obj);
        if (!string.IsNullOrEmpty(path) && File.Exists(path)) {
          path = Path.GetDirectoryName(path);
          break;
        }
      }

      path = Path.Combine(path, DEFAULT_ASSET_NAME);
      path = AssetDatabase.GenerateUniqueAssetPath(path);

      InteractionMaterial material = CreateInstance<InteractionMaterial>();
      AssetDatabase.CreateAsset(material, path);
      AssetDatabase.SaveAssets();

      Selection.activeObject = material;
    }
#endif
  }
}
