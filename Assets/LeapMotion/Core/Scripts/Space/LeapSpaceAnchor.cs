/******************************************************************************
 * Copyright (C) Leap Motion, Inc. 2011-2018.                                 *
 * Leap Motion proprietary and confidential.                                  *
 *                                                                            *
 * Use subject to the terms of the Leap Motion SDK Agreement available at     *
 * https://developer.leapmotion.com/sdk_agreement, or another agreement       *
 * between Leap Motion and you, your company or other organization.           *
 ******************************************************************************/

using UnityEngine;
using System;

namespace Leap.Unity.Space {

  [DisallowMultipleComponent]
  public class LeapSpaceAnchor : MonoBehaviour {

    [HideInInspector]
    public LeapSpaceAnchor parent;

    [HideInInspector]
    public LeapSpace space;

    public ITransformer transformer;

    protected virtual void OnEnable() { }

    protected virtual void OnDisable() { }

    public void RecalculateParentAnchor() {
      if (this is LeapSpace) {
        parent = this;
      } else {
        parent = GetAnchor(transform.parent);
      }
    }

    public static LeapSpaceAnchor GetAnchor(Transform root) {
      while (true) {
        if (root == null) {
          return null;
        }

        var anchor = root.GetComponent<LeapSpaceAnchor>();
        if (anchor != null && anchor.enabled) {
          return anchor;
        }

        root = root.parent;
      }
    }
  }
}
