using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public interface ISupportsFeature<T> where T : LeapGuiFeatureBase {
  /// <summary>
  /// Must be implemented by a renderer to report what level of support
  /// it has for all features of this type.  
  /// 
  /// The 'features' list will
  /// contain all features requested in priority order, and the 'info'
  /// list will come pre-filled with full-support info items.  The
  /// renderer must change these full-support items to a warning or
  /// error item to reflect what it is able to support.
  /// 
  /// This method will NEVER be called if there are 0 features of type T.
  /// </summary>
  void GetSupportInfo(List<T> features, List<FeatureSupportInfo> info);
}

public struct FeatureSupportInfo {
  public SupportType support;
  public string message;

  public static FeatureSupportInfo FullSupport() {
    return new FeatureSupportInfo() { support = SupportType.Full, message = null };
  }

  public static FeatureSupportInfo Warning(string message) {
    return new FeatureSupportInfo() { support = SupportType.Warning, message = message };
  }

  public static FeatureSupportInfo Error(string message) {
    return new FeatureSupportInfo() { support = SupportType.Error, message = message };
  }

  public FeatureSupportInfo OrWorse(FeatureSupportInfo other) {
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

public static class FeatureSupportUtil {
  public static void OnlySupportFirstFeature<T>(List<T> features, List<FeatureSupportInfo> info)
    where T : LeapGuiFeatureBase {
    for (int i = 1; i < features.Count; i++) {
      info[i] = FeatureSupportInfo.Error("This renderer only supports a single " +
                                          LeapGuiFeatureNameAttribute.GetFeatureName(typeof(T)) +
                                          " feature.");
    }
  }
}
