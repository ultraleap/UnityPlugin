/******************************************************************************
 * Copyright (C) Ultraleap, Inc. 2011-2024.                                   *
 *                                                                            *
 * Use subject to the terms of the Apache License 2.0 available at            *
 * http://www.apache.org/licenses/LICENSE-2.0, or another agreement           *
 * between Ultraleap and you, your company or other organization.             *
 ******************************************************************************/

using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Leap.Attributes
{

    public class UnitsAttribute : CombinablePropertyAttribute, IAfterFieldAdditiveDrawer
    {
        public readonly string unitsName;

        public UnitsAttribute(string _unitsName)
        {
            this.unitsName = _unitsName;
        }

#if UNITY_EDITOR
        public float GetWidth()
        {
            return EditorStyles.label.CalcSize(new GUIContent(unitsName)).x + 5; // Give it a small 5 unit buffer
        }

        public void Draw(Rect _rect, SerializedProperty _property)
        {
            GUI.Label(_rect, unitsName);
        }
#endif
    }
}