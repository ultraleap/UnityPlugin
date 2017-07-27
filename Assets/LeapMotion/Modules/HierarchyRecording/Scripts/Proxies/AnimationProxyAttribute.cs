using System;

[AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = true)]
public class AnimationProxyAttribute : Attribute {
  public readonly Type playbackType;

  public AnimationProxyAttribute(Type playbackType) {
    this.playbackType = playbackType;
  }

  public static bool IsAnimationProxy(object obj) {
    return IsAnimationProxy(obj.GetType());
  }

  public static bool IsAnimationProxy(Type type) {
    return type.GetCustomAttributes(typeof(AnimationProxyAttribute), inherit: true).Length > 0;
  }

  public static Type ConvertToPlaybackType(Type recordingType) {
    var attributes = recordingType.GetCustomAttributes(typeof(AnimationProxyAttribute), inherit: true);
    if (attributes.Length > 0) {
      return (attributes[0] as AnimationProxyAttribute).playbackType;
    } else {
      throw new Exception("Not a proxy type!");
    }
  }
}
