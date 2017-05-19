/******************************************************************************
 * Copyright (C) Leap Motion, Inc. 2011-2017.                                 *
 * Leap Motion proprietary and  confidential.                                 *
 *                                                                            *
 * Use subject to the terms of the Leap Motion SDK Agreement available at     *
 * https://developer.leapmotion.com/sdk_agreement, or another agreement       *
 * between Leap Motion and you, your company or other organization.           *
 ******************************************************************************/

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
