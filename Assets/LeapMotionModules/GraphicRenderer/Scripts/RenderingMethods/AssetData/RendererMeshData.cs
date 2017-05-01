using System.Collections.Generic;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Leap.Unity.GraphicalRenderer {

  public class RendererMeshData : SceneTiedAsset {
    [SerializeField]
    private List<Mesh> meshes = new List<Mesh>();

    [System.NonSerialized]
    private Queue<Mesh> _tempMeshPool = new Queue<Mesh>();

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
        if (mesh != null) {
          mesh.Clear();
          _tempMeshPool.Enqueue(mesh);
        }
      }
      meshes.Clear();
    }

    public Mesh GetMeshFromPoolOrNew() {
      if (_tempMeshPool.Count > 0) {
        return _tempMeshPool.Dequeue();
      } else {
        return new Mesh();
      }
    }

    public void ClearPool() {
      while (_tempMeshPool.Count > 0) {
        DestroyImmediate(_tempMeshPool.Dequeue(), allowDestroyingAssets: true);
      }
    }

    public void AddMesh(Mesh mesh) {
      mesh.hideFlags = HideFlags.HideInHierarchy | HideFlags.HideInInspector;

      meshes.Add(mesh);
#if UNITY_EDITOR
      if (isSavedAsset && string.IsNullOrEmpty(AssetDatabase.GetAssetPath(mesh))) {
        AssetDatabase.AddObjectToAsset(mesh, this);
      }
#endif
    }

    public void RemoveMesh(int index) {
      Mesh mesh = meshes[index];
      meshes.RemoveAt(index);
      DestroyImmediate(mesh, allowDestroyingAssets: true);
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
