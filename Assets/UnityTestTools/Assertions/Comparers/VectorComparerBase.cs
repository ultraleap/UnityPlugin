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
    public abstract class VectorComparerBase<T> : ComparerBaseGeneric<T>
    {
        protected bool AreVectorMagnitudeEqual(float a, float b, double floatingPointError)
        {
            if (Math.Abs(a) < floatingPointError && Math.Abs(b) < floatingPointError)
                return true;
            if (Math.Abs(a - b) < floatingPointError)
                return true;
            return false;
        }
    }
}
