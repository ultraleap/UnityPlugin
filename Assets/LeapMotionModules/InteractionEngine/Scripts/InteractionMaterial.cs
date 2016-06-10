using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif
using System;
using System.IO;

namespace Leap.Unity.Interaction {

  public class InteractionMaterial : ScriptableObject {

    [AttributeUsage(AttributeTargets.Field, AllowMultiple = false)]
    public class ControllerAttribute : Attribute {
      public readonly bool AllowNone;

      public ControllerAttribute(bool allowNone = false) {
        AllowNone = allowNone;
      }
    }

    public enum PhysicMaterialModeEnum {
      NoAction,
      DuplicateExisting,
      Replace
    }

    [Tooltip("How far the object can get from the hand before it is released.")]
    [SerializeField]
    protected float _releaseDistance = 0.15f;

    [Tooltip("What to do with the physic materials when a grasp occurs.")]
    [SerializeField]
    protected PhysicMaterialModeEnum _physicMaterialMode = PhysicMaterialModeEnum.DuplicateExisting;

    [Tooltip("What material to replace with when a grasp occurs.")]
    [SerializeField]
    protected PhysicMaterial _replacementMaterial;

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

    [Controller]
    [SerializeField]
    protected IHoldingPoseController _holdingPoseController;

    [Controller]
    [SerializeField]
    protected IMoveToController _moveToController;

    [Controller(allowNone: true)]
    [SerializeField]
    protected ISuspensionController _suspensionController;

    [Controller(allowNone: true)]
    [SerializeField]
    protected IThrowingController _throwingController;

    [Controller(allowNone: true)]
    [SerializeField]
    protected ILayerController _layerController;

    public IHoldingPoseController CreateHoldingPoseController(InteractionBehaviour obj) {
      return IControllerBase.CreateInstance(obj, _holdingPoseController);
    }

    public IMoveToController CreateMoveToController(InteractionBehaviour obj) {
      return IControllerBase.CreateInstance(obj, _moveToController);
    }

    public ISuspensionController CreateSuspensionController(InteractionBehaviour obj) {
      return IControllerBase.CreateInstance(obj, _suspensionController);
    }

    public IThrowingController CreateThrowingController(InteractionBehaviour obj) {
      return IControllerBase.CreateInstance(obj, _throwingController);
    }

    public ILayerController CreateLayerController(InteractionBehaviour obj) {
      return IControllerBase.CreateInstance(obj, _layerController);
    }

    public bool ContactEnabled {
      get {
        return true;
      }
    }

    public float BrushDisableDistance {
      get {
        return 0.017f;
      }
    }

    public float ReleaseDistance {
      get {
        return _releaseDistance;
      }
    }

    public PhysicMaterialModeEnum PhysicMaterialMode {
      get {
        return _physicMaterialMode;
      }
    }

    public PhysicMaterial ReplacementPhysicMaterial {
      get {
        return _replacementMaterial;
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

#if UNITY_EDITOR
    private const string DEFAULT_ASSET_NAME = "InteractionMaterial.asset";

    [MenuItem("Assets/Create/Interaction Material", priority = 510)]
    private static void createNewBuildSetup() {
      string path = "Assets";

      foreach (UnityEngine.Object obj in Selection.GetFiltered(typeof(UnityEngine.Object), SelectionMode.Assets)) {
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

      material._holdingPoseController = createDefaultAsset<HoldingPoseControllerKabsch>(material);
      material._moveToController = createDefaultAsset<MoveToControllerVelocity>(material);
      material._suspensionController = createDefaultAsset<SuspensionControllerDefault>(material);
      material._throwingController = createDefaultAsset<ThrowingControllerPalmVelocity>(material);

      AssetDatabase.SaveAssets();

      Selection.activeObject = material;
    }

    private static T createDefaultAsset<T>(InteractionMaterial material) where T : ScriptableObject {
      T t = CreateInstance<T>();
      t.hideFlags = HideFlags.HideInHierarchy | HideFlags.HideInInspector;
      AssetDatabase.AddObjectToAsset(t, material);
      return t;
    }
#endif
  }
}
