using System.Collections.Generic;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Leap.Unity.GraphicalRenderer {

  public class RendererMeshData : SceneTiedAsset {
    [SerializeField]
    private List<Mesh> meshes = new List<Mesh>();

#if UNITY_EDITOR
    protected override void OnAssetSaved() {
      base.OnAssetSaved();

      //Make sure all our meshes are saved too!
      foreach (var mesh in meshes) {
        if (!AssetDatabase.IsSubAsset(mesh)) {
          AssetDatabase.AddObjectToAsset(mesh, this);
        }
      }
    }
#endif

    private void OnDestroy() {
      foreach (var mesh in meshes) {
        DestroyImmediate(mesh, allowDestroyingAssets: true);
      }
    }

    public void Clear() {
      foreach (var mesh in meshes) {
        DestroyImmediate(mesh, allowDestroyingAssets: true);
      }
      meshes.Clear();
    }

    public void AddMesh(Mesh mesh) {
      meshes.Add(mesh);
#if UNITY_EDITOR
      if (isSavedAsset) {
        AssetDatabase.AddObjectToAsset(mesh, this);
      }
#endif
    }

    public int Count {
      get {
        return meshes.Count;
      }
    }

    public Mesh this[int index] {
      get {
        return meshes[index];
      }
    }
  }
}
