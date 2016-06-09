using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif
using System;
using System.IO;

namespace Leap.Unity.Interaction {

  public class InteractionMaterial2 : ScriptableObject {

    [AttributeUsage(AttributeTargets.Field, AllowMultiple = false)]
    public class ControllerAttribute : Attribute {
      public readonly bool AllowNone;

      public ControllerAttribute(bool allowNone = false) {
        AllowNone = allowNone;
      }
    }

    [Controller]
    [SerializeField]
    protected IGraspController _graspController;

    [Controller]
    [SerializeField]
    protected IHoldingController _holdingController;

    [Controller]
    [SerializeField]
    protected IPhysicsController _physicsController;

    [Controller(allowNone: true)]
    [SerializeField]
    protected ISuspensionController _suspensionController;

    [Controller(allowNone: true)]
    [SerializeField]
    protected IThrowingController _throwingController;

    [Controller(allowNone: true)]
    [SerializeField]
    protected ILayerController _layerController;

    public IGraspController CreateGraspController(InteractionBehaviour obj) {
      return IControllerBase.CreateInstance(obj, _graspController);
    }

    public IHoldingController CreateHoldingController(InteractionBehaviour obj) {
      return IControllerBase.CreateInstance(obj, _holdingController);
    }

    public IPhysicsController CreatePhysicsController(InteractionBehaviour obj) {
      return IControllerBase.CreateInstance(obj, _physicsController);
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

#if UNITY_EDITOR
    private const string DEFAULT_ASSET_NAME = "InteractionMaterial.asset";

    [MenuItem("Assets/Create/Interaction Material 2", priority = 510)]
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

      InteractionMaterial2 material = CreateInstance<InteractionMaterial2>();
      material._graspController = CreateInstance<GraspControllerDefault>();
      material._holdingController = CreateInstance<HoldingControllerKabsch>();
      material._physicsController = CreateInstance<PhysicsControllerKinematic>();

      //TODO: set defaults

      AssetDatabase.CreateAsset(material, path);
      AssetDatabase.SaveAssets();

      Selection.activeObject = material;
    }
#endif
  }
}
