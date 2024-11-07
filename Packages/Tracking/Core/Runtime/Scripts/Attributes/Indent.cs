/******************************************************************************
 * Copyright (C) Ultraleap, Inc. 2011-2024.                                   *
 *                                                                            *
 * Use subject to the terms of the Apache License 2.0 available at            *
 * http://www.apache.org/licenses/LICENSE-2.0, or another agreement           *
 * between Ultraleap and you, your company or other organization.             *
 ******************************************************************************/

#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
#endif

namespace Leap.Attributes
{
    public class IndentAttribute : CombinablePropertyAttribute, IBeforeLabelAdditiveDrawer
    {
        float width = 20;


#if UNITY_EDITOR
        public IndentAttribute()
        {
            width = 20;
        }

        public IndentAttribute(float _width)
        {
            width = _width;
        }

        public void Draw(Rect _rect, SerializedProperty _property)
        {

        }

        public float GetWidth()
        {
            return width;
        }

#endif
    }
}