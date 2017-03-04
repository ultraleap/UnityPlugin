using UnityEngine;
using System;

namespace Procedural.DynamicMesh {

  public class TransformModifier : ModifierBehaviour<TransformMod>, IPreGenerate {

    [SerializeField]
    private Vector3 _position = Vector3.zero;

    [SerializeField]
    private Quaternion _rotation = Quaternion.identity;

    [SerializeField]
    private Vector3 _scale = Vector3.one;

    public bool OnPreGenerate() {
      _modifier.matrix = Matrix4x4.TRS(_position, _rotation, _scale);
      return true;
    }
  }

  [Serializable]
  public struct TransformMod : IMeshMod {
    public Matrix4x4 matrix;

    public void Modify(ref RawMesh input) {
      for (int i = input.verts.Count; i-- != 0;) {
        input.verts[i] = matrix.MultiplyPoint3x4(input.verts[i]);
      }

      if (input.normals != null) {
        for (int i = input.normals.Count; i-- != 0;) {
          input.normals[i] = matrix.MultiplyVector(input.normals[i]);
        }
      }
    }
  }
}
