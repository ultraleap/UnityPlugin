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
      public readonly Type DefaultType;

      public ControllerAttribute(Type defaultType, bool allowNone) {
        AllowNone = allowNone;
        DefaultType = defaultType;
      }
    }

    [Controller(typeof(void), allowNone: false)]
    [SerializeField]
    protected IGraspController _graspController;

    [Controller(typeof(void), allowNone: false)]
    [SerializeField]
    protected IHoldingController _holdingController;

    [Controller(typeof(void), allowNone: false)]
    [SerializeField]
    protected IPhysicsController _physicsController;

    [Controller(typeof(void), allowNone: true)]
    [SerializeField]
    protected ISuspensionController _suspensionController;

    [Controller(typeof(void), allowNone: true)]
    [SerializeField]
    protected IThrowingController _throwingController;

    [Controller(typeof(void), allowNone: true)]
    [SerializeField]
    protected ILayerController _layerController;

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
      AssetDatabase.CreateAsset(material, path);
      AssetDatabase.SaveAssets();

      Selection.activeObject = material;
    }
#endif
  }
}
