using System.Collections;
using System.Collections.Generic;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

public class RendererMeshData : ScriptableObject {
  [SerializeField]
  private List<Mesh> meshes = new List<Mesh>();

  private void OnDestroy() {
    foreach (var mesh in meshes) {
      DestroyImmediate(mesh, allowDestroyingAssets: true);
    }
  }

#if UNITY_EDITOR
  public void Clear() {
    foreach (var mesh in meshes) {
      DestroyImmediate(mesh, allowDestroyingAssets: true);
    }
    meshes.Clear();
  }

  public void AddMesh(Mesh mesh) {
    meshes.Add(mesh);
    AssetDatabase.AddObjectToAsset(mesh, this);
  }
#endif

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
