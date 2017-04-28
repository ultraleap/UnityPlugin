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
    public class IntComparer : ComparerBaseGeneric<int>
    {
        public enum CompareType
        {
            Equal,
            NotEqual,
            Greater,
            GreaterOrEqual,
            Less,
            LessOrEqual
        };

        public CompareType compareType;

        protected override bool Compare(int a, int b)
        {
            switch (compareType)
            {
                case CompareType.Equal:
                    return a == b;
                case CompareType.NotEqual:
                    return a != b;
                case CompareType.Greater:
                    return a > b;
                case CompareType.GreaterOrEqual:
                    return a >= b;
                case CompareType.Less:
                    return a < b;
                case CompareType.LessOrEqual:
                    return a <= b;
            }
            throw new Exception();
        }
    }
}
