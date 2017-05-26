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
using System.Linq;
using System.Text.RegularExpressions;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

public static class VariantEnabler {
  public const string EARLY_OUT_KEYWORD = "#pragma";

  public static Regex isSurfaceShaderRegex = new Regex(@"#pragma\s+surface\s+surf");
  public static Regex disabledVariantRegex = new Regex(@"/{2,}\s*#pragma\s+shader_feature\s+_\s+GRAPHIC_RENDERER");
  public static Regex enabledVariantRegex = new Regex(@"^\s*#pragma\s+shader_feature\s+_\s+GRAPHIC_RENDERER");

  public static bool IsSurfaceShader(Shader shader) {
    ShaderInfo info;
    if (tryGetShaderInfo(shader, out info)) {
      return info.isSurfaceShader;
    } else {
      return false;
    }
  }

  public static bool DoesShaderHaveVariantsDisabled(Shader shader) {
    ShaderInfo info;
    if (tryGetShaderInfo(shader, out info)) {
      return info.doesHaveShaderVariantsDisabled;
    } else {
      return false;
    }
  }

  public static void SetShaderVariantsEnabled(Shader shader, bool enable) {
    string path = AssetDatabase.GetAssetPath(shader);
    if (string.IsNullOrEmpty(path)) {
      return;
    }

    _infoCache.Remove(path);

    string[] lines = File.ReadAllLines(path);
    using (var writer = File.CreateText(path)) {
      foreach (var line in lines) {
        if (enable && disabledVariantRegex.IsMatch(line)) {
          writer.WriteLine(line.Replace("/", " "));
        } else if (!enable && enabledVariantRegex.IsMatch(line)) {
          var startEnum = line.TakeWhile(c => char.IsWhiteSpace(c));
          int count = Mathf.Max(0, startEnum.Count() - 2);
          var start = new string(startEnum.Take(count).ToArray());
          writer.WriteLine(start + "//" + line.TrimStart());
        } else {
          writer.WriteLine(line);
        }
      }
    }
  }

  private static Dictionary<string, ShaderInfo> _infoCache = new Dictionary<string, ShaderInfo>();
  private static bool tryGetShaderInfo(Shader shader, out ShaderInfo info) {
    string path = AssetDatabase.GetAssetPath(shader);
    if (string.IsNullOrEmpty(path)) {
      info = default(ShaderInfo);
      return false;
    }

    DateTime modifiedTime = File.GetLastWriteTime(path);

    if (_infoCache.TryGetValue(path, out info)) {
      //If the check time is newer than the modified time, return cached results
      if (modifiedTime < info.checkTime) {
        return true;
      }
    }

    info.isSurfaceShader = false;
    info.doesHaveShaderVariantsDisabled = false;
    info.checkTime = modifiedTime;

    string[] lines = File.ReadAllLines(path);
    foreach (var line in lines) {
      if (!line.Contains(EARLY_OUT_KEYWORD)) {
        continue;
      }

      if (disabledVariantRegex.IsMatch(line)) {
        info.doesHaveShaderVariantsDisabled = true;
      }

      if (isSurfaceShaderRegex.IsMatch(line)) {
        info.isSurfaceShader = true;
      }
    }

    _infoCache[path] = info;
    return true;
  }

  private struct ShaderInfo {
    public bool doesHaveShaderVariantsDisabled;
    public bool isSurfaceShader;
    public DateTime checkTime;
  }
}
