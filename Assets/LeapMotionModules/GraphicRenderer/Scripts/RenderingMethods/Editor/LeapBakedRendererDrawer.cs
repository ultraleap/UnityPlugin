using UnityEditor;

namespace Leap.Unity.GraphicalRenderer {

  [CustomPropertyDrawer(typeof(LeapBakedRenderer))]
  public class LeapBakedRendererDrawer : LeapMesherBaseDrawer {

    protected override void init(SerializedProperty property) {
      base.init(property);

      drawProperty("_motionType");
      drawProperty("_createMeshRenderers");
    }
  }
}
