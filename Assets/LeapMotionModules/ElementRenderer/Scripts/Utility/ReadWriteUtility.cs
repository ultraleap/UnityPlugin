using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

public static class ReadWriteUtility {

  public static Texture2D GetReadableTexture(this Texture2D texture) {
    RenderTexture rt = new RenderTexture(texture.width, texture.height, 0, RenderTextureFormat.ARGB32, RenderTextureReadWrite.Linear);
    Graphics.Blit(texture, rt);

    Texture2D reciever = new Texture2D(texture.width, texture.height, TextureFormat.ARGB32, mipmap: false, linear: true);
    reciever.hideFlags = HideFlags.HideAndDontSave;

    RenderTexture.active = rt;
    reciever.ReadPixels(new Rect(0, 0, texture.width, texture.height), 0, 0);
    RenderTexture.active = null;
    rt.Release();

    reciever.Apply(updateMipmaps: false, makeNoLongerReadable: false);
    reciever.filterMode = texture.filterMode;
    reciever.wrapMode = texture.wrapMode;
    return reciever;
  }

#if UNITY_EDITOR
  public static bool IsReadable(this Texture2D texture) {
    string assetPath = AssetDatabase.GetAssetPath(texture);
    if (string.IsNullOrEmpty(assetPath)) {
      return false;
    }

    ModelImporter importer = AssetImporter.GetAtPath(assetPath) as ModelImporter;
    if (importer == null) {
      return false;
    }

    return importer.isReadable;
  }
#endif

  public static bool TryEnableReadWrite(this Mesh mesh) {
#if UNITY_EDITOR
    string assetPath = AssetDatabase.GetAssetPath(mesh);
    if (string.IsNullOrEmpty(assetPath)) {
      return false;
    }

    ModelImporter importer = AssetImporter.GetAtPath(assetPath) as ModelImporter;
    if (importer == null) {
      return false;
    }

    if (!importer.isReadable) {
      importer.isReadable = true;
      importer.SaveAndReimport();
      AssetDatabase.Refresh();
    }
#endif

    return mesh.isReadable;
  }
}
