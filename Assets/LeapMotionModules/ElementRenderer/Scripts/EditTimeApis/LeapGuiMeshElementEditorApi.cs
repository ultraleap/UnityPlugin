using System.Collections.Generic;
using UnityEngine;

public abstract partial class LeapGuiMeshElementBase : LeapGuiElement {

#if UNITY_EDITOR
  public class MeshEditorApi : EditorApi {
    protected readonly LeapGuiMeshElementBase _meshElement;

    public MeshEditorApi(LeapGuiMeshElementBase meshElement) : base(meshElement) {
      _meshElement = meshElement;
    }

    public override void RebuildEditorPickingMesh() {
      base.RebuildEditorPickingMesh();

      _meshElement.RefreshMeshData();

      List<Vector3> pickingVerts = new List<Vector3>();
      List<int> pickingTris = new List<int>();

      pickingVerts.Clear();
      pickingTris.Clear();

      if (pickingMesh == null) {
        pickingMesh = new Mesh();
        pickingMesh.MarkDynamic();
        pickingMesh.hideFlags = HideFlags.HideAndDontSave;
        pickingMesh.name = "Gui Element Picking Mesh";
      }
      pickingMesh.Clear();

      if (_meshElement.mesh == null) return;

      var topology = MeshCache.GetTopology(_meshElement.mesh);
      for (int i = 0; i < topology.tris.Length; i++) {
        pickingTris.Add(topology.tris[i] + pickingVerts.Count);
      }

      ITransformer transformer = _meshElement.attachedGroup.gui.space.GetTransformer(_meshElement.anchor);
      for (int i = 0; i < topology.verts.Length; i++) {
        Vector3 localRectVert = _meshElement.attachedGroup.transform.InverseTransformPoint(_meshElement.transform.TransformPoint(topology.verts[i]));
        pickingVerts.Add(transformer.TransformPoint(localRectVert));
      }

      pickingMesh.SetVertices(pickingVerts);
      pickingMesh.SetTriangles(pickingTris, 0, calculateBounds: true);
      pickingMesh.RecalculateNormals();
    }
  }
#endif
}
