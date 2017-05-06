/******************************************************************************
 * Copyright (C) Leap Motion, Inc. 2011-2017.                                 *
 * Leap Motion proprietary and  confidential.                                 *
 *                                                                            *
 * Use subject to the terms of the Leap Motion SDK Agreement available at     *
 * https://developer.leapmotion.com/sdk_agreement, or another agreement       *
 * between Leap Motion and you, your company or other organization.           *
 ******************************************************************************/

using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Leap.Unity.GraphicalRenderer {

  public static class ReadWriteUtility {

    public static bool IsReadable(this Texture2D texture) {
#if UNITY_EDITOR
      string assetPath = AssetDatabase.GetAssetPath(texture);
      if (string.IsNullOrEmpty(assetPath)) {
        return false;
      }

      ModelImporter importer = AssetImporter.GetAtPath(assetPath) as ModelImporter;
      if (importer == null) {
        return false;
      }

      return importer.isReadable;
#else
    //Welp, guess the user is just gonna have to eat the errors if they are wrong!
    //No way to check currently :(
    return true;
#endif
    }


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
}
