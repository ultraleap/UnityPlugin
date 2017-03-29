using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif
using Leap.Unity.Attributes;

public interface ICustomChannelFeature {
  string channelName { get; }
}

public abstract class CustomChannelFeatureBase<T> : LeapGuiFeature<T>, ICustomChannelFeature
  where T : LeapGuiElementData {

  [EditTimeOnly]
  [SerializeField]
  private string _channelName = "_CustomChannel";

  public string channelName {
    get {
      return _channelName;
    }
  }

  public override SupportInfo GetSupportInfo(LeapGuiGroup group) {
    foreach (var feature in group.features) {
      if (feature == this) continue;

      var channelFeature = feature as ICustomChannelFeature;
      if (channelFeature != null && channelFeature.channelName == channelName) {
        return SupportInfo.Error("Cannot have two custom channels with the same name.");
      }
    }

    return SupportInfo.FullSupport();
  }

#if UNITY_EDITOR
  public override void DrawFeatureEditor(Rect rect, bool isActive, bool isFocused) {
    _channelName = EditorGUI.TextField(rect, "Channel name", _channelName);
  }

  public override float GetEditorHeight() {
    return EditorGUIUtility.singleLineHeight;
  }
#endif
}
