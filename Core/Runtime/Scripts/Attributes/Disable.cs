/******************************************************************************
 * Copyright (C) Ultraleap, Inc. 2011-2020.                                   *
 *                                                                            *
 * Use subject to the terms of the Apache License 2.0 available at            *
 * http://www.apache.org/licenses/LICENSE-2.0, or another agreement           *
 * between Ultraleap and you, your company or other organization.             *
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
