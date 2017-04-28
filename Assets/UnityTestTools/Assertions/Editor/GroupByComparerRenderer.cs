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
using System.Linq;
using UnityEngine;

namespace UnityTest
{
    public class GroupByComparerRenderer : AssertionListRenderer<Type>
    {
        protected override IEnumerable<IGrouping<Type, AssertionComponent>> GroupResult(IEnumerable<AssertionComponent> assertionComponents)
        {
            return assertionComponents.GroupBy(c => c.Action.GetType());
        }

        protected override string GetStringKey(Type key)
        {
            return key.Name;
        }
    }
}
