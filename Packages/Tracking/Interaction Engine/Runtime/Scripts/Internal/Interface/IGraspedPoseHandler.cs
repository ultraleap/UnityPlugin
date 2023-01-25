/******************************************************************************
 * Copyright (C) Ultraleap, Inc. 2011-2023.                                   *
 *                                                                            *
 * Use subject to the terms of the Apache License 2.0 available at            *
 * http://www.apache.org/licenses/LICENSE-2.0, or another agreement           *
 * between Ultraleap and you, your company or other organization.             *
 ******************************************************************************/

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Leap.Unity.Interaction
{

    /// <summary>
    /// An IGraspedPoseHandler specifies where an object grasped by a Leap hand or
    /// controller (or multiple hands/controllers) should move as the grasping
    /// controllers(s) move. The default implementation provided by the Interaction Engine
    /// is the KabschGraspedPose, which produces a physically-intuitive following motion
    /// for the object no matter how a grasping hand moves.
    /// 
    /// IGraspedPoseHandlers do not actually move an object; they only calculate where
    /// an object should be moved. Actually moving the object is the concern of an
    /// IGraspedMovementHandler.
    /// </summary>
    public interface IGraspedPoseHandler
    {

        /// <summary>
        /// Called when a new InteractionController begins grasping a certain object.
        /// The controller or Leap hand should be included in the held pose calculation.
        /// </summary>
        void AddController(InteractionController controller);

        /// <summary>
        /// Called when an InteractionController stops grasping a certain object; the
        /// controller should no longer be included in the held pose calculation.
        /// </summary>
        void RemoveController(InteractionController controller);

        /// <summary>
        /// Called e.g. if the InteractionBehaviour is set not to move while being grasped;
        /// this should clear any hands to be included in the grasping position calculation.
        /// </summary>
        void ClearControllers();

        /// <summary>
        /// Calculate the best holding position and orientation given the current state of
        /// all InteractionControllers (added via AddController()).
        /// </summary>
        void GetGraspedPosition(out Vector3 position, out Quaternion rotation);

    }

}