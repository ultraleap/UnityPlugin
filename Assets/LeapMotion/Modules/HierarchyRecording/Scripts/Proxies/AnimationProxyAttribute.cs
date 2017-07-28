using System;
using System.Collections.Generic;

[AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = true)]
public class AnimationProxyAttribute : Attribute {
  private static Dictionary<Type, Type> _typeToPlayback = new Dictionary<Type, Type>();

  public readonly Type playbackType;

  public AnimationProxyAttribute(Type playbackType) {
    this.playbackType = playbackType;
  }

  public static bool IsAnimationProxy(object obj) {
    return IsAnimationProxy(obj.GetType());
  }

  public static bool IsAnimationProxy(Type type) {
    return ConvertToPlaybackType(type) != null;
  }

  public static Type ConvertToPlaybackType(Type recordingType) {
    Type playbackType;
    if (!_typeToPlayback.TryGetValue(recordingType, out playbackType)) {
      var attributes = recordingType.GetCustomAttributes(typeof(AnimationProxyAttribute), inherit: true);
      if (attributes.Length > 0) {
        playbackType = (attributes[0] as AnimationProxyAttribute).playbackType;
      } else {
        playbackType = null;
      }
    }

    return playbackType;
  }


}
