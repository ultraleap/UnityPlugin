using UnityEngine;
using UnityTest;

namespace Leap.Unity.Interaction.Testing {
  
  [ExecuteInEditMode]
  public class SadisticTestManager : TestComponent {

    [SerializeField]
    private SadisticInteractionBehaviour.SadisticAction _actions;

    [SerializeField]
    private SadisticInteractionBehaviour.Callback _callbacks;

    private bool _update = false;

    public override void OnValidate() {
      base.OnValidate();

      if (!Application.isPlaying) {
        _update = true;
      }
    }

    void Update() {
      if (_update) {
        updateChildrenTests();
        _update = false;
      }
    }

    private void updateChildrenTests() {
      Transform[] transforms = GetComponentsInChildren<Transform>(true);
      foreach (Transform child in transforms) {
        if (child != transform) {
          DestroyImmediate(child.gameObject);
        }
      }

      /*

      for (int i = 0; i < SadisticInteractionBehaviour.definitions.Count; i++) {
        GameObject childObj = new GameObject("Sadistic Test " + i);
        childObj.transform.parent = transform;
        var childTest = childObj.AddComponent<SadisticTest>();
        childTest.sadisticTestIndex = i;
        childTest.timeout = timeout;
      }
      */
    }
  }
}
