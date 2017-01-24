using System.Collections;
using System.Collections.Generic;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.Sprites;
#endif
using Leap.Unity.Query;

namespace Leap.Unity.Gui.Space {

  public class LeapGuiSprite : ProceduralMeshSource {

    public override Mesh GetMesh(LeapElement element) {
#if UNITY_EDITOR
      Sprite sprite = element.GetSprite(0);
      if (sprite == null) {
        throw new System.NotImplementedException();
      }

      Mesh mesh = new Mesh();
      mesh.vertices = sprite.vertices.Query().Select(v => (Vector3)v).ToArray();
      mesh.triangles = sprite.triangles.Query().Select(i => (int)i).ToArray();
      mesh.uv = SpriteUtility.GetSpriteUVs(element.GetSprite(0), getAtlasData: true);
      return mesh;
#else
      throw new System.NotImplementedException();
#endif
    }

    public override bool DoesMeshHaveAtlasUvs(int uvChannel) {
      return true;
    }

    public override bool CanGenerateMeshForElement(LeapElement element) {
      return true;
    }
  }
}
