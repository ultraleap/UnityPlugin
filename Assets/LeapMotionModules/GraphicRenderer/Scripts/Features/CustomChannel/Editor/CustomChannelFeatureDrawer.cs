using UnityEditor;

namespace Leap.Unity.GraphicalRenderer {

  [CustomPropertyDrawer(typeof(CustomFloatChannelFeature))]
  [CustomPropertyDrawer(typeof(CustomVectorChannelFeature))]
  [CustomPropertyDrawer(typeof(CustomColorChannelFeature))]
  [CustomPropertyDrawer(typeof(CustomMatrixChannelFeature))]
  public class CustomChannelFeatureDrawer : CustomPropertyDrawerBase {

    protected override void init(SerializedProperty property) {
      base.init(property);

      drawProperty("_channelName");
    }
  }
}
