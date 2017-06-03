/******************************************************************************
 * Copyright (C) Leap Motion, Inc. 2011-2017.                                 *
 * Leap Motion proprietary and  confidential.                                 *
 *                                                                            *
 * Use subject to the terms of the Leap Motion SDK Agreement available at     *
 * https://developer.leapmotion.com/sdk_agreement, or another agreement       *
 * between Leap Motion and you, your company or other organization.           *
 ******************************************************************************/

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Leap.Unity.Attributes {

  public class DisableAttribute : CombinablePropertyAttribute, IPropertyDisabler {

#if UNITY_EDITOR
    public bool ShouldDisable(SerializedProperty property) {
      return true;
    }
#endif
  }
}
