/******************************************************************************
 * Copyright (C) Leap Motion, Inc. 2011-2018.                                 *
 * Leap Motion proprietary and confidential.                                  *
 *                                                                            *
 * Use subject to the terms of the Leap Motion SDK Agreement available at     *
 * https://developer.leapmotion.com/sdk_agreement, or another agreement       *
 * between Leap Motion and you, your company or other organization.           *
 ******************************************************************************/

using Leap.Unity.RuntimeGizmos;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Leap.Unity.Examples {

  [AddComponentMenu("")]
  public class RenderWireSphere : MonoBehaviour, IRuntimeGizmoComponent {

    public float radius = 0.30F;
    public Color color = Color.red;

    public void OnDrawRuntimeGizmos(RuntimeGizmoDrawer drawer) {
      if (!gameObject.activeInHierarchy) return;

      drawer.color = color;
      drawer.DrawWireSphere(this.transform.position, radius);
    }

  }

}
