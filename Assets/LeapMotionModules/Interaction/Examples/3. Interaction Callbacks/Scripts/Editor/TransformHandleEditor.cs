using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Leap.Unity.Examples {

  [CustomEditor(typeof(TransformHandle), true)]
  [CanEditMultipleObjects]
  public class TransformHandleEditor : CustomEditorBase<TransformHandle> {

    protected override void OnEnable() {
      base.OnEnable();

      deferProperty("OnHandleDeactivated");
      deferProperty("OnHandleActivated");
      deferProperty("OnShouldHideHandle");
      deferProperty("OnShouldShowHandle");
    }

  }

}
