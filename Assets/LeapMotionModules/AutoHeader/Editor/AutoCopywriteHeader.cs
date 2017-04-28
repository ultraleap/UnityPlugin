/******************************************************************************\
* Copyright (C) Leap Motion, Inc. 2011-2017.                                   *
* Leap Motion proprietary and  confidential.  

* Use subject to the terms of the Leap Motion SDK Agreement available at https://developer.leapmotion.com/sdk_agreement, or another agreement between Leap Motion and you, your company or other organization.                    
\******************************************************************************/

using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using UnityEditor;

public static class AutoCopywriteHeader {

  private static Regex beginPattern = new Regex(@"^\/\*");
  private static Regex endPattern = new Regex(@"\*\/");

  private static string[] copywriteNotice = {"/* this is a test",
                                             " * of a copywrite notice */" };

  [MenuItem("Assets/Update Copywrite Headers")]
  public static void PopulateAutoHeaders() {
    var files = Directory.GetFiles("Assets", "*.cs", SearchOption.AllDirectories);
    StringBuilder builder = new StringBuilder();

    try {
      for (int i = 0; i < files.Length; i++) {
        string filename = files[i];

        if (EditorUtility.DisplayCancelableProgressBar("Updating copywrite notices",
                                                       "Updating " + Path.GetFileNameWithoutExtension(filename) + "...",
                                                       i / (files.Length - 1.0f))) {
          return;
        }

        handleFile(filename, builder);

        File.WriteAllText(filename, builder.ToString());
        builder.Length = 0;
      }
    } finally {
      EditorUtility.ClearProgressBar();
      AssetDatabase.Refresh();
    }
  }

  private static void handleFile(string filename, StringBuilder builder) {
    using (var reader = File.OpenText(filename)) {
      string line;
      do {
        line = reader.ReadLine();

        //Empty Cs file
        if (line == null) {
          return;
        }
      } while (line.Trim().Length == 0);

      if (beginPattern.IsMatch(line)) {
        do {
          line = reader.ReadLine();

          //Unclosed closed comment block
          if (line == null) {
            return;
          }
        } while (!endPattern.IsMatch(line));
        line = reader.ReadLine();
      }

      foreach (var noticeLine in copywriteNotice) {
        builder.AppendLine(noticeLine);
      }

      builder.AppendLine(line);

      while (true) {
        line = reader.ReadLine();

        if (line == null) {
          return;
        }

        builder.AppendLine(line);
      }
    }
  }
}
