using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif
using System.IO;

namespace Leap.Unity.Interaction {

  public class InteractionMaterial2 : ScriptableObject {

    [SerializeField]
    protected IGraspSolver _graspSolver;

    [SerializeField]
    protected IPhysicsDriver _physicsDriver;

    [SerializeField]
    protected ISuspensionController _suspensionHandler;

    [SerializeField]
    protected IThrowingHandler _throwingHandler;

#if UNITY_EDITOR
    private const string DEFAULT_ASSET_NAME = "InteractionMaterial.asset";

    [MenuItem("Assets/Create/Interaction Material 2", priority = 510)]
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

      InteractionMaterial2 material = CreateInstance<InteractionMaterial2>();
      AssetDatabase.CreateAsset(material, path);
      AssetDatabase.SaveAssets();

      Selection.activeObject = material;
    }
#endif
  }
}
