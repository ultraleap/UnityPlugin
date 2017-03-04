using UnityEngine;
using System;
using System.Collections.Generic;

namespace Procedural.DynamicMesh {

  public class VertexColorModifier : ModifierBehaviour<VertexColorMod> { }

  [Serializable]
  public struct VertexColorMod : IMeshMod {
    public Color color;
    public Operation operation;

    public void Modify(ref RawMesh input) {
      if (input.colors != null && operation == Operation.Provide) {
        return;
      }

      if (operation == Operation.Override || input.colors == null) {
        if (input.colors == null) {
          input.colors = new List<Color>();
        }
        for (int i = input.verts.Count; i-- != 0;) {
          input.colors.Add(color);
        }
      } else {
        for (int i = input.colors.Count; i-- != 0;) {
          input.colors[i] *= color;
        }
      }
    }

    public enum Operation {
      Override,
      Provide,
      Multiply
    }
  }
}
