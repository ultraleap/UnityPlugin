/******************************************************************************
 * Copyright (C) Ultraleap, Inc. 2011-2020.                                   *
 * Ultraleap proprietary and confidential.                                    *
 *                                                                            *
 * Use subject to the terms of the Leap Motion SDK Agreement available at     *
 * https://developer.leapmotion.com/sdk_agreement, or another agreement       *
 * between Ultraleap and you, your company or other organization.             *
 ******************************************************************************/

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Leap.Unity.Interaction {

  public interface IInternalInteractionController {

    void FixedUpdateController();

    bool CheckHoverEnd(out HashSet<IInteractionBehaviour> hoverEndedObjects);
    bool CheckHoverBegin(out HashSet<IInteractionBehaviour> hoverBeganObjects);
    bool CheckHoverStay(out HashSet<IInteractionBehaviour> hoveredObjects);

    bool CheckPrimaryHoverEnd(out IInteractionBehaviour primaryHoverEndedObject);
    bool CheckPrimaryHoverBegin(out IInteractionBehaviour primaryHoverBeganObject);
    bool CheckPrimaryHoverStay(out IInteractionBehaviour primaryHoveredObject);

    bool CheckContactEnd(out HashSet<IInteractionBehaviour> contactEndedObjects);
    bool CheckContactBegin(out HashSet<IInteractionBehaviour> contactBeganObjects);
    bool CheckContactStay(out HashSet<IInteractionBehaviour> contactedObjects);

    bool CheckGraspEnd(out IInteractionBehaviour releasedObject);
    bool CheckGraspBegin(out IInteractionBehaviour newlyGraspedObject);
    bool CheckGraspHold(out IInteractionBehaviour graspedObject);

    bool CheckSuspensionBegin(out IInteractionBehaviour suspendedObject);
    bool CheckSuspensionEnd(out IInteractionBehaviour resumedObject);

  }

}
