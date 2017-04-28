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
    public class GeneralComparer : ComparerBase
    {
        public enum CompareType { AEqualsB, ANotEqualsB }

        public CompareType compareType;

        protected override bool Compare(object a, object b)
        {
            if (compareType == CompareType.AEqualsB)
                return a.Equals(b);
            if (compareType == CompareType.ANotEqualsB)
                return !a.Equals(b);
            throw new Exception();
        }
    }
}
