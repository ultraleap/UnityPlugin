/******************************************************************************
 * Copyright (C) Leap Motion, Inc. 2011-2018.                                 *
 * Leap Motion proprietary and confidential.                                  *
 *                                                                            *
 * Use subject to the terms of the Leap Motion SDK Agreement available at     *
 * https://developer.leapmotion.com/sdk_agreement, or another agreement       *
 * between Leap Motion and you, your company or other organization.           *
 ******************************************************************************/

using System.Collections.Generic;
using UnityEngine;

namespace Leap.Unity.Interaction.Internal {

  public delegate void GraspedMovementEvent(Vector3 oldPosition, Quaternion oldRotation,
                                            Vector3 newPosition, Quaternion newRotation,
                                            List<InteractionController> graspingControllers);

}
