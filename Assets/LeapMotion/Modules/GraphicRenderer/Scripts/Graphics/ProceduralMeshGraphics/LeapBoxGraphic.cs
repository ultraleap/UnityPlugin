/******************************************************************************
 * Copyright (C) Leap Motion, Inc. 2011-2018.                                 *
 * Leap Motion proprietary and confidential.                                  *
 *                                                                            *
 * Use subject to the terms of the Leap Motion SDK Agreement available at     *
 * https://developer.leapmotion.com/sdk_agreement, or another agreement       *
 * between Leap Motion and you, your company or other organization.           *
 ******************************************************************************/

using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using Leap.Unity.Query;
using Leap.Unity.Attributes;
using System;

namespace Leap.Unity.GraphicalRenderer {

  /// <summary>
  /// The Box Graphic is a type of procedural mesh graphic that can generate thick panels
  /// with a number of useful features:
  ///  - It allows nine slicing when using a sprite as the source for the texture data.
  ///  - It allows automatic tessellation such that it can be correctly warped by a space.
  ///  - It allows automatic resizing based on an attached RectTransform.
  /// </summary>
  [DisallowMultipleComponent]
  public class LeapBoxGraphic : LeapSlicedGraphic {

    [MinValue(0.0001f)]
    [EditTimeOnly]
    [SerializeField]
    private float _thickness = 0.01f;

    /// <summary>
    /// Gets the dimensions of the box graphic in local space.
    /// </summary>
    public Vector3 size {
      get { return new Vector3(_size.x, _size.y, _thickness); }
      private set { _size = new Vector2(value.x, value.y); _thickness = value.z; }
    }

