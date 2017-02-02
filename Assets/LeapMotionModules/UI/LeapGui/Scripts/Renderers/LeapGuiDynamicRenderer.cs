using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Profiling;
using UnityEngine.Rendering;
#if UNITY_EDITOR
using UnityEditor;
#endif
using Leap.Unity.Query;
using Leap.Unity.Attributes;

public class LeapGuiDynamicRenderer : LeapGuiRenderer,
  ISupportsFeature<LeapGuiMeshFeature> {

  [SerializeField]
  private Shader _shader;


  private List<Mesh> _elementMeshes = new List<Mesh>();

  //Feature lists
  private List<LeapGuiMeshFeature> _meshFeatures = new List<LeapGuiMeshFeature>();
  private List<LeapGuiTextureFeature> _textureFeatures = new List<LeapGuiTextureFeature>();





  public void GetSupportInfo(List<LeapGuiMeshFeature> features, List<FeatureSupportInfo> info) { }


  public override void OnEnableRenderer() {
    throw new NotImplementedException();
  }

  public override void OnDisableRenderer() {
    throw new NotImplementedException();
  }

  public override void OnUpdateRenderer() {
    throw new NotImplementedException();
  }

  public override void OnEnableRendererEditor() {
    throw new NotImplementedException();
  }

  public override void OnDisableRendererEditor() {
    throw new NotImplementedException();
  }

  public override void OnUpdateRendererEditor() {
    //Destroy meshes that will no longer be used
    while (_elementMeshes.Count > gui.elements.Count) {
      DestroyImmediate(_elementMeshes.RemoveLast());
    }

    //Clear out the meshes that remain
    foreach (var mesh in _elementMeshes) {
      mesh.Clear();
    }

    //Create new meshes that need to exist
    while (_elementMeshes.Count < gui.elements.Count) {
      Mesh elementMesh = new Mesh();
      elementMesh.MarkDynamic();
      elementMesh.name = "Generated Element Mesh";
      _elementMeshes.Add(elementMesh);
    }

    if (gui.GetSupportedFeatures(_meshFeatures)) {
      Profiler.BeginSample("Bake Verts");
      bakeVerts();
      Profiler.EndSample();

      Profiler.BeginSample("Bake Colors");
      bakeColors();
      Profiler.EndSample();
    }
  }

  private List<Vector3> _tempVertList = new List<Vector3>();
  private List<int> _tempTriList = new List<int>();
  private void bakeVerts() {
    for (int i = 0; i < gui.elements.Count; i++) {
      _tempVertList.Clear();
      _tempTriList.Clear();

      foreach (var meshFeature in _meshFeatures) {
        var elementData = meshFeature.data[i];

        var topology = MeshCache.GetTopology(elementData.mesh);

        int vertOffset = _tempVertList.Count;
        for (int j = 0; j < topology.tris.Length; j++) {
          _tempTriList.Add(topology.tris[i] + vertOffset);
        }

        _tempVertList.AddRange(topology.verts);
      }

      _elementMeshes[i].SetVertices(_tempVertList);
      _elementMeshes[i].SetTriangles(_tempTriList, 0);
    }
  }

  private List<Color> _tempColorList = new List<Color>();
  private void bakeColors() {
    //If no mesh feature wants colors, don't bake them!
    if (!_meshFeatures.Query().Any(f => f.color)) {
      return;
    }

    for (int i = 0; i < gui.elements.Count; i++) {
      _tempColorList.Clear();

      foreach (var meshFeature in _meshFeatures) {
        var elementData = meshFeature.data[i];
      }
    }

  }




}
