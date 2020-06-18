/******************************************************************************
 * Copyright (C) Ultraleap, Inc. 2011-2020.                                   *
 *                                                                            *
 * Use subject to the terms of the Apache License 2.0 available at            *
 * http://www.apache.org/licenses/LICENSE-2.0, or another agreement           *
 * between Ultraleap and you, your company or other organization.             *
 ******************************************************************************/

using UnityEditor;

namespace Leap.Unity.Recording {

  [CustomEditor(typeof(OnUnityCallback))]
  public class OnUnityCallbackEditor : CustomEditorBase<OnUnityCallback> {

    private SerializedProperty _tableProp;
    private EnumEventTableEditor _tableEditor;

    protected override void OnEnable() {
      base.OnEnable();

      _tableProp = serializedObject.FindProperty("_table");
      _tableEditor = new EnumEventTableEditor(_tableProp, typeof(OnUnityCallback.CallbackType));

      specifyCustomDrawer("_table", drawTable);
    }

    private void drawTable(SerializedProperty p) {
      _tableEditor.DoGuiLayout();
    }
  }
}
