/******************************************************************************
 * Copyright (C) Leap Motion, Inc. 2011-2018.                                 *
 * Leap Motion proprietary and confidential.                                  *
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
    public const int GRAPHIC_COUNT_SOFT_CEILING = 144;
    public const string LEAP_GRAPHIC_CGINC_PATH = "LeapMotion/Modules/GraphicRenderer/Resources/GraphicRenderer.cginc";
    public const string LEAP_GRAPHIC_SHADER_FOLDER = "Assets/LeapMotion/Modules/GraphicRenderer/Shaders/";
    private static Regex _graphicMaxRegex = new Regex(@"^#define\s+GRAPHIC_MAX\s+(\d+)\s*$");

    public const string PROMPT_WHEN_GROUP_CHANGE_KEY = "LeapGraphicRenderer_ShouldPromptWhenGroupChange";

    public const string PROMP_WHEN_ADD_CUSTOM_CHANNEL_LEY = "LeapGraphicRenderer_ShouldPrompWhenAddCustomChannel";

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

    public static bool promptWhenAddCustomChannel {
      get {
        return EditorPrefs.GetBool(PROMP_WHEN_ADD_CUSTOM_CHANNEL_LEY, true);
      }
      set {
        EditorPrefs.SetBool(PROMP_WHEN_ADD_CUSTOM_CHANNEL_LEY, value);
      }
    }

    [LeapPreferences("Graphic Renderer", 20)]
    private static void preferencesGUI() {
      drawGraphicMaxField();

      GUIContent groupChangedContent = new GUIContent("Prompt When Group Changed", "Should the system prompt the user when they change the group of a graphic to a group with different features.");
      bool newPromptValue = EditorGUILayout.Toggle(groupChangedContent, promptWhenGroupChange);
      if (promptWhenGroupChange != newPromptValue) {
        promptWhenGroupChange = newPromptValue;
      }

      GUIContent addChannelContent = new GUIContent("Prompt For Custom Channel", "Should the system warn the user about writing custom shaders when they try to add a Custom Channel feature?");
      bool newPromptWhenAddCustomChannelValue = EditorGUILayout.Toggle(addChannelContent, promptWhenAddCustomChannel);
      if (newPromptWhenAddCustomChannelValue != promptWhenAddCustomChannel) {
        promptWhenAddCustomChannel = newPromptWhenAddCustomChannelValue;
      }

      EditorGUILayout.Space();
      GUILayout.Label("Surface-shader variant options", EditorStyles.boldLabel);

      EditorGUILayout.HelpBox("Using surface-shader variants can drastically increase import time!  Only enable variants if you are using surface shaders with the graphic renderer.", MessageType.Info);

      using (new EditorGUILayout.HorizontalScope()) {
        if (GUILayout.Button("Enable all variants")) {
          setVariantsEnabledForSurfaceShaders(enable: true);
        }

        if (GUILayout.Button("Disable all variants")) {
          setVariantsEnabledForSurfaceShaders(enable: false);
        }
      }
    }

    private static void setVariantsEnabledForSurfaceShaders(bool enable) {
      foreach (var path in Directory.GetFiles(LEAP_GRAPHIC_SHADER_FOLDER, "*.shader", SearchOption.AllDirectories)) {
        var shader = AssetDatabase.LoadAssetAtPath<Shader>(path);
        if (shader == null) continue;

        if (VariantEnabler.IsSurfaceShader(shader)) {
          VariantEnabler.SetShaderVariantsEnabled(shader, enable);
        }
      }
      AssetDatabase.Refresh();
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

      int newGraphicMax = EditorGUILayout.DelayedIntField("Max Graphics Per-Group", graphicMax);
      newGraphicMax = Mathf.Min(newGraphicMax, 1023); //maximum array length for Unity shaders is 1023

      if (newGraphicMax > GRAPHIC_COUNT_SOFT_CEILING && graphicMax <= GRAPHIC_COUNT_SOFT_CEILING) {
        if (!EditorUtility.DisplayDialog("Large Graphic Count",
                                        "Setting the graphic count larger than 144 can cause incorrect rendering " +
                                        "or shader compilation failure on certain systems, are you sure you want " +
                                        "to continue?", "Yes", "Cancel")) {
          return;
        }
      }

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
