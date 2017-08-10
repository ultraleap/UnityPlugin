/******************************************************************************
 * Copyright (C) Leap Motion, Inc. 2011-2017.                                 *
 * Leap Motion proprietary and  confidential.                                 *
 *                                                                            *
 * Use subject to the terms of the Leap Motion SDK Agreement available at     *
 * https://developer.leapmotion.com/sdk_agreement, or another agreement       *
 * between Leap Motion and you, your company or other organization.           *
 ******************************************************************************/

using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using Leap.Unity.Query;

namespace Leap.Unity.GraphicalRenderer {

  [CanEditMultipleObjects]
  [CustomEditor(typeof(LeapPanelOutlineGraphic), editorForChildClasses: true)]
  public class LeapPanelOutlineGraphicEditor : LeapSlicedGraphicEditor {

    protected override void OnEnable() {
      base.OnEnable();

      specifyConditionalDrawing(() => targets.Query()
                                             .Cast<LeapPanelOutlineGraphic>()
                                             .Any(t => !t.nineSliced || t.overrideSpriteBorders),
                                "_thickness");
    }

  }

}
