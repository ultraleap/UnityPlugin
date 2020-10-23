/******************************************************************************
 * Copyright (C) Ultraleap, Inc. 2011-2020.                                   *
 *                                                                            *
 * Use subject to the terms of the Apache License 2.0 available at            *
 * http://www.apache.org/licenses/LICENSE-2.0, or another agreement           *
 * between Ultraleap and you, your company or other organization.             *
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
