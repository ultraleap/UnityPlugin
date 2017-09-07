/******************************************************************************
 * Copyright (C) Leap Motion, Inc. 2011-2017.                                 *
 * Leap Motion proprietary and  confidential.                                 *
 *                                                                            *
 * Use subject to the terms of the Leap Motion SDK Agreement available at     *
 * https://developer.leapmotion.com/sdk_agreement, or another agreement       *
 * between Leap Motion and you, your company or other organization.           *
 ******************************************************************************/

using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Assertions;
using Leap.Unity.Space;

namespace Leap.Unity.GraphicalRenderer {

  public abstract partial class LeapMeshGraphicBase : LeapGraphic {

#if UNITY_EDITOR
    public class MeshEditorApi : EditorApi {
      protected readonly LeapMeshGraphicBase _meshGraphic;

      public MeshEditorApi(LeapMeshGraphicBase meshGraphic) : base(meshGraphic) {
        _meshGraphic = meshGraphic;
      }

      public override void RebuildEditorPickingMesh() {
        base.RebuildEditorPickingMesh();

        Assert.IsNotNull(_meshGraphic);

        _meshGraphic.RefreshMeshData();

        List<Vector3> pickingVerts = new List<Vector3>();
        List<int> pickingTris = new List<int>();

        pickingVerts.Clear();
        pickingTris.Clear();

        if (pickingMesh == null) {
          pickingMesh = new Mesh();
          pickingMesh.MarkDynamic();
          pickingMesh.hideFlags = HideFlags.HideAndDontSave;
          pickingMesh.name = "Graphic Picking Mesh";
        }
        pickingMesh.Clear(keepVertexLayout: false);

        if (_meshGraphic.mesh == null) return;

        var topology = MeshCache.GetTopology(_meshGraphic.mesh);
        for (int i = 0; i < topology.tris.Length; i++) {
          pickingTris.Add(topology.tris[i] + pickingVerts.Count);
        }

        ITransformer transformer = null;
        if (_meshGraphic.anchor != null) {
          transformer = _meshGraphic.anchor.transformer;
        }

        for (int i = 0; i < topology.verts.Length; i++) {
          Vector3 localRectVert = _meshGraphic.attachedGroup.renderer.transform.InverseTransformPoint(_meshGraphic.transform.TransformPoint(topology.verts[i]));

          if (transformer != null) {
            localRectVert = transformer.TransformPoint(localRectVert);
          }

          localRectVert = _meshGraphic.attachedGroup.renderer.transform.TransformPoint(localRectVert);

          pickingVerts.Add(localRectVert);
        }

        pickingMesh.SetVertices(pickingVerts);
        pickingMesh.SetTriangles(pickingTris, 0, calculateBounds: true);
        pickingMesh.RecalculateNormals();
      }
    }
#endif
  }
}
