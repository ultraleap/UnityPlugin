/******************************************************************************
 * Copyright (C) Ultraleap, Inc. 2011-2020.                                   *
 *                                                                            *
 * Use subject to the terms of the Apache License 2.0 available at            *
 * http://www.apache.org/licenses/LICENSE-2.0, or another agreement           *
 * between Ultraleap and you, your company or other organization.             *
 ******************************************************************************/

using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

public static class EmptyFolderUtility {

  [MenuItem("Assets/Delete Empty Folders")]
  public static void DeleteEmptyFolders() {
    string[] directories = Directory.GetDirectories("Assets", "*", SearchOption.AllDirectories);

    foreach (var directory in directories) {
      try {
        if (!Directory.Exists(directory)) {
          continue;
        }

        if (Directory.GetFiles(directory, "*", SearchOption.AllDirectories).Count(p => Path.GetExtension(p) != ".meta") > 0) {
          continue;
        }
      } catch (Exception e) {
        Debug.LogException(e);
      }

      try {
        Directory.Delete(directory, recursive: true);
      } catch (Exception e) {
        Debug.LogError("Could not delete directory " + directory);
        Debug.LogException(e);
      }

    }

    AssetDatabase.Refresh();
  }
}
