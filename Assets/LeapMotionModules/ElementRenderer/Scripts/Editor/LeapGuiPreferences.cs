using UnityEngine;
using UnityEditor;
using System;
using System.IO;
using System.Collections.Generic;
using System.Text.RegularExpressions;

public class LeapGuiPreferences : MonoBehaviour {
  public const string LEAP_GUI_CGINC_PATH = "LeapMotionModules/ElementRenderer/Resources/LeapGui.cginc";
  public const string LEAP_GUI_SHADER_FOLDER = "Assets/LeapMotionModules/ElementRenderer/Shaders/";
  private static Regex _elementMaxRegex = new Regex(@"^#define\s+ELEMENT_MAX\s+(\d+)\s*$");

  private static int _cachedElementMax = -1; //-1 signals dirty
  public static int ElementMax {
    get {
      if (_cachedElementMax == -1) {
        string errorMesage;
        string path;
        List<string> lines;
        int lineIndex;

        tryCalculateElementMax(out _cachedElementMax, out errorMesage, out path, out lines, out lineIndex);

        if (errorMesage != null) {
          _cachedElementMax = int.MaxValue;
        }
      }

      return _cachedElementMax;
    }
  }

  [PreferenceItem("Leap Gui")]
  private static void preferencesGUI() {
    int elementMax;
    string errorMessage;
    string path;
    List<string> lines;
    int lineIndex;

    _cachedElementMax = -1;
    if (!tryCalculateElementMax(out elementMax, out errorMessage, out path, out lines, out lineIndex)) {
      EditorGUILayout.HelpBox(errorMessage +
                              "\n\nRe-installing the Leap Gui package can help fix this problem.",
                              MessageType.Warning);
      return;
    }

    int newElementMax = EditorGUILayout.DelayedIntField("Maximum Elements", elementMax);
    newElementMax = Mathf.Clamp(newElementMax, 1, 1024);

    if (newElementMax == elementMax) {
      return; //Work here is done!  Nothing to change!
    }

    lines[lineIndex] = lines[lineIndex].Replace(elementMax.ToString(), newElementMax.ToString());

    //Write the new data to the file
    File.WriteAllLines(path, lines.ToArray());

    //Make sure to re-import all the shaders
    AssetDatabase.ImportAsset(LEAP_GUI_SHADER_FOLDER, ImportAssetOptions.ImportRecursive);
  }

  private static bool tryCalculateElementMax(out int elementMax,
                                             out string errorMessage,
                                             out string path,
                                             out List<string> lines,
                                             out int lineIndex) {
    elementMax = -1;
    errorMessage = null;
    lines = null;
    lineIndex = -1;

    path = Path.Combine(Application.dataPath, LEAP_GUI_CGINC_PATH);
    if (!File.Exists(path)) {
      errorMessage = "Could not locate the Leap cginclude file, was it renamed or deleted?";
      return false;
    }

    lines = new List<string>();

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
      errorMessage = "Exception caught when trying to read file.";
      Debug.LogError(e);
      return false;
    } finally {
      if (reader != null) {
        reader.Dispose();
      }
    }

    Match successMatch = null;
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
      errorMessage = "Could not parse the file correctly, it might have been modified!";
      return false;
    }

    if (!int.TryParse(successMatch.Groups[1].Value, out elementMax)) {
      errorMessage = "The maximum element value must always be an integer value!";
      return false;
    }

    return true;
  }
}
