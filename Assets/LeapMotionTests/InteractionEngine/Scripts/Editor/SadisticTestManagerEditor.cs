using UnityEditor;

namespace Leap.Unity.Interaction.Testing {

  [CustomEditor(typeof(SadisticTestManager))]
  public class SadisticTestManagerEditor : CustomEditorBase<SadisticTestManager> {

    public override void OnInspectorGUI() {
      base.OnInspectorGUI();

      if (_modifiedProperties.Count > 0) {
        target.UpdateChildrenTests();
      }
    }
  }
}
