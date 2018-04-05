/******************************************************************************
 * Copyright (C) Leap Motion, Inc. 2011-2018.                                 *
 * Leap Motion proprietary and confidential.                                  *
 *                                                                            *
 * Use subject to the terms of the Leap Motion SDK Agreement available at     *
 * https://developer.leapmotion.com/sdk_agreement, or another agreement       *
 * between Leap Motion and you, your company or other organization.           *
 ******************************************************************************/

using System;
using System.Collections.Generic;

namespace Leap.Unity.GraphicalRenderer {

  /// <summary>
  /// The support info class provides a very basic way to notify that something
  /// is fully supported, partially supported, or not supported.  The struct contains
  /// a support type, which can be either Full, Warning, or Error.  The struct
  /// also contains a message to give information about the support.
  /// </summary>
  [Serializable]
  public struct SupportInfo {
    public SupportType support;
    public string message;

    /// <summary>
    /// Helper getter to return a struct that signifies full support.
    /// </summary>
    public static SupportInfo FullSupport() {
      return new SupportInfo() { support = SupportType.Full, message = null };
    }

    /// <summary>
    /// Helper getter to return a struct that signifies partial support with a warning message.
    /// </summary>
    public static SupportInfo Warning(string message) {
      return new SupportInfo() { support = SupportType.Warning, message = message };
    }

    /// <summary>
    /// Helper getter to return a struct that signifies no support with an error message.
    /// </summary>
    public static SupportInfo Error(string message) {
      return new SupportInfo() { support = SupportType.Error, message = message };
    }

    /// <summary>
    /// Helper method that returns either the current support info struct, or the argument
    /// support info struct.  The argument is only chosen as the return value if it has
    /// less support than the current support.
    /// </summary>
    public SupportInfo OrWorse(SupportInfo other) {
      if (other.support > support) {
        return other;
      } else {
        return this;
      }
    }
  }

  public enum SupportType {
    Full,
    Warning,
    Error
  }

  public static class SupportUtil {

    /// <summary>
    /// Helper method to only support the first element in a list.
    /// </summary>
    public static void OnlySupportFirstFeature<T>(List<SupportInfo> info) {
      for (int i = 1; i < info.Count; i++) {
        info[i] = SupportInfo.Error("Only the first " + LeapGraphicTagAttribute.GetTagName(typeof(T)) + " is supported.");
      }
    }
  }
}
