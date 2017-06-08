/******************************************************************************
 * Copyright (C) Leap Motion, Inc. 2011-2017.                                 *
 * Leap Motion proprietary and  confidential.                                 *
 *                                                                            *
 * Use subject to the terms of the Leap Motion SDK Agreement available at     *
 * https://developer.leapmotion.com/sdk_agreement, or another agreement       *
 * between Leap Motion and you, your company or other organization.           *
 ******************************************************************************/

using UnityEngine;
using System.Collections.Generic;

namespace Leap.Unity.DetectionExamples {

  [ExecuteInEditMode]
  public class MengerSponge : MonoBehaviour {

    [SerializeField]
    private int _rendererLod = 2;

    [SerializeField]
    private int _subMeshLod = 3;

    [SerializeField]
    private Material _material;

    [SerializeField]
    private bool _overrideShadowDistance = false;

    private List<GameObject> _renderers = new List<GameObject>();
    private Mesh _subMesh = null;

    void OnValidate() {
      _rendererLod = Mathf.Clamp(_rendererLod, 1, 3);
      _subMeshLod = Mathf.Clamp(_subMeshLod, 1, 3);
      _subMesh = null;
    }

    void Update() {
      if (_overrideShadowDistance && Application.isPlaying) {
        QualitySettings.shadowDistance = transform.lossyScale.x * 10;
      }

      if (_subMesh == null) {
        for (int i = 0; i < _renderers.Count; i++) {
          DestroyImmediate(_renderers[i]);
        }
        _renderers.Clear();

        _subMesh = generateMengerMesh(_subMeshLod);

        int size = Mathf.RoundToInt(Mathf.Pow(3, _rendererLod));
        for (int x = 0; x < size; x++) {
          for (int y = 0; y < size; y++) {
            for (int z = 0; z < size; z++) {
              if (isSpaceFilled(x, y, z, size / 3)) {
                GameObject subRenderer = new GameObject("MengerPiece");
                subRenderer.hideFlags = HideFlags.DontSave | HideFlags.NotEditable;
                subRenderer.transform.parent = transform;
                subRenderer.transform.localPosition = new Vector3(x, y, z) / size + Vector3.one * 0.5f / size - Vector3.one * 0.5f;
                subRenderer.transform.localRotation = Quaternion.identity;
                subRenderer.transform.localScale = Vector3.one / size;

                subRenderer.AddComponent<MeshFilter>().mesh = _subMesh;
                subRenderer.AddComponent<MeshRenderer>().sharedMaterial = _material;
                _renderers.Add(subRenderer);
              }
            }
          }
        }
      }
    }

    private Mesh generateMengerMesh(int lod) {
      Mesh mesh = new Mesh();
      mesh.name = "MengerMesh";

      int size = Mathf.RoundToInt(Mathf.Pow(3, _subMeshLod));

      bool[,,] _isFilled = new bool[size, size, size];
      for (int x = 0; x < size; x++) {
        for (int y = 0; y < size; y++) {
          for (int z = 0; z < size; z++) {
            if (isSpaceFilled(x, y, z, size / 3)) {
              _isFilled[x, y, z] = true;
            }
          }
        }
      }

      List<Vector3> verts = new List<Vector3>();
      List<int> tris = new List<int>();

      float quadRadius = 0.5f / size;

      for (int x = 0; x < size; x++) {
        for (int y = 0; y < size; y++) {
          for (int z = 0; z < size; z++) {
            if (_isFilled[x, y, z]) {
              Vector3 position = new Vector3(x, y, z) / size + Vector3.one * quadRadius - Vector3.one * 0.5f;

              if (x == 0 || !_isFilled[x - 1, y, z]) {
                addQuad(verts, tris, position + Vector3.left * quadRadius, Vector3.forward, Vector3.up, quadRadius);
              }
              if (x == size - 1 || !_isFilled[x + 1, y, z]) {
                addQuad(verts, tris, position + Vector3.right * quadRadius, Vector3.up, Vector3.forward, quadRadius);
              }

              if (y == 0 || !_isFilled[x, y - 1, z]) {
                addQuad(verts, tris, position + Vector3.down * quadRadius, Vector3.right, Vector3.forward, quadRadius);
              }
              if (y == size - 1 || !_isFilled[x, y + 1, z]) {
                addQuad(verts, tris, position + Vector3.up * quadRadius, Vector3.forward, Vector3.right, quadRadius);
              }

              if (z == 0 || !_isFilled[x, y, z - 1]) {
                addQuad(verts, tris, position + Vector3.back * quadRadius, Vector3.up, Vector3.right, quadRadius);
              }
              if (z == size - 1 || !_isFilled[x, y, z + 1]) {
                addQuad(verts, tris, position + Vector3.forward * quadRadius, Vector3.right, Vector3.up, quadRadius);
              }
            }
          }
        }
      }

      List<Vector2> uvs = new List<Vector2>();
      for (int i = 0; i < verts.Count; i++) {
        uvs.Add(verts[i]);
      }

      mesh.SetVertices(verts);
      mesh.SetUVs(0, uvs);
      mesh.SetIndices(tris.ToArray(), MeshTopology.Triangles, 0);
      mesh.RecalculateNormals();
      mesh.RecalculateBounds();

      mesh.UploadMeshData(true);

      return mesh;
    }

    private void addQuad(List<Vector3> verts, List<int> tris, Vector3 center, Vector3 axisA, Vector3 axisB, float radius) {
      tris.Add(verts.Count + 0);
      tris.Add(verts.Count + 1);
      tris.Add(verts.Count + 2);

      tris.Add(verts.Count + 0);
      tris.Add(verts.Count + 2);
      tris.Add(verts.Count + 3);

      verts.Add(center + axisA * radius + axisB * radius);
      verts.Add(center - axisA * radius + axisB * radius);
      verts.Add(center - axisA * radius - axisB * radius);
      verts.Add(center + axisA * radius - axisB * radius);
    }

    private bool isSpaceFilled(int x, int y, int z, int size) {
      if (size == 1) {
        return spaceFilledBaseCase(x, y, z);
      }

      if (!spaceFilledBaseCase(x / size, y / size, z / size)) {
        return false;
      }

      x -= (x / size) * size;
      y -= (y / size) * size;
      z -= (z / size) * size;

      return isSpaceFilled(x, y, z, size / 3);
    }

    private bool spaceFilledBaseCase(int x, int y, int z) {
      int ones = 0;
      if (x == 1) ones++;
      if (y == 1) ones++;
      if (z == 1) ones++;
      return ones < 2;
    }
  }
}
