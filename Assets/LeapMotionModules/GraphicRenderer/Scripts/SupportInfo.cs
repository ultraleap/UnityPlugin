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
        info[i] = SupportInfo.Error("This renderer only supports a single " +
                                            LeapGraphicTagAttribute.GetTag(typeof(T)) +
                                            " feature.");
      }
    }
  }
}
