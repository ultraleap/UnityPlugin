using UnityEngine;
using UnityEditor;
using System;
using System.IO;
using System.Collections.Generic;
using System.Text.RegularExpressions;

public class LeapGuiPreferences : MonoBehaviour {
  public const string LEAP_GUI_CGINC_PATH = "LeapMotionModules/ElementRenderer/Resources/LeapGui.cginc";
  private static Regex _elementMaxRegex = new Regex(@"^#define\s+ELEMENT_MAX\s+(\d+)\s*$");

  [PreferenceItem("Leap Gui")]
  private static void preferencesGUI() {
    string path = Path.Combine(Application.dataPath, LEAP_GUI_CGINC_PATH);
    if (!File.Exists(path)) {
      displayHelpBox("Could not locate the Leap cginclude file, was it renamed or deleted?");
      return;
    }

    List<string> lines = new List<string>();

    StreamReader reader = null;
    try {
      reader = File.OpenText(path);

      while (true) {
        string line = reader.ReadLine();
        if (line == null) {
          break;
        }
        lines.Add(line);
      }
    } catch (Exception e) {
      displayHelpBox("Exception caught when trying to read file.");
      Debug.LogError(e);
      return;
    } finally {
      if (reader != null) {
        reader.Dispose();
      }
    }

    Match successMatch = null;
    int lineIndex = -1;
    for (int i = 0; i < lines.Count; i++) {
      string line = lines[i];
      var match = _elementMaxRegex.Match(line);
      if (match.Success) {
        successMatch = match;
        lineIndex = i;
        break;
      }
    }

    if (successMatch == null) {
      displayHelpBox("Could not parse the file correctly, it might have been modified!");
      return;
    }

    int elementMax;
    if (!int.TryParse(successMatch.Groups[1].Value, out elementMax)) {
      displayHelpBox("The maximum element value must always be an integer value!");
      return;
    }
    
    int newElementMax = EditorGUILayout.DelayedIntField("Maximum Elements", elementMax);
    newElementMax = Mathf.Clamp(newElementMax, 1, 1024);

    if (newElementMax == elementMax) {
      return; //work here is done!
    }

    lines[lineIndex] = lines[lineIndex].Replace(successMatch.Groups[1].Value, newElementMax.ToString());

    //Write the new data to the file
    File.WriteAllLines(path, lines.ToArray());

    //Make sure to re-import all the shaders
    AssetDatabase.ImportAsset("Assets/LeapMotionModules/ElementRenderer/Shaders/", ImportAssetOptions.ImportRecursive);
  }

  private static void displayHelpBox(string primaryMessage) {
    EditorGUILayout.HelpBox(primaryMessage +
                            "\n\nRe-installing the Leap Gui package can help fix this problem.",
                            MessageType.Warning);
  }

}
