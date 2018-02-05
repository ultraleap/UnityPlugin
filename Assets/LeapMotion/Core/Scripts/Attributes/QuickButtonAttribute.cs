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

    public Type type;
    public System.Reflection.MethodInfo method;

    public QuickButtonAttribute(string buttonLabel, string methodOnPress, string tooltip = "") {
      this.label = buttonLabel;
      this.methodOnPress = methodOnPress;
      this.tooltip = tooltip;
    }

    public float GetWidth() {
      return GUI.skin.label.CalcSize(new GUIContent(label)).x + 12f + PADDING_RIGHT;
    }

#if UNITY_EDITOR
    public void Draw(Rect rect, SerializedProperty property) {
      
      type = targets.Query().FirstOrDefault().GetType();
      method = type.GetMethod(methodOnPress, System.Reflection.BindingFlags.Instance
                                             | System.Reflection.BindingFlags.Public
                                             | System.Reflection.BindingFlags.NonPublic);

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
          method.Invoke(target, new object[] { });
        }
      }

    }
#endif

  }

  public static class RectExtensions {

    /// <summary>
    /// Returns a new Rect centered on the original Rect but with the specified amount of
    /// inner edge padding for each edge.
    /// </summary>
    public static Rect PadInner(this Rect r, float padding) {
      return new Rect(r.x + padding, r.y + padding, r.width - padding, r.height - padding);
    }

    /// <summary>
    /// Returns a new Rect centered on the original Rect but with the specified amount of
    /// inner edge padding for each edge.
    /// </summary>
    public static Rect PadInner(this Rect r, float top, float bottom, float left, float right) {
      return new Rect(r.x + left, r.y + bottom, r.width - right, r.height - top);
    }

  }

}