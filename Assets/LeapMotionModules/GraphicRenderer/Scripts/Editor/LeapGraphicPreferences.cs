/******************************************************************************
 * Copyright (C) Leap Motion, Inc. 2011-2017.                                 *
 * Leap Motion proprietary and  confidential.                                 *
 *                                                                            *
 * Use subject to the terms of the Leap Motion SDK Agreement available at     *
 * https://developer.leapmotion.com/sdk_agreement, or another agreement       *
 * between Leap Motion and you, your company or other organization.           *
 ******************************************************************************/

using UnityEngine;
using UnityEditor;
using System;
using System.IO;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace Leap.Unity.GraphicalRenderer {

  public class LeapGraphicPreferences : MonoBehaviour {
    public const string LEAP_GRAPHIC_CGINC_PATH = "LeapMotionModules/GraphicRenderer/Resources/GraphicRenderer.cginc";
    public const string LEAP_GRAPHIC_SHADER_FOLDER = "Assets/LeapMotionModules/GraphicRenderer/Shaders/";
    private static Regex _graphicMaxRegex = new Regex(@"^#define\s+GRAPHIC_MAX\s+(\d+)\s*$");

    public const string PROMPT_WHEN_GROUP_CHANGE_KEY = "LeapGraphicRenderer_ShouldPromptWhenGroupChange";

    private static int _cachedGraphicMax = -1; //-1 signals dirty
    public static int graphicMax {
      get {
        if (_cachedGraphicMax == -1) {
          string errorMesage;
          string path;
          List<string> lines;
          int lineIndex;

          tryCalculateGraphicMax(out _cachedGraphicMax, out errorMesage, out path, out lines, out lineIndex);

          if (errorMesage != null) {
            _cachedGraphicMax = int.MaxValue;
          }
        }

        return _cachedGraphicMax;
      }
    }

    public static bool promptWhenGroupChange {
      get {
        return EditorPrefs.GetBool(PROMPT_WHEN_GROUP_CHANGE_KEY, true);
      }
      set {
        EditorPrefs.SetBool(PROMPT_WHEN_GROUP_CHANGE_KEY, value);
      }
    }

    [PreferenceItem("Leap Graphics")]
    private static void preferencesGUI() {
      drawGraphicMaxField();

      GUIContent prompContent = new GUIContent("Prompt When Group Changed", "Should the system prompt the user when they change the group of a graphic to a group with different features.");
      bool newPromptValue = EditorGUILayout.Toggle(prompContent, promptWhenGroupChange);
      if (promptWhenGroupChange != newPromptValue) {
        promptWhenGroupChange = newPromptValue;
      }
    }

    private static void drawGraphicMaxField() {
      int graphicMax;
      string errorMessage;
      string path;
      List<string> lines;
      int lineIndex;

      _cachedGraphicMax = -1;
      if (!tryCalculateGraphicMax(out graphicMax, out errorMessage, out path, out lines, out lineIndex)) {
        EditorGUILayout.HelpBox(errorMessage +
                                "\n\nRe-installing the Leap Gui package can help fix this problem.",
                                MessageType.Warning);
        return;
      }

      int newGraphicMax = EditorGUILayout.DelayedIntField("Maximum Graphics", graphicMax);
      newGraphicMax = Mathf.Clamp(newGraphicMax, 1, 1024);

      if (newGraphicMax == graphicMax) {
        return; //Work here is done!  Nothing to change!
      }

      lines[lineIndex] = lines[lineIndex].Replace(graphicMax.ToString(), newGraphicMax.ToString());

      //Write the new data to the file
      File.WriteAllLines(path, lines.ToArray());

      //Make sure to re-import all the shaders
      AssetDatabase.ImportAsset(LEAP_GRAPHIC_SHADER_FOLDER, ImportAssetOptions.ImportRecursive);
    }

    private static bool tryCalculateGraphicMax(out int elementMax,
                                               out string errorMessage,
                                               out string path,
                                               out List<string> lines,
                                               out int lineIndex) {
      elementMax = -1;
      errorMessage = null;
      lines = null;
      lineIndex = -1;

      path = Path.Combine(Application.dataPath, LEAP_GRAPHIC_CGINC_PATH);
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
        var match = _graphicMaxRegex.Match(line);
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
        errorMessage = "The maximum graphic value must always be an integer value!";
        return false;
      }

      return true;
    }
  }
}
