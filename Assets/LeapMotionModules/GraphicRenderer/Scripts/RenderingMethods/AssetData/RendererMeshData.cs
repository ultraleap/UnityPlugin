using System;
using System.Collections.Generic;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Leap.Unity.GraphicalRenderer {

  [Serializable]
  public class RendererMeshData {
    [SerializeField]
    private List<Mesh> meshes = new List<Mesh>();

    [System.NonSerialized]
    private Queue<Mesh> _tempMeshPool = new Queue<Mesh>();

    private void OnDestroy() {
      foreach (var mesh in meshes) {
        UnityEngine.Object.DestroyImmediate(mesh);
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
        UnityEngine.Object.DestroyImmediate(_tempMeshPool.Dequeue());
      }
    }

    public void AddMesh(Mesh mesh) {
      mesh.hideFlags = HideFlags.HideInHierarchy | HideFlags.HideInInspector;
      meshes.Add(mesh);
    }

    public void RemoveMesh(int index) {
      Mesh mesh = meshes[index];
      meshes.RemoveAt(index);
      UnityEngine.Object.DestroyImmediate(mesh);
    }

    public void Validate(LeapRenderingMethod renderingMethod) {
      for (int i = meshes.Count; i-- != 0;) {
        Mesh mesh = meshes[i];
        if (mesh == null) {
          meshes.RemoveAt(i);
          continue;
        }

        renderingMethod.PreventDuplication(ref mesh);
        meshes[i] = mesh;
      }
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
      set {
        meshes[index] = value;
      }
    }
  }
}
