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
    public class TransformComparer : ComparerBaseGeneric<Transform>
    {
        public enum CompareType { Equals, NotEquals }

        public CompareType compareType;

        protected override bool Compare(Transform a, Transform b)
        {
            if (compareType == CompareType.Equals)
            {
                return a.position == b.position;
            }
            if (compareType == CompareType.NotEquals)
            {
                return a.position != b.position;
            }
            throw new Exception();
        }
    }
}
