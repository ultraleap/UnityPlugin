/****************************************************************************** 
 * Copyright (C) Leap Motion, Inc. 2011-2017.                                 * 
 * Leap Motion proprietary and  confidential.                                 * 
 *                                                                            * 
 * Use subject to the terms of the Leap Motion SDK Agreement available at     * 
 * https://developer.leapmotion.com/sdk_agreement, or another agreement       * 
 * between Leap Motion and you, your company or other organization.           * 
 ******************************************************************************/

using System;
using System.Collections.Generic;
using UnityEngine;

namespace UnityTest
{
    public class IsRenderedByCamera : ComparerBaseGeneric<Renderer, Camera>
    {
        public enum CompareType
        {
            IsVisible,
            IsNotVisible,
        };

        public CompareType compareType;

        protected override bool Compare(Renderer renderer, Camera camera)
        {
            var planes = GeometryUtility.CalculateFrustumPlanes(camera);
            var isVisible = GeometryUtility.TestPlanesAABB(planes, renderer.bounds);
            switch (compareType)
            {
                case CompareType.IsVisible:
                    return isVisible;
                case CompareType.IsNotVisible:
                    return !isVisible;
            }
            throw new Exception();
        }
    }
}
