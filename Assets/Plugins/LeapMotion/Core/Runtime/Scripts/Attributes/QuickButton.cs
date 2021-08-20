/******************************************************************************
 * Copyright (C) Ultraleap, Inc. 2011-2020.                                   *
 *                                                                            *
 * Use subject to the terms of the Apache License 2.0 available at            *
 * http://www.apache.org/licenses/LICENSE-2.0, or another agreement           *
 * between Ultraleap and you, your company or other organization.             *
 ******************************************************************************/

using System.Linq;
using System.Reflection;
using UnityEngine;
using Leap.Unity.Query;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Leap.Unity.Attributes {

  public class QuickButtonAttribute : CombinablePropertyAttribute, 
    IAfterFieldAdditiveDrawer
  {

    public const float PADDING_RIGHT = 12f;

    public readonly string label = "Quick Button";
    public readonly string methodOnPress = null;
    public readonly string tooltip = "";

    public QuickButtonAttribute(string buttonLabel, string methodOnPress,
      string tooltip = "")
    {
      this.label = buttonLabel;
      this.methodOnPress = methodOnPress;
      this.tooltip = tooltip;
    }

#if UNITY_EDITOR
    /// <summary>
    /// IBeforeFieldAdditiveDrawer uses this to determine the width of the rect to pass
    /// to the Draw method.
    /// </summary>
    public float GetWidth() {
      return GUI.skin.label.CalcSize(new GUIContent(label)).x + PADDING_RIGHT;
    }

    public void Draw(Rect rect, SerializedProperty property) {

      var type = targets.Query().FirstOrDefault().GetType();
      var namedMethods = type.GetMethods(BindingFlags.Instance |
                                         BindingFlags.Public |
                                         BindingFlags.NonPublic).
                              Where(m => m.Name == methodOnPress);

      MethodInfo method = null;
      string errorMessage = null;
      string buttonTooltip = tooltip;

      if (namedMethods.Count() == 0) {
        errorMessage = "QuickButton tried to prepare " + methodOnPress + " for calling, " +
                       "but the type " + type.Name + " has no such method.";
      } else {
        var validMethods = namedMethods.Where(m => m.GetParameters().
                                                     All(p => p.IsOptional));

        if (validMethods.Count() == 0) {
          errorMessage = "QuickButton tried to prepare " + methodOnPress + " for calling, " +
                         "but the type " + type.Name + " had no valid methods.";
        } else if (validMethods.Count() > 1) {
          errorMessage = "QuickButton tried to prepare " + methodOnPress + " for calling, " +
                         "but the type " + type.Name + " had more than one valid method.";
        } else {
          method = validMethods.Single();
        }
      }

      Color prevColor = GUI.color;
      if (method == null) {
        Debug.LogError(errorMessage);
        buttonTooltip = errorMessage;
        GUI.color = Color.red;
      }

      using (new EditorGUI.DisabledScope(method == null)) {
        if (GUI.Button(rect.PadInner(0, 0, 0, 0), new GUIContent(label, buttonTooltip))) {
          foreach (var target in targets) {
            if (target is MonoBehaviour) {
              Undo.RegisterFullObjectHierarchyUndo((target as MonoBehaviour).gameObject,
                "Perform QuickButton Action");
            } else {
              Undo.RegisterFullObjectHierarchyUndo(target, "Perform QuickButton Action");
            }
          }
          foreach (var target in targets) {
            try {
              method.Invoke(target,
                            method.GetParameters().Select(p => p.DefaultValue).ToArray());
            } catch (TargetInvocationException e) {
              Debug.LogError("Quick button received an error while trying to invoke " + methodOnPress + " on " + type.Name);
              Debug.LogException(e.InnerException);
            }
          }
        }
      }

      GUI.color = prevColor;
    }
#endif
  }
}
