/******************************************************************************
 * Copyright (C) Leap Motion, Inc. 2011-2018.                                 *
 * Leap Motion proprietary and confidential.                                  *
 *                                                                            *
 * Use subject to the terms of the Leap Motion SDK Agreement available at     *
 * https://developer.leapmotion.com/sdk_agreement, or another agreement       *
 * between Leap Motion and you, your company or other organization.           *
 ******************************************************************************/

using System.IO;
using UnityEditor;

namespace Leap.Unity {

  [CustomPropertyDrawer(typeof(StreamingFolder), useForChildren: true)]
  public class StreamingAssetPropertyDrawer : StreamingFolderPropertyDrawer {

    protected override string PromptUserForPath(string currentPath) {
      return EditorUtility.OpenFilePanel("Select File", currentPath, "");
    }

    protected override bool ValidatePath(string fullPath, string relativePath, out string errorMessage) {
      if (!File.Exists(fullPath)) {
        errorMessage = "The specified file does not exist!";
        return false;
      }

      if ((File.GetAttributes(fullPath) & FileAttributes.Directory) == FileAttributes.Directory) {
        errorMessage = "You must specify a file and not a directory!";
        return false;
      }

      bool isValid = base.ValidatePath(fullPath, relativePath, out errorMessage);
      if (!isValid) {
        errorMessage = "The specified file is not a streaming asset. Streaming assets must be inside project's Assets/StreamingAssets directory.";
      }
      return isValid;
    }
  }
}
