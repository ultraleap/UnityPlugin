/******************************************************************************
 * Copyright (C) Leap Motion, Inc. 2011-2017.                                 *
 * Leap Motion proprietary and  confidential.                                 *
 *                                                                            *
 * Use subject to the terms of the Leap Motion SDK Agreement available at     *
 * https://developer.leapmotion.com/sdk_agreement, or another agreement       *
 * between Leap Motion and you, your company or other organization.           *
 ******************************************************************************/

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Leap.Unity.Examples {

  /// <summary>
  /// This utility script scans through its Transform's children and tells PhysX to
  /// ignore collisions between all pairs of Colliders it finds. This is particlarly
  /// useful, for example, for interfaces, where buttons shouldn't collide with other
  /// buttons.
  /// 
  /// This is not the recommended strategy in general: It is much more optimal for your
  /// application to put interface objects on a layer and disable self-collision for that
  /// layer in your Physics settings (Edit / Project Settings / Physics).
  /// </summary>
  public class IgnoreCollisionsInChildren : MonoBehaviour {

    void Start() {
      IgnoreCollisionsInChildrenOf(this.transform);
    }

    public static void IgnoreCollisionsInChildrenOf(Transform t, bool ignore = true) {
      var colliders = Pool<List<Collider>>.Spawn();
      try {
        t.GetComponentsInChildren<Collider>(true, colliders);

        for (int i = 0; i < colliders.Count; i++) {
          for (int j = 0; j < colliders.Count; j++) {
            if (i == j) continue;

            Physics.IgnoreCollision(colliders[i], colliders[j], ignore);
          }
        }
      }
      finally {
        colliders.Clear();
        Pool<List<Collider>>.Recycle(colliders);
      }
    }

  }

}
