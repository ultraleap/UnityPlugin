using UnityEngine;
using UnityEngine.Rendering;
using Leap.Unity.Query;

public class ProceduralSpriteMesh : ProceduralMeshSource {

  public override bool TryGenerateMesh(LeapGuiMeshData meshFeature,
                                   out Mesh mesh,
                                   out UVChannelFlags remappableChannels) {

    var spriteData = meshFeature.element.Sprite();
    if (spriteData == null || spriteData.sprite == null) {
      mesh = null;
      remappableChannels = 0;
      Debug.Log("obn");
      return false;
    }

    var sprite = spriteData.sprite;

    mesh = new Mesh();
    mesh.name = "Sprite Mesh";
    mesh.hideFlags = HideFlags.HideAndDontSave;
    mesh.vertices = sprite.vertices.Query().Select(v => (Vector3)v).ToArray();
    mesh.uv = SpriteAtlasUtil.GetAtlasedUvs(sprite);
    mesh.RecalculateBounds();

    //We are using atlas uvs, so no remapping allowed!
    remappableChannels = 0;

    return true;
  }
}
