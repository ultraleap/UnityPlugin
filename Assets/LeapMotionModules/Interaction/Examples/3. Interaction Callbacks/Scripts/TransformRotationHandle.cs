/******************************************************************************
 * Copyright (C) Leap Motion, Inc. 2011-2017.                                 *
 * Leap Motion proprietary and  confidential.                                 *
 *                                                                            *
 * Use subject to the terms of the Leap Motion SDK Agreement available at     *
 * https://developer.leapmotion.com/sdk_agreement, or another agreement       *
 * between Leap Motion and you, your company or other organization.           *
 ******************************************************************************/

using Leap.Unity.Interaction;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Leap.Unity.Examples {

  [AddComponentMenu("")]
  public class TransformRotationHandle : TransformHandle {

    protected override void Start() {
      // Populates _intObj with the InteractionBehaviour, and _tool with the TransformTool.
      base.Start();

      // Subscribe to OnGraspedMovement; all of the logic will happen when the handle is moved via grasping.
      _intObj.OnGraspedMovement += onGraspedMovement;
    }

    private void onGraspedMovement(Vector3 presolvePos, Quaternion presolveRot, Vector3 solvedPos, Quaternion solvedRot, List<InteractionController> controllers) {
      /* 
       * The RotationHandle works very similarly to the TranslationHandle.
       * 
       * We use OnGraspedMovement to get the position and rotation of this object
       * before and after it was moved by its grapsing hand. We calculate how the handle
       * would have rotated and report that to the Transform Tool, and then we move
       * the handle back where it was before it was moved, because the Tool will
       * actually move all of its handles at the end of the frame.
       */

      // Constrain the position of the handle and determine the resulting rotation required to get there.
      Vector3 presolveToolToHandle = presolvePos - _tool.transform.position;
      Vector3 solvedToolToHandleDirection = (solvedPos - _tool.transform.position).normalized;
      Vector3 constrainedToolToHandle = Vector3.ProjectOnPlane(solvedToolToHandleDirection, (presolveRot * Vector3.up)).normalized * presolveToolToHandle.magnitude;
      Quaternion deltaRotation = Quaternion.FromToRotation(presolveToolToHandle, constrainedToolToHandle);

      // Notify the tool about the calculated rotation.
      _tool.NotifyHandleRotation(deltaRotation);

      // Move the object back to its original position, to be moved correctly later on by the Transform Tool.
      _intObj.rigidbody.position = presolvePos;
      _intObj.rigidbody.rotation = presolveRot;
    }

  }

}
