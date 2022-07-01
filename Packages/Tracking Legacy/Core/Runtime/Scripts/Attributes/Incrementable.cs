/******************************************************************************
 * Copyright (C) Ultraleap, Inc. 2011-2021.                                   *
 *                                                                            *
 * Use subject to the terms of the Apache License 2.0 available at            *
 * http://www.apache.org/licenses/LICENSE-2.0, or another agreement           *
 * between Ultraleap and you, your company or other organization.             *
 ******************************************************************************/

using System.Collections.Generic;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Leap.Unity.Attributes
{
    [System.Obsolete("This code will be moved to a legacy package in the next major version of the plugin. If you believe that it needs to be kept in tracking, please open a discussion on the GitHub forum (https://github.com/ultraleap/UnityPlugin/discussions)")]
    public class Incrementable : CombinablePropertyAttribute, IAfterFieldAdditiveDrawer
    {
        public const float BUTTON_WIDTH = 20;

#if UNITY_EDITOR
        public void Draw(Rect rect, SerializedProperty property)
        {
            rect.width = BUTTON_WIDTH;

            if (GUI.Button(rect, "-"))
            {
                property.intValue--;
            }

            rect.x += rect.width;

            if (GUI.Button(rect, "+"))
            {
                property.intValue++;
            }
        }

        public float GetWidth()
        {
            return BUTTON_WIDTH * 2;
        }

        public override IEnumerable<SerializedPropertyType> SupportedTypes
        {
            get
            {
                yield return SerializedPropertyType.Integer;
            }
        }
#endif
    }
}