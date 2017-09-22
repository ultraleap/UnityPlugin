/******************************************************************************
 * Copyright (C) Leap Motion, Inc. 2011-2017.                                 *
 * Leap Motion proprietary and  confidential.                                 *
 *                                                                            *
 * Use subject to the terms of the Leap Motion SDK Agreement available at     *
 * https://developer.leapmotion.com/sdk_agreement, or another agreement       *
 * between Leap Motion and you, your company or other organization.           *
 ******************************************************************************/

using System;
using System.IO;
using UnityEngine;
using UnityEditor;

namespace Leap.Unity {

  [CustomPropertyDrawer(typeof(StreamingFolder), useForChildren: true)]
  public class StreamingFolderPropertyDrawer : AssetFolderPropertyDrawer {

    protected override bool ValidatePath(string fullPath, string relativePath, out string errorMessage) {
      var fullInfo = new DirectoryInfo(fullPath);
      var streamingInfo = new DirectoryInfo(Application.streamingAssetsPath);

      if (IsInsideOrEqual(fullInfo, streamingInfo)) {
        errorMessage = null;
        return true;
      } else {
        errorMessage = "The specified folder is not a streaming asset folder. Streaming asset folders must be inside project's Assets/StreamingAssets directory.";
        return false;
      }
    }

    private bool IsInsideOrEqual(DirectoryInfo path, DirectoryInfo folder) {
      if (path.Parent == null) {
        return false;
      }

      if (string.Equals(path.FullName, folder.FullName, StringComparison.InvariantCultureIgnoreCase)) {
        return true;
      }

      return IsInsideOrEqual(path.Parent, folder);
    }
  }
}
