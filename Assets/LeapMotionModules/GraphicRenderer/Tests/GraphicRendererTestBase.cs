using UnityEngine;

namespace Leap.Unity.GraphicalRenderer.Tests {

  public abstract class GraphicRendererTestBase {

    protected LeapGraphicRenderer renderer;

    protected LeapGraphic oneGraphic;

    protected LeapGraphicGroup firstGroup;

    protected LeapGraphicGroup secondGroup;

    protected void InitTest(string prefabName) {
      var prefab = Resources.Load<GameObject>(prefabName);
      var obj = Object.Instantiate(prefab);

      renderer = obj.GetComponent<LeapGraphicRenderer>();

      oneGraphic = renderer.GetComponentInChildren<LeapGraphic>(includeInactive: true);

      firstGroup = renderer.groups[0];

      secondGroup = renderer.groups.Count > 1 ? renderer.groups[1] : null;
    }

    protected LeapGraphic CreateGraphic(string prefabName) {
      var prefab = Resources.Load<GameObject>(prefabName);
      var obj = Object.Instantiate(prefab);
      obj.transform.SetParent(renderer.transform);

      var graphic = obj.GetComponent<LeapGraphic>();

      if (oneGraphic == null) {
        oneGraphic = graphic;
      }

      return graphic;
    }
  }
}
