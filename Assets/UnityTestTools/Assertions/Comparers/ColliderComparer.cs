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
    public class ColliderComparer : ComparerBaseGeneric<Bounds>
    {
        public enum CompareType
        {
            Intersects,
            DoesNotIntersect
        };

        public CompareType compareType;

        protected override bool Compare(Bounds a, Bounds b)
        {
            switch (compareType)
            {
                case CompareType.Intersects:
                    return a.Intersects(b);
                case CompareType.DoesNotIntersect:
                    return !a.Intersects(b);
            }
            throw new Exception();
        }
    }
}
