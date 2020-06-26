/******************************************************************************
 * Copyright (C) Ultraleap, Inc. 2011-2020.                                   *
 *                                                                            *
 * Use subject to the terms of the Apache License 2.0 available at            *
 * http://www.apache.org/licenses/LICENSE-2.0, or another agreement           *
 * between Ultraleap and you, your company or other organization.             *
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
