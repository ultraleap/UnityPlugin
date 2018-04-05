/******************************************************************************
 * Copyright (C) Leap Motion, Inc. 2011-2018.                                 *
 * Leap Motion proprietary and confidential.                                  *
 *                                                                            *
 * Use subject to the terms of the Leap Motion SDK Agreement available at     *
 * https://developer.leapmotion.com/sdk_agreement, or another agreement       *
 * between Leap Motion and you, your company or other organization.           *
 ******************************************************************************/

using UnityEngine;

namespace Leap.Unity.RuntimeGizmos {

  public class RuntimeColliderGizmos : MonoBehaviour, IRuntimeGizmoComponent {

    public Color color = Color.white;
    public bool useWireframe = true;
    public bool traverseHierarchy = true;
    public bool drawTriggers = false;

    /// <summary>
    /// An empty Start() method; gives the MonoBehaviour an enable/disable checkbox.
    /// </summary>
    void Start() { }

    public void OnDrawRuntimeGizmos(RuntimeGizmoDrawer drawer) {
      if (!this.gameObject.activeInHierarchy
          || !this.enabled) return;

      drawer.color = color;
      drawer.DrawColliders(gameObject, useWireframe, traverseHierarchy, drawTriggers);
    }
  }
}
