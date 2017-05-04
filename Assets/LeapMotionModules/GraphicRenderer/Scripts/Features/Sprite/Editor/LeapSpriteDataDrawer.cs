using System;
using UnityEditor;

namespace Leap.Unity.GraphicalRenderer {

  [CustomPropertyDrawer(typeof(LeapSpriteData))]
  public class LeapSpriteDataDrawer : CustomPropertyDrawerBase {

    protected override void init(SerializedProperty property) {
      base.init(property);

      var channelFeature = LeapGraphicEditor.currentFeature as LeapSpriteFeature;
      Func<string> nameFunc = () => {
        if (channelFeature == null) {
          return null;
        } else {
          return channelFeature.propertyName;
        }
      };

      drawProperty("sprite", nameFunc);
    }
  }
}
