/******************************************************************************
 * Copyright (C) Leap Motion, Inc. 2011-2018.                                 *
 * Leap Motion proprietary and confidential.                                  *
 *                                                                            *
 * Use subject to the terms of the Leap Motion SDK Agreement available at     *
 * https://developer.leapmotion.com/sdk_agreement, or another agreement       *
 * between Leap Motion and you, your company or other organization.           *
 ******************************************************************************/

using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Leap.Unity.Query;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Leap.Unity.Attributes {

  public class QuickButtonAttribute : CombinablePropertyAttribute, IBeforeFieldAdditiveDrawer {

    public const float PADDING_RIGHT = 12f;

    public string label = "Quick Button";
    public string methodOnPress = null;
    public string tooltip = "";

    public QuickButtonAttribute(string buttonLabel, string methodOnPress, string tooltip = "") {
      this.label = buttonLabel;
      this.methodOnPress = methodOnPress;
      this.tooltip = tooltip;
    }

    /// <summary>
    /// IBeforeFieldAdditiveDrawer uses this to determine the width of the rect to pass
    /// to the Draw method.
    /// </summary>
    public float GetWidth() {
      return GUI.skin.label.CalcSize(new GUIContent(label)).x + 12f + PADDING_RIGHT;
    }

#if UNITY_EDITOR
    public void Draw(Rect rect, SerializedProperty property) {
      
      var type = targets.Query().FirstOrDefault().GetType();
      System.Reflection.MethodInfo method;
      try {
        method = type.GetMethod(methodOnPress, System.Reflection.BindingFlags.Instance
                                             | System.Reflection.BindingFlags.Public
                                             | System.Reflection.BindingFlags.NonPublic);
      }
      catch (System.Reflection.AmbiguousMatchException e) {
        Debug.LogError("QuickButton tried to prepare " + methodOnPress + " for calling, "
                     + "but received an AmbiguousMatchException:\n" + e.ToString());
        return;
      }

      if (method == null) {
        Debug.LogError("QuickButton tried to prepare " + methodOnPress + " for calling, "
                     + "but the type " + type.Name + " has no such method.");
        return;
      }

      if (method.GetParameters().Query().Any(p => !p.IsOptional)) {
        Debug.LogError("QuickButton can't call " + method.Name + " because it requires "
                     + "non-optional parameters.");
        return;
      }

      if (GUI.Button(rect.PadInner(0, 0, 0, PADDING_RIGHT), new GUIContent(label, tooltip))) {
        foreach (var target in targets) {
          Undo.RegisterFullObjectHierarchyUndo(target, "Perform QuickButton Action");
        }
        foreach (var target in targets) {
          try {
            method.Invoke(target, new object[] { });
          }
          catch (Exception e) {
            Debug.LogError(e.GetType().Name + " thrown trying to call method "
              + method.Name + " on target " + target.name + ":\n"
              + e.ToString());
          }
        }
      }

    }
#endif

  }

}
