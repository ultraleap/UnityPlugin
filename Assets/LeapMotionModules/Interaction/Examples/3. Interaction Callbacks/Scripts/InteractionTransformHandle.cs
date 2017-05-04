/******************************************************************************
 * Copyright (C) Leap Motion, Inc. 2011-2017.                                 *
 * Leap Motion proprietary and  confidential.                                 *
 *                                                                            *
 * Use subject to the terms of the Leap Motion SDK Agreement available at     *
 * https://developer.leapmotion.com/sdk_agreement, or another agreement       *
 * between Leap Motion and you, your company or other organization.           *
 ******************************************************************************/

using Leap.Unity.UI.Interaction;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Leap.Unity.Examples.TransformHandles {

  [AddComponentMenu("")]
  [RequireComponent(typeof(InteractionBehaviour))]
  public abstract class InteractionTransformHandle : MonoBehaviour {

    // We'll hide the MeshRenderer when a hand isn't hovering nearby this handle.
    private const float MIN_RENDER_DISTANCE = 0.30F; // In this case, "nearby" means 30 cm.
    private MeshRenderer _meshRenderer;

    protected TransformTool _tool;
    protected InteractionBehaviour _intObj;

    public Action<InteractionTransformHandle> onHandleActivated = (handle) => { };
    public Action<InteractionTransformHandle> onHandleDeactivated = (handle) => { };

    protected virtual void Awake() {
      _tool = GetComponentInParent<TransformTool>();

      if (_tool == null) {
        Debug.LogError("Couldn't find a TransformTool in one of an InteractionTransformHandle's parent transforms.", this.gameObject);
      }

      _intObj = GetComponent<InteractionBehaviour>();
      _meshRenderer = GetComponent<MeshRenderer>();
    }

    protected virtual void Update() {
      if (_intObj.isHovered && Vector3.Distance(this.transform.position,
                                               _intObj.closestHoveringHand.PalmPosition.ToVector3())
                                                  < MIN_RENDER_DISTANCE) {
        if (_meshRenderer != null) _meshRenderer.enabled = true;
      }
      else {
        if (_meshRenderer != null) _meshRenderer.enabled = false;
      }
    }

  }

}
