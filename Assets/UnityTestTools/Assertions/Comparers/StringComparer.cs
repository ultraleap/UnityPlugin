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
    public class StringComparer : ComparerBaseGeneric<string>
    {
        public enum CompareType
        {
            Equal,
            NotEqual,
            Shorter,
            Longer
        }

        public CompareType compareType;
        public StringComparison comparisonType = StringComparison.Ordinal;
        public bool ignoreCase = false;

        protected override bool Compare(string a, string b)
        {
            if (ignoreCase)
            {
                a = a.ToLower();
                b = b.ToLower();
            }
            switch (compareType)
            {
                case CompareType.Equal:
                    return String.Compare(a, b, comparisonType) == 0;
                case CompareType.NotEqual:
                    return String.Compare(a, b, comparisonType) != 0;
                case CompareType.Longer:
                    return String.Compare(a, b, comparisonType) > 0;
                case CompareType.Shorter:
                    return String.Compare(a, b, comparisonType) < 0;
            }
            throw new Exception();
        }
    }
}
