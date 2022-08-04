/******************************************************************************
 * Copyright (C) Ultraleap, Inc. 2011-2022.                                   *
 *                                                                            *
 * Use subject to the terms of the Apache License 2.0 available at            *
 * http://www.apache.org/licenses/LICENSE-2.0, or another agreement           *
 * between Ultraleap and you, your company or other organization.             *
 ******************************************************************************/
using Leap.Unity.Interaction;
using Leap.Unity.Interaction.PhysicsHands;
using Leap.Unity.Preview.HandRays;
using UnityEngine;

public class HandRayStraightLineRenderer : HandRayRenderer
{
    public float lineRendererDistance = 50f;

    protected override bool UpdateLineRendererLogic(HandRayDirection handRayDirection, out RaycastHit raycastResult)
    {
        Vector3 lineRendererEndPos;
        raycastResult = new RaycastHit();
        bool hit = false;
        if (Physics.Raycast(new Ray(handRayDirection.RayOrigin, handRayDirection.Direction), out raycastResult, lineRendererDistance, _layerMask))
        {
            hit = true;
            lineRendererEndPos = raycastResult.point;
        }
        else
        {
            lineRendererEndPos = handRayDirection.RayOrigin + handRayDirection.Direction * lineRendererDistance;
        }
        UpdateLineRendererPositions(2, new Vector3[]{ handRayDirection.VisualAimPosition, lineRendererEndPos});
        return hit;
    }
}