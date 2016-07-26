using UnityEngine;
using UnityEditor;
using UnityTest;
using System;
using Leap.Unity.Attributes;

namespace Leap.Unity.Interaction.Testing {

  [ExecuteInEditMode]
  public class SadisticTestManager : TestComponent {

    [Header("Sadistic Settings")]
    [SerializeField]
    private InteractionTestRecording _recording;

    [EnumFlags]
    [SerializeField]
    private SadisticInteractionBehaviour.Callback _callbacks;

    [EnumFlags]
    [SerializeField]
    private SadisticInteractionBehaviour.SadisticAction _actions;

    private bool _update = false;

    public override void OnValidate() {
      base.OnValidate();

      if (!Application.isPlaying) {
        //_update = true;
        //gameObject.SetActive(true);
      }
    }

    void Update() {
      if (_update) {
        updateChildrenTests();
        _update = false;
      }
    }

    [ContextMenu("Update tests")]
    private void updateChildrenTests() {
      Transform[] transforms = GetComponentsInChildren<Transform>(true);
      foreach (Transform child in transforms) {
        if (child != transform) {
          DestroyImmediate(child.gameObject);
        }
      }

      var actionType = typeof(SadisticInteractionBehaviour.SadisticAction);
      var callbackType = typeof(SadisticInteractionBehaviour.Callback);

      int[] actionValues = (int[])Enum.GetValues(actionType);
      int[] callbackValues = (int[])Enum.GetValues(callbackType);
      string[] actionNames = Enum.GetNames(actionType);
      string[] callbackNames = Enum.GetNames(callbackType);

      for (int i = actionValues.Length; i-- != 0;) {
        var actionValue = actionValues[i];
        if (((int)_actions & actionValue) == 0) continue;

        for (int j = callbackValues.Length; j-- != 0;) {
          var callbackValue = callbackValues[j];
          if (((int)_callbacks & callbackValue) == 0) continue;

          GameObject testObj = new GameObject(ObjectNames.NicifyVariableName(callbackNames[j]) +
                                             " " +
                                             ObjectNames.NicifyVariableName(actionNames[i]));
          testObj.transform.parent = transform;

          var test = testObj.AddComponent<SadisticTest>();
          test.sadisticDefinition = new SadisticInteractionBehaviour.SadisticDef(
                                           (SadisticInteractionBehaviour.Callback)callbackValue,
                                           (SadisticInteractionBehaviour.SadisticAction)actionValue);
          test.recording = _recording;
          test.timeout = timeout;
        }
      }
    }
  }
}
