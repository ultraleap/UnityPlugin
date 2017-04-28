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
    public class ValueDoesNotChange : ActionBase
    {
        private object m_Value;

        protected override bool Compare(object a)
        {
            if (m_Value == null)
                m_Value = a;
            if (!m_Value.Equals(a))
                return false;
            return true;
        }
    }
}
