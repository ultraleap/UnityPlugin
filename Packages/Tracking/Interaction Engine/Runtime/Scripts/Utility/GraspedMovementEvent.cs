/******************************************************************************
 * Copyright (C) Ultraleap, Inc. 2011-2023.                                   *
 *                                                                            *
 * Use subject to the terms of the Apache License 2.0 available at            *
 * http://www.apache.org/licenses/LICENSE-2.0, or another agreement           *
 * between Ultraleap and you, your company or other organization.             *
 ******************************************************************************/

using System.Collections.Generic;
using UnityEngine;

namespace Leap.Unity.Interaction.Internal
{

    public delegate void GraspedMovementEvent(Vector3 oldPosition, Quaternion oldRotation,
                                              Vector3 newPosition, Quaternion newRotation,
                                              List<InteractionController> graspingControllers);

}