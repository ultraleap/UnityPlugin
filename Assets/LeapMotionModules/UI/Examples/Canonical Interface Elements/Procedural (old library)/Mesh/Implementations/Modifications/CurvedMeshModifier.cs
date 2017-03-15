using UnityEngine;
using System;
using Procedural.DynamicMesh;
using Leap.Unity.Attributes;
using Leap.Unity.RuntimeGizmos;

public class CurvedMeshModifier : ModifierBehaviour<CurvedMeshMod>, IRuntimeGizmoComponent {

  public void OnDrawRuntimeGizmos(RuntimeGizmoDrawer drawer) {
    if (_modifier.useReferenceRadius) {
      drawer.DrawWireCircle(transform.position, transform.up, _modifier.referenceRadius);
    }
  }
}

[Serializable]
public struct CurvedMeshMod : IMeshMod {
  public CurvedSpace space;
  public bool useReferenceRadius;
  public float referenceRadius;

  public void Modify(ref RawMesh input) {
    if (space == null) return;

    for (int i = input.verts.Count; i-- != 0;) {
      Vector3 pos = input.verts[i];
      input.verts[i] = space.RectToLocal(pos, referenceRadius, useReferenceRadius);
    }

    input.normals = null;
  }
}
