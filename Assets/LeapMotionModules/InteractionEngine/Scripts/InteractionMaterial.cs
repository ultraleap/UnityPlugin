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
    protected bool _enableContact = true;

    [Tooltip("A curve used to calculate a multiplier of the throwing velocity.  Maps original velocity to multiplier.")]
    [SerializeField]
    protected AnimationCurve _throwingVelocityCurve = new AnimationCurve(new Keyframe(0.0f, 1.0f, 0.0f, 0.0f),
                                                                         new Keyframe(1.0f, 1.0f, 0.0f, 0.0f),
                                                                         new Keyframe(2.0f, 1.5f, 0.0f, 0.0f));

    [Header("Grasp Settings")]
    [SerializeField]
    protected GraspMethodEnum _graspMethod = GraspMethodEnum.Velocity;

    [Tooltip("How long it takes for the graphical anchor to return to the origin after a release.")]
    [SerializeField]
    protected float _graphicalReturnTime = 0.25f;

    [Tooltip("How far the object can get from the hand before it is released.")]
    [SerializeField]
    protected float _releaseDistance = 0.15f;

    [Tooltip("How fast the object can move to try to get to the hand.")]
    [SerializeField]
    protected float _maxVelocity = 1;

    [Tooltip("How strong the attraction is from the hand to the object when being held.  At strength 1 the object " +
             "will try to move 100% of the way to the hand every frame.")]
    [Range(0, 1)]
    [SerializeField]
    protected float _followStrength = 1.0f;



    public bool EnableContact {
      get {
        return _enableContact;
      }
    }

    public AnimationCurve ThrowingVelocityCurve {
      get {
        return _throwingVelocityCurve;
      }
    }

    public GraspMethodEnum GraspMethod {
      get {
        return _graspMethod;
      }
    }

    public float GraphicalReturnTime {
      get {
        return _graphicalReturnTime;
      }
    }

    public float MaxVelocity {
      get {
        return _maxVelocity;
      }
    }

    public float FollowStrength {
      get {
        return _followStrength;
      }
    }

#if UNITY_EDITOR
    private const string DEFAULT_ASSET_NAME = "InteractionMaterial.asset";

    [MenuItem("Assets/Create/Interaction Material", priority = 90)]
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
