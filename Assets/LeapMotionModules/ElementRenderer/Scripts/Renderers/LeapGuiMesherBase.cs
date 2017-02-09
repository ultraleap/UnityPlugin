using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using Leap.Unity.Query;

public abstract class LeapGuiMesherBase : LeapGuiRenderer {

  protected List<Mesh> _meshes = new List<Mesh>();
  protected Material _material;

  protected LeapGuiElement _currElement;
  protected int _currIndex;
  protected Mesh _currMesh;

  protected List<Vector3> _verts = new List<Vector3>();
  protected List<Vector3> _normals = new List<Vector3>();
  protected List<int> _tris = new List<int>();
  protected List<Color> _colors = new List<Color>();
  protected List<Vector4>[] _uvs = new List<Vector4>[4] {
    new List<Vector4>(),
    new List<Vector4>(),
    new List<Vector4>(),
    new List<Vector4>()
  };

  private bool _doesRequireColors;
  private bool _doesRequireUv3;
  private List<UVChannelFlags> _requiredUvChannels;

  //Feature lists
  protected List<LeapGuiMeshFeature> _meshFeatures = new List<LeapGuiMeshFeature>();
  protected List<LeapGuiTextureFeature> _textureFeatures = new List<LeapGuiTextureFeature>();
  protected List<LeapGuiSpriteFeature> _spriteFeatures = new List<LeapGuiSpriteFeature>();
  protected List<LeapGuiBlendShapeFeature> _blendShapeFeatures = new List<LeapGuiBlendShapeFeature>();

  //#### Textures ####
  private Dictionary<UVChannelFlags, Rect[]> _packedRects;

  protected virtual void beginMesh() {
    _currMesh = new Mesh();
    _currMesh.name = "Procedural Gui Mesh";
    _currMesh.hideFlags = HideFlags.None;

    _verts.Clear();
    _normals.Clear();
    _tris.Clear();
    _colors.Clear();
    for (int i = 0; i < 4; i++) {
      _uvs[i].Clear();
    }
  }

  protected virtual void applyMesh() {
    _currMesh.SetVertices(_verts);
    _currMesh.SetTriangles(_tris, 0);

    if (_normals.Count == _verts.Count) {
      _currMesh.SetNormals(_normals);
    }

    if (_colors.Count == _verts.Count) {
      _currMesh.SetColors(_colors);
    }

    for (int i = 0; i < 4; i++) {
      _currMesh.SetUVsAuto(i, _uvs[i]);
    }
  }

  protected virtual void buildElement() {
    foreach (var meshFeature in _meshFeatures) {
      var meshData = meshFeature.data[_currIndex];
      if (meshData.mesh == null) continue;

      buildTopology(meshData);

      if (_doesRequireColors) {
        buildColors(meshFeature, meshData);
      }

      foreach (var channel in _requiredUvChannels) {
        buildUvs(channel, meshData);
      }

      buildUv3(meshData);
    }

    foreach (var blendShapeFeature in _blendShapeFeatures) {
      buildBlendShapes(blendShapeFeature.data[_currIndex]);
    }
  }

  protected virtual void buildTopology(LeapGuiMeshData meshData) {
    var topology = MeshCache.GetTopology(meshData.mesh);

    int vertOffset = _verts.Count;
    for (int i = 0; i < topology.tris.Length; i++) {
      _tris.Add(topology.tris[i] = vertOffset);
    }

    for (int i = 0; i < topology.verts.Length; i++) {
      _verts.Add(elementVertToMeshVert(topology.verts[i]));
    }
  }

  protected virtual void buildColors(LeapGuiMeshFeature feature, LeapGuiMeshData meshData) {
    Color totalTint = feature.tint * meshData.tint;
    MeshCache.GetColors(meshData.mesh).Query().Select(c => c * totalTint).AppendList(_colors);
  }

  protected virtual void buildUvs(UVChannelFlags channel, LeapGuiMeshData meshData) {
    var uvs = MeshCache.GetUvs(meshData.mesh, channel);

    Rect[] atlasedUvs;
    if (_packedRects != null &&
       _packedRects.TryGetValue(channel, out atlasedUvs) &&
       (meshData.remappableChannels & channel) != 0) {
      MeshUtil.RemapUvs(uvs, atlasedUvs[_currIndex]);
    }

    _uvs[channel.Index()].AddRange(uvs);
  }

  protected virtual void buildUv3(LeapGuiMeshData meshData) {
    _uvs[3].Append(meshData.mesh.vertexCount, new Vector4(0, 0, 0, _currIndex));
  }

  protected virtual void buildBlendShapes(LeapGuiBlendShapeData blendShapeData) {
    var shape = blendShapeData.blendShape;

    int offset = _verts.Count - shape.vertexCount;

    var verts = shape.vertices;
    for (int i = 0; i < verts.Length; i++) {
      Vector3 shapeVert = verts[i];
      Vector3 delta = elementVertToMeshVert(shapeVert) - _verts[i + offset];

      Vector4 currUv = _uvs[3][i + offset];
      currUv.x = delta.x;
      currUv.y = delta.y;
      currUv.z = delta.z;
      _uvs[3][i + offset] = currUv;
    }
  }

  protected abstract Vector3 elementVertToMeshVert(Vector3 vertex);

}
