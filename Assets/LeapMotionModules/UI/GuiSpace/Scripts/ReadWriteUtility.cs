using System.Collections;
using System.Collections.Generic;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

public static class ReadWriteUtility {


#if UNITY_EDITOR
  public static bool EnsureReadWriteEnabled(this Texture texture) {
    string assetPath = AssetDatabase.GetAssetPath(texture);
    if (string.IsNullOrEmpty(assetPath)) {
      return false;
    }

    TextureImporter importer = AssetImporter.GetAtPath(assetPath) as TextureImporter;
    if (importer == null) {
      return false;
    }

    if (!importer.isReadable) {
      importer.isReadable = true;
      importer.SaveAndReimport();
      AssetDatabase.Refresh();
    }

    return true;
  }

  public static bool EnsureReadWriteEnabled(this Mesh mesh) {
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

    return true;
  }
#endif



}
