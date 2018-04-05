/******************************************************************************
 * Copyright (C) Leap Motion, Inc. 2011-2018.                                 *
 * Leap Motion proprietary and confidential.                                  *
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

  public enum TranslationAxis { X, Y, Z }

  [AddComponentMenu("")]
  public class TransformTranslationHandle : TransformHandle {

    public TranslationAxis axis;

    protected override void Start() {
      // Populates _intObj with the InteractionBehaviour, and _tool with the
      // TransformTool.
      base.Start();

      // Subscribe to OnGraspedMovement; all of the logic will happen when the handle is
      // moved via grasping.
      _intObj.OnGraspedMovement += onGraspedMovement;
    }

    private void onGraspedMovement(Vector3 presolvePos, Quaternion presolveRot,
                                   Vector3 solvedPos, Quaternion solvedRot,
                                   List<InteractionController> controllers) {
      /* 
       * OnGraspedMovement provides the position and rotation of the Interaction object
       * before and after it was moved by its grasping hand. This callback only occurs
       * when one or more hands is grasping the Interaction object. In this case, we
       * don't care about how many or which hands are grasping the object, only where
       * the object is moved.
       * 
       * The Translation Handle uses the pre- and post-solve movement information to
       * calculate how the user is trying to move the object along this handle's forward
       * direction. Then the Translation Handle will simply override the movement caused
       * by the grasping hand and reset itself back to its original position.
       * 
       * The movement calculated by the Handle in this method is reported to the Transform
       * Tool, which accumulates movement caused by all Handles over the course of a frame
       * and then moves the target object and all of its child Handles appropriately at
       * the end of the frame.
       */

      // Calculate the constrained movement of the handle along its forward axis only.
      Vector3 deltaPos = solvedPos - presolvePos;
      Vector3 handleForwardDirection = presolveRot * Vector3.forward;
      Vector3 deltaAxisPos = handleForwardDirection * Vector3.Dot(handleForwardDirection,  deltaPos);

      // Notify the tool about the calculated movement.
      _tool.NotifyHandleMovement(deltaAxisPos);

      // In this case, the tool itself will accumulate delta positions and delta rotations
      // from all handles, and will then synchronize handles to the appropriate positions and
      // rotations at the end of the frame.

      // Because the Tool will be the one to actually move this Handle, all we have left to do
      // is to undo all of the motion caused by the grasping hand.
      _intObj.rigidbody.position = presolvePos;
      _intObj.rigidbody.rotation = presolveRot;
    }

  }

}
