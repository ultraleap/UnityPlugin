/******************************************************************************
 * Copyright (C) Leap Motion, Inc. 2011-2017.                                 *
 * Leap Motion proprietary and  confidential.                                 *
 *                                                                            *
 * Use subject to the terms of the Leap Motion SDK Agreement available at     *
 * https://developer.leapmotion.com/sdk_agreement, or another agreement       *
 * between Leap Motion and you, your company or other organization.           *
 ******************************************************************************/

using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

public static class AutoCopywriteHeader {

  private static Regex beginPattern = new Regex(@"^\/\*");
  private static Regex endPattern = new Regex(@"\*\/");


  /// <summary>
  /// The copywrite notice to populate at the start of *every* file ending with the extension .cs 
  /// Note that for the auto-update to function property, the following conditions must hold:
  ///  - the first line of the notice contains the comment block begin token /*
  ///  - the last and ONLY the last line of the notice contains the comment block end token */
  /// </summary>
  private static string[] copywriteNotice = {"/******************************************************************************",
                                             " * Copyright (C) Leap Motion, Inc. 2011-2017.                                 *",
                                             " * Leap Motion proprietary and  confidential.                                 *",
                                             " *                                                                            *",
                                             " * Use subject to the terms of the Leap Motion SDK Agreement available at     *",
                                             " * https://developer.leapmotion.com/sdk_agreement, or another agreement       *",
                                             " * between Leap Motion and you, your company or other organization.           *",
                                             " ******************************************************************************/"};

  private static string[] searchFolders = { "LeapMotion",
                                            "LeapMotionModules",
                                            "LeapMotionTests"};

  [MenuItem("Assets/Update Copywrite Headers")]
  public static void PopulateAutoHeaders() {
    List<string> files = new List<string>();
    foreach (var folder in searchFolders) {
      files.AddRange(Directory.GetFiles(Path.Combine("Assets", folder), "*.cs", SearchOption.AllDirectories));
    }

    StringBuilder builder = new StringBuilder();

    try {
      for (int i = 0; i < files.Count; i++) {
        string filename = files[i];

        if (EditorUtility.DisplayCancelableProgressBar("Updating copywrite notices",
                                                       "Updating " + Path.GetDirectoryName(filename) + "...",
                                                       i / (files.Count - 1.0f))) {
          return;
        }

        if (tryBuildFile(filename, builder)) {
          File.WriteAllText(filename, builder.ToString());
        } else {
          Debug.LogWarning("Could not add header to " + filename);
        }

        builder.Length = 0;
      }
    } finally {
      EditorUtility.ClearProgressBar();
      AssetDatabase.Refresh();
    }
  }

  private static bool tryBuildFile(string filename, StringBuilder builder) {
    using (var reader = File.OpenText(filename)) {
      string line;
      do {
        line = reader.ReadLine();

        //Empty Cs file
        if (line == null) {
          return false;
        }
      } while (line.Trim().Length == 0);

      //If we find a comment block already, skip past it, we are going to overwrite it!
      if (beginPattern.IsMatch(line)) {
        do {
          line = reader.ReadLine();

          //Unclosed closed comment block
          if (line == null) {
            return false;
          }
        } while (!endPattern.IsMatch(line));
        line = reader.ReadLine();

        //After we skip past the comment block, consume one extra empty line
        if (line.Trim().Length == 0) {
          line = reader.ReadLine();

          //A file with just a single comment block
          if (line == null) {
            return false;
          }
        }
      }

      //Append the comment block first
      foreach (var noticeLine in copywriteNotice) {
        builder.AppendLine(noticeLine);
      }

      //Then append a single empty line
      builder.AppendLine();

      //Then append the single valid line we are holding onto
      builder.AppendLine(line);

      //Finally append the rest of the file
      while (true) {
        line = reader.ReadLine();

        if (line == null) {
          return true;
        }

        builder.AppendLine(line);
      }
    }
  }
}
