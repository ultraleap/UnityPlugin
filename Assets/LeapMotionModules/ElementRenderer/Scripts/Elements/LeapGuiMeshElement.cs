using UnityEngine;
using UnityEngine.Rendering;
using System.Collections.Generic;
using Leap.Unity.Attributes;

public abstract class LeapGuiMeshElementBase : LeapGuiElement {

  [EditTimeOnly]
  public Color tint = Color.white;

  public Mesh mesh { get; protected set; }
  public UVChannelFlags remappableChannels { get; protected set; }

  public abstract void RefreshMeshData();

  public override void RebuildEditorPickingMesh() {
    base.RebuildEditorPickingMesh();

    RefreshMeshData();

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

    if (mesh == null) return;

    var topology = MeshCache.GetTopology(mesh);
    for (int i = 0; i < topology.tris.Length; i++) {
      pickingTris.Add(topology.tris[i] + pickingVerts.Count);
    }

    ITransformer transformer = attachedGroup.gui.space.GetTransformer(anchor);
    for (int i = 0; i < topology.verts.Length; i++) {
      Vector3 localRectVert = attachedGroup.transform.InverseTransformPoint(transform.TransformPoint(topology.verts[i]));
      pickingVerts.Add(transformer.TransformPoint(localRectVert));
    }

    pickingMesh.SetVertices(pickingVerts);
    pickingMesh.SetTriangles(pickingTris, 0, calculateBounds: true);
    pickingMesh.RecalculateNormals();
  }
}

public class LeapGuiMeshElement : LeapGuiMeshElementBase {

  [EditTimeOnly]
  [SerializeField]
  private Mesh _mesh;

  [Tooltip("All channels that are allowed to be remapped into atlas coordinates.")]
  [EditTimeOnly]
  [EnumFlags]
  [SerializeField]
  private UVChannelFlags _remappableChannels = UVChannelFlags.UV0 |
                                               UVChannelFlags.UV1 |
                                               UVChannelFlags.UV2 |
                                               UVChannelFlags.UV3;

  public override void RefreshMeshData() {
    mesh = _mesh;
    remappableChannels = _remappableChannels;
  }
}
