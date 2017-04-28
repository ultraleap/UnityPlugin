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
    public class GroupByNothingRenderer : AssertionListRenderer<string>
    {
        protected override IEnumerable<IGrouping<string, AssertionComponent>> GroupResult(IEnumerable<AssertionComponent> assertionComponents)
        {
            return assertionComponents.GroupBy(c => "");
        }

        protected override string GetStringKey(string key)
        {
            return "";
        }
    }
}
