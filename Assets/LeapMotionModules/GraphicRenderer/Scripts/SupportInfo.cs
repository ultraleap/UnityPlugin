/******************************************************************************
 * Copyright (C) Leap Motion, Inc. 2011-2017.                                 *
 * Leap Motion proprietary and  confidential.                                 *
 *                                                                            *
 * Use subject to the terms of the Leap Motion SDK Agreement available at     *
 * https://developer.leapmotion.com/sdk_agreement, or another agreement       *
 * between Leap Motion and you, your company or other organization.           *
 ******************************************************************************/

using System;
using System.Collections.Generic;

namespace Leap.Unity.GraphicalRenderer {

  [Serializable]
  public struct SupportInfo {
    public SupportType support;
    public string message;

    public static SupportInfo FullSupport() {
      return new SupportInfo() { support = SupportType.Full, message = null };
    }

    public static SupportInfo Warning(string message) {
      return new SupportInfo() { support = SupportType.Warning, message = message };
    }

    public static SupportInfo Error(string message) {
      return new SupportInfo() { support = SupportType.Error, message = message };
    }

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
    public static void OnlySupportFirstFeature<T>(List<T> features, List<SupportInfo> info)
      where T : LeapGraphicFeatureBase {
      for (int i = 1; i < features.Count; i++) {
        info[i] = SupportInfo.Error("Only the first " + LeapGraphicTagAttribute.GetTagName(typeof(T)) + " is supported.");
      }
    }
  }
}
