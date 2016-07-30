using UnityEditor;
using System.Collections.Generic;

namespace Leap.Unity.Interaction.Testing {

  [CustomEditor(typeof(SadisticTestManager))]
  public class SadisticTestManagerEditor : CustomEditorBase {

    public override void OnInspectorGUI() {
      base.OnInspectorGUI();

      if (_modifiedProperties.Count > 0) {
        (target as SadisticTestManager).UpdateChildrenTests();
      }
    }
  }
}
