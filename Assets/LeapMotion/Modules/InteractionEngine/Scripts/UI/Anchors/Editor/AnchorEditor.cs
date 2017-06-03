/******************************************************************************
 * Copyright (C) Leap Motion, Inc. 2011-2017.                                 *
 * Leap Motion proprietary and  confidential.                                 *
 *                                                                            *
 * Use subject to the terms of the Leap Motion SDK Agreement available at     *
 * https://developer.leapmotion.com/sdk_agreement, or another agreement       *
 * between Leap Motion and you, your company or other organization.           *
 ******************************************************************************/

using Leap.Unity;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Leap.Unity.Interaction {

  [CanEditMultipleObjects]
  [CustomEditor(typeof(Anchor))]
  public class AnchorEditor : CustomEditorBase<Anchor> {

    protected override void OnEnable() {
      base.OnEnable();

      deferProperty("_eventTable");
      specifyCustomDrawer("_eventTable", drawEventTable);
    }

    private EnumEventTableEditor _tableEditor;
    private void drawEventTable(SerializedProperty property) {
      if (_tableEditor == null) {
        _tableEditor = new EnumEventTableEditor(property, typeof(Anchor.EventType));
      }

      _tableEditor.DoGuiLayout();
    }

  }

}
