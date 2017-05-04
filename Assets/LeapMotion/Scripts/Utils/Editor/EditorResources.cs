/******************************************************************************
 * Copyright (C) Leap Motion, Inc. 2011-2017.                                 *
 * Leap Motion proprietary and  confidential.                                 *
 *                                                                            *
 * Use subject to the terms of the Leap Motion SDK Agreement available at     *
 * https://developer.leapmotion.com/sdk_agreement, or another agreement       *
 * between Leap Motion and you, your company or other organization.           *
 ******************************************************************************/

using System.IO;
using UnityEngine;
using UnityEditor;

namespace Leap.Unity {

  public static class EditorResources {

    /// <summary>
    /// Use like Resources.Load, but searches folders named EditorResources instead
    /// of folders named Resources.  Remember that you should not include the file
    /// extension, just like when using Resources!
    /// </summary>
    public static T Load<T>(string name) where T : Object {
      foreach (var dir in Directory.GetDirectories("Assets", "EditorResources", SearchOption.AllDirectories)) {
        foreach (var filename in Directory.GetFiles(dir, name + ".*")) {
          if (!string.IsNullOrEmpty(AssetDatabase.AssetPathToGUID(filename))) {
            return AssetDatabase.LoadAssetAtPath<T>(filename);
          }
        }
      }
      return null;
    }
  }
}