    public override void RefreshSlicedMeshData(Vector2i    resolution,
                                               RectMargins meshMargins,
                                               RectMargins uvMargins) {
      List<Vector3> verts = new List<Vector3>();
      List<Vector2> uvs = new List<Vector2>();
      List<Vector3> normals = new List<Vector3>();
      List<int>     tris = new List<int>();

      // Back
      for (int vy = 0; vy < resolution.y; vy++) {
        for (int vx = 0; vx < resolution.x; vx++) {
          Vector2 vert;
          vert.x = calculateVertAxis(vx, resolution.x, rect.width, meshMargins.left, meshMargins.right);
          vert.y = calculateVertAxis(vy, resolution.y, rect.height, meshMargins.top, meshMargins.bottom);
          verts.Add(vert + new Vector2(rect.x, rect.y));
          normals.Add(Vector3.forward);

          Vector2 uv;
          uv.x = calculateVertAxis(vx, resolution.x, 1, uvMargins.left, uvMargins.right);
          uv.y = calculateVertAxis(vy, resolution.y, 1, uvMargins.top, uvMargins.bottom);
          uvs.Add(uv);
        }
      }

      int backVertsCount = verts.Count;

      // Front
      float depth = -size.z;
      for (int vy = 0; vy < resolution.y; vy++) {
        for (int vx = 0; vx < resolution.x; vx++) {
          Vector3 vert = Vector3.zero;
          vert.x = calculateVertAxis(vx, resolution.x, rect.width, meshMargins.left, meshMargins.right);
          vert.y = calculateVertAxis(vy, resolution.y, rect.height, meshMargins.top, meshMargins.bottom);
          verts.Add(vert + new Vector3(rect.x, rect.y, depth));
          normals.Add(Vector3.back);

          Vector2 uv;
          uv.x = calculateVertAxis(vx, resolution.x, 1, uvMargins.left, uvMargins.right);
          uv.y = calculateVertAxis(vy, resolution.y, 1, uvMargins.top, uvMargins.bottom);
          uvs.Add(uv);
        }
      }

      // Back
      for (int vy = 0; vy < resolution.y - 1; vy++) {
        for (int vx = 0; vx < resolution.x - 1; vx++) {
          int vertIndex = vy * resolution.x + vx;

          tris.Add(vertIndex);
          tris.Add(vertIndex + 1);
          tris.Add(vertIndex + 1 + resolution.x);

          tris.Add(vertIndex);
          tris.Add(vertIndex + 1 + resolution.x);
          tris.Add(vertIndex + resolution.x);
        }
      }

      // Front
      for (int vy = 0; vy < resolution.y - 1; vy++) {
        for (int vx = 0; vx < resolution.x - 1; vx++) {
          int vertIndex = backVertsCount + (vy * resolution.x + vx);

          tris.Add(vertIndex);
          tris.Add(vertIndex + 1 + resolution.x);
          tris.Add(vertIndex + 1);

          tris.Add(vertIndex);
          tris.Add(vertIndex + resolution.x);
          tris.Add(vertIndex + 1 + resolution.x);
        }
      }

      // Edges
      int ex = 0, ey = 0;
      int backVertIdx = verts.Count, frontVertIdx = verts.Count;

      // Left
      for (int vy = 0; vy < resolution.y; vy++) { // Repeat back edge, left side
        Vector2 vert;
        vert.x = calculateVertAxis(ex, resolution.x, rect.width, meshMargins.left, meshMargins.right);
        vert.y = calculateVertAxis(vy, resolution.y, rect.height, meshMargins.top, meshMargins.bottom);
        verts.Add(vert + new Vector2(rect.x, rect.y));
        normals.Add(Vector3.left);

        frontVertIdx += 1;

        Vector2 uv;
        uv.x = calculateVertAxis(ex, resolution.x, 1, uvMargins.left, uvMargins.right) + 0.01F /* cheat UVs in, prevents edge tearing */;
        uv.y = calculateVertAxis(vy, resolution.y, 1, uvMargins.top, uvMargins.bottom);
        uvs.Add(uv);
      }
      for (int vy = 0; vy < resolution.y; vy++) { // Repeat front edge, left side
        Vector3 vert = Vector3.zero;
        vert.x = calculateVertAxis(ex, resolution.x, rect.width, meshMargins.left, meshMargins.right);
        vert.y = calculateVertAxis(vy, resolution.y, rect.height, meshMargins.top, meshMargins.bottom);
        verts.Add(vert + new Vector3(rect.x, rect.y, depth));
        normals.Add(Vector3.left);

        Vector2 uv;
        uv.x = calculateVertAxis(ex, resolution.x, 1, uvMargins.left, uvMargins.right);
        uv.y = calculateVertAxis(vy, resolution.y, 1, uvMargins.top, uvMargins.bottom);
        uvs.Add(uv);
      }
      for (int vy = 0; vy < resolution.y - 1; vy++) { // Add quads
        addQuad(tris, frontVertIdx + vy, backVertIdx + vy, backVertIdx + vy + 1, frontVertIdx + vy + 1);
      }

      // Right
      ex = resolution.x - 1;
      backVertIdx = verts.Count;
      frontVertIdx = verts.Count;
      for (int vy = 0; vy < resolution.y; vy++) { // Repeat back edge, right side
        Vector2 vert;
        vert.x = calculateVertAxis(ex, resolution.x, rect.width, meshMargins.left, meshMargins.right);
        vert.y = calculateVertAxis(vy, resolution.y, rect.height, meshMargins.top, meshMargins.bottom);
        verts.Add(vert + new Vector2(rect.x, rect.y));
        normals.Add(Vector3.right);

        frontVertIdx += 1;

        Vector2 uv;
        uv.x = calculateVertAxis(ex, resolution.x, 1, uvMargins.left, uvMargins.right) - 0.01F /* cheat UVs in, prevents edge tearing */;
        uv.y = calculateVertAxis(vy, resolution.y, 1, uvMargins.top, uvMargins.bottom);
        uvs.Add(uv);
      }
      for (int vy = 0; vy < resolution.y; vy++) { // Repeat front edge, right side
        Vector3 vert = Vector3.zero;
        vert.x = calculateVertAxis(ex, resolution.x, rect.width, meshMargins.left, meshMargins.right);
        vert.y = calculateVertAxis(vy, resolution.y, rect.height, meshMargins.top, meshMargins.bottom);
        verts.Add(vert + new Vector3(rect.x, rect.y, depth));
        normals.Add(Vector3.right);

        Vector2 uv;
        uv.x = calculateVertAxis(ex, resolution.x, 1, uvMargins.left, uvMargins.right);
        uv.y = calculateVertAxis(vy, resolution.y, 1, uvMargins.top, uvMargins.bottom);
        uvs.Add(uv);
      }
      for (int vy = 0; vy < resolution.y - 1; vy++) { // Add quads
        addQuad(tris, frontVertIdx + vy + 1, backVertIdx + vy + 1, backVertIdx + vy, frontVertIdx + vy);
      }

      // Top
      ey = resolution.y - 1;
      backVertIdx = verts.Count;
      frontVertIdx = verts.Count;
      for (int vx = 0; vx < resolution.x; vx++) { // Repeat back edge, upper side
        Vector2 vert;
        vert.x = calculateVertAxis(vx, resolution.x, rect.width, meshMargins.left, meshMargins.right);
        vert.y = calculateVertAxis(ey, resolution.y, rect.height, meshMargins.top, meshMargins.bottom);
        verts.Add(vert + new Vector2(rect.x, rect.y));
        normals.Add(Vector3.up);

        frontVertIdx += 1;

        Vector2 uv;
        uv.x = calculateVertAxis(vx, resolution.x, 1, uvMargins.left, uvMargins.right);
        uv.y = calculateVertAxis(ey, resolution.y, 1, uvMargins.top, uvMargins.bottom) - 0.01F /* cheat UVs in, prevents edge tearing */;
        uvs.Add(uv);
      }
      for (int vx = 0; vx < resolution.x; vx++) { // Repeat front edge, upper side
        Vector3 vert = Vector3.zero;
        vert.x = calculateVertAxis(vx, resolution.x, rect.width, meshMargins.left, meshMargins.right);
        vert.y = calculateVertAxis(ey, resolution.y, rect.height, meshMargins.top, meshMargins.bottom);
        verts.Add(vert + new Vector3(rect.x, rect.y, depth));
        normals.Add(Vector3.up);

        Vector2 uv;
        uv.x = calculateVertAxis(vx, resolution.x, 1, uvMargins.left, uvMargins.right);
        uv.y = calculateVertAxis(ey, resolution.y, 1, uvMargins.top, uvMargins.bottom);
        uvs.Add(uv);
      }
      for (int vx = 0; vx < resolution.x - 1; vx++) { // Add quads
        addQuad(tris, frontVertIdx + vx, backVertIdx + vx, backVertIdx + vx + 1, frontVertIdx + vx + 1);
      }

      // Bottom
      ey = 0;
      backVertIdx = verts.Count;
      frontVertIdx = verts.Count;
      for (int vx = 0; vx < resolution.x; vx++) { // Repeat back edge, upper side
        Vector2 vert;
        vert.x = calculateVertAxis(vx, resolution.x, rect.width, meshMargins.left, meshMargins.right);
        vert.y = calculateVertAxis(ey, resolution.y, rect.height, meshMargins.top, meshMargins.bottom);
        verts.Add(vert + new Vector2(rect.x, rect.y));
        normals.Add(Vector3.down);

        frontVertIdx += 1;

        Vector2 uv;
        uv.x = calculateVertAxis(vx, resolution.x, 1, uvMargins.left, uvMargins.right);
        uv.y = calculateVertAxis(ey, resolution.y, 1, uvMargins.top, uvMargins.bottom) + 0.01F /* cheat UVs in, prevents edge tearing */;
        uvs.Add(uv);
      }
      for (int vx = 0; vx < resolution.x; vx++) { // Repeat front edge, upper side
        Vector3 vert = Vector3.zero;
        vert.x = calculateVertAxis(vx, resolution.x, rect.width, meshMargins.left, meshMargins.right);
        vert.y = calculateVertAxis(ey, resolution.y, rect.height, meshMargins.top, meshMargins.bottom);
        verts.Add(vert + new Vector3(rect.x, rect.y, depth));
        normals.Add(Vector3.down);

        Vector2 uv;
        uv.x = calculateVertAxis(vx, resolution.x, 1, uvMargins.left, uvMargins.right);
        uv.y = calculateVertAxis(ey, resolution.y, 1, uvMargins.top, uvMargins.bottom);
        uvs.Add(uv);
      }
      for (int vx = 0; vx < resolution.x - 1; vx++) { // Add quads
        addQuad(tris, frontVertIdx + vx + 1, backVertIdx + vx + 1, backVertIdx + vx, frontVertIdx + vx);
      }

      if (mesh == null) {
        mesh = new Mesh();
      }

      mesh.name = "Box Mesh";
      mesh.hideFlags = HideFlags.HideAndDontSave;

      mesh.Clear(keepVertexLayout: false);
      mesh.SetVertices(verts);
      mesh.SetNormals(normals);
      mesh.SetTriangles(tris, 0);
      mesh.SetUVs(uvChannel.Index(), uvs);
      mesh.RecalculateBounds();

      remappableChannels = UVChannelFlags.UV0;
    }

    private void addQuad(List<int> tris, int idx0, int idx1, int idx2, int idx3) {
      tris.Add(idx0);
      tris.Add(idx1);
      tris.Add(idx2);

      tris.Add(idx0);
      tris.Add(idx2);
      tris.Add(idx3);
    }
  }
}
