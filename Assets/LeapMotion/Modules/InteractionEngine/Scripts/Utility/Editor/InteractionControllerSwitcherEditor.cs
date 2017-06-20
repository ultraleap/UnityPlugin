/******************************************************************************
 * Copyright (C) Leap Motion, Inc. 2011-2017.                                 *
 * Leap Motion proprietary and  confidential.                                 *
 *                                                                            *
 * Use subject to the terms of the Leap Motion SDK Agreement available at     *
 * https://developer.leapmotion.com/sdk_agreement, or another agreement       *
 * between Leap Motion and you, your company or other organization.           *
 ******************************************************************************/

using UnityEngine;
using UnityEditor;
using UnityEditorInternal;

namespace Leap.Unity.Interaction {

  [CustomEditor(typeof(InteractionControllerSwitcher))]
  public class InteractionControllerSwitcherEditor : CustomEditorBase<InteractionControllerSwitcher> {

    private ReorderableList leftList;
    private ReorderableList rightList;
    protected override void OnEnable() {
      base.OnEnable();
      leftList = new ReorderableList(serializedObject,
                                 serializedObject.FindProperty("leftHandControllers"),
                                 true, true, false, false);
      leftList.drawHeaderCallback = (Rect rect) => { EditorGUI.LabelField(rect, "Left Controller Priority"); };
      leftList.drawElementCallback = (Rect rect, int index, bool isActive, bool isFocused) => { EditorGUI.PropertyField(rect, leftList.serializedProperty.GetArrayElementAtIndex(index)); };
      leftList.elementHeight = EditorGUIUtility.singleLineHeight;
      specifyCustomDrawer("leftHandControllers", (SerializedProperty p) => { leftList.DoLayoutList(); });

      rightList = new ReorderableList(serializedObject,
                           serializedObject.FindProperty("rightHandControllers"),
                           true, true, false, false);
      rightList.drawHeaderCallback = (Rect rect) => { EditorGUI.LabelField(rect, "Right Controller Priority"); };
      rightList.drawElementCallback = (Rect rect, int index, bool isActive, bool isFocused) => { EditorGUI.PropertyField(rect, rightList.serializedProperty.GetArrayElementAtIndex(index)); };
      rightList.elementHeight = EditorGUIUtility.singleLineHeight;
      specifyCustomDrawer("rightHandControllers", (SerializedProperty p) => { rightList.DoLayoutList(); });
    }
  }

}
