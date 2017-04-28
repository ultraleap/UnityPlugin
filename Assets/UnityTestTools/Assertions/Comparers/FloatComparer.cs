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
    public class FloatComparer : ComparerBaseGeneric<float>
    {
        public enum CompareTypes
        {
            Equal,
            NotEqual,
            Greater,
            Less
        }

        public CompareTypes compareTypes;
        public double floatingPointError = 0.0001f;

        protected override bool Compare(float a, float b)
        {
            switch (compareTypes)
            {
                case CompareTypes.Equal:
                    return Math.Abs(a - b) < floatingPointError;
                case CompareTypes.NotEqual:
                    return Math.Abs(a - b) > floatingPointError;
                case CompareTypes.Greater:
                    return a > b;
                case CompareTypes.Less:
                    return a < b;
            }
            throw new Exception();
        }
        public override int GetDepthOfSearch()
        {
            return 3;
        }
    }
}
