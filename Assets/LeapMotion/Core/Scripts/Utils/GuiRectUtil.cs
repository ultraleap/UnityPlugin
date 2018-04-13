/******************************************************************************
 * Copyright (C) Leap Motion, Inc. 2011-2018.                                 *
 * Leap Motion proprietary and confidential.                                  *
 *                                                                            *
 * Use subject to the terms of the Leap Motion SDK Agreement available at     *
 * https://developer.leapmotion.com/sdk_agreement, or another agreement       *
 * between Leap Motion and you, your company or other organization.           *
 ******************************************************************************/

using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Leap.Unity {

  public static class GuiRectUtil {

    public static Vector3 Corner00(this Rect rect) {
      return new Vector3(rect.x, rect.y);
    }

    public static Vector3 Corner10(this Rect rect) {
      return new Vector3(rect.x + rect.width, rect.y);
    }

    public static Vector3 Corner01(this Rect rect) {
      return new Vector3(rect.x, rect.y + rect.height);
    }

    public static Vector3 Corner11(this Rect rect) {
      return new Vector3(rect.x + rect.width, rect.y + rect.height);
    }

    public static Rect Encapsulate(this Rect rect, Vector2 point) {
      if (point.x < rect.x) {
        rect.width += rect.x - point.x;
        rect.x = point.x;
      } else if (point.x > rect.x + rect.width) {
        rect.width = point.x - rect.x;
      }

      if (point.y < rect.y) {
        rect.height += rect.y - point.y;
        rect.y = point.y;
      } else if (point.y > rect.y + rect.height) {
        rect.height = point.y - rect.y;
      }

      return rect;
    }

    public static void SplitHorizontally(this Rect rect, out Rect left, out Rect right) {
      left = rect;
      left.width /= 2;
      right = left;
      right.x += right.width;
    }

    public static void SplitHorizontallyWithRight(this Rect rect, out Rect left, out Rect right, float rightWidth) {
      left = rect;
      left.width -= rightWidth;
      right = left;
      right.x += right.width;
      right.width = rightWidth;
    }

    public static Rect NextLine(this Rect rect) {
      rect.y += rect.height;
      return rect;
    }

    public static Rect FromRight(this Rect rect, float width) {
      rect.x = rect.width - width;
      rect.width = width;
      return rect;
    }

#if UNITY_EDITOR
    public static Rect SingleLine(this Rect rect) {
      rect.height = EditorGUIUtility.singleLineHeight;
      return rect;
    }

    public static Rect Indent(this Rect rect) {
      rect.x += EditorGUIUtility.singleLineHeight;
      rect.width -= EditorGUIUtility.singleLineHeight;
      return rect;
    }
#endif
  }
}
