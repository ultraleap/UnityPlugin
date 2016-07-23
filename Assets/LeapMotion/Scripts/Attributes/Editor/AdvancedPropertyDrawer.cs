using UnityEngine;
using UnityEditor;
using System.Linq;
using System.Collections.Generic;

namespace Leap.Unity.Attributes {

  [CustomPropertyDrawer(typeof(CombinablePropertyAttribute), true)]
  public class CombinablePropertyDrawer : PropertyDrawer {

    private IEnumerable<CombinablePropertyAttribute> attributes {
      get {
        foreach (object o in fieldInfo.GetCustomAttributes(typeof(CombinablePropertyAttribute), true)) {
          yield return o as CombinablePropertyAttribute;
        }
      }
    }

    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label) {
      float defaultLabelWidth = EditorGUIUtility.labelWidth;
      float fieldWidth = position.width - EditorGUIUtility.labelWidth;

      bool canUseDefaultDrawer = true;
      bool shouldDisable = false;

      RangeAttribute rangeAttribute = fieldInfo.GetCustomAttributes(typeof(RangeAttribute), true).FirstOrDefault() as RangeAttribute;

      IFullPropertyDrawer fullPropertyDrawer = null;
      foreach (var a in attributes) {
        if (a is IBeforeLabelAdditiveDrawer) {
          EditorGUIUtility.labelWidth -= (a as IBeforeLabelAdditiveDrawer).GetWidth();
        }

        if (a is IAfterLabelAdditiveDrawer) {
          EditorGUIUtility.labelWidth -= (a as IAfterLabelAdditiveDrawer).GetWidth();
          canUseDefaultDrawer = false;
        }

        if (a is IBeforeFieldAdditiveDrawer) {
          fieldWidth -= (a as IBeforeFieldAdditiveDrawer).GetWidth();
          canUseDefaultDrawer = false;
        }

        if (a is IAfterFieldAdditiveDrawer) {
          fieldWidth -= (a as IAfterFieldAdditiveDrawer).GetWidth();
        }

        if (a is IPropertyDisabler) {
          shouldDisable |= (a as IPropertyDisabler).ShouldDisable(property);
        }

        if (a is IFullPropertyDrawer) {
          if (fullPropertyDrawer != null) {
            Debug.LogError("Cannot have 2 advanced attributes that both override the field drawing");
            return;
          }
          fullPropertyDrawer = a as IFullPropertyDrawer;
        }
      }

      if (fullPropertyDrawer != null && !canUseDefaultDrawer) {
        Debug.LogError("Cannot have an advanced attribute drawer that draws a custom field, and also have an adavanced attribute drawer that draws between label and field!");
        return;
      }

      Rect r = position;
      EditorGUI.BeginDisabledGroup(shouldDisable);

      drawAdditive<IBeforeLabelAdditiveDrawer>(ref r, property);

      if (canUseDefaultDrawer) {
        r.width = EditorGUIUtility.labelWidth + fieldWidth;

        if (fullPropertyDrawer != null) {
          fullPropertyDrawer.DrawProperty(r, property, label);
        } else {
          if (rangeAttribute != null) {
            if (property.propertyType == SerializedPropertyType.Integer) {
              property.intValue = EditorGUI.IntSlider(r, label, property.intValue, (int)rangeAttribute.min, (int)rangeAttribute.max);
            } else if (property.propertyType == SerializedPropertyType.Float) {
              property.floatValue = EditorGUI.Slider(r, label, property.floatValue, rangeAttribute.min, rangeAttribute.max);
            } else {
              EditorGUI.PropertyField(r, property, label);
            }
          } else {
            EditorGUI.PropertyField(r, property, label);
          }
        }

        r.x += r.width;
      } else {
        r.width = EditorGUIUtility.labelWidth;
        r = EditorGUI.PrefixLabel(r, label);

        drawAdditive<IAfterLabelAdditiveDrawer>(ref r, property);
        drawAdditive<IBeforeFieldAdditiveDrawer>(ref r, property);

        r.width = fieldWidth;
        EditorGUI.PropertyField(r, property, GUIContent.none);
        r.x += r.width;
      }

      drawAdditive<IAfterFieldAdditiveDrawer>(ref r, property);

      EditorGUI.EndDisabledGroup();

      foreach (var a in attributes) {
        if (a is IPropertyConstrainer) {
          (a as IPropertyConstrainer).ConstrainValue(property);
        }
      }

      EditorGUIUtility.labelWidth = defaultLabelWidth;
    }

    private void drawAdditive<T>(ref Rect r, SerializedProperty property) where T : class, IAdditiveDrawer {
      foreach (var a in attributes) {
        if (a is T) {
          T t = a as T;
          r.width = t.GetWidth();
          t.Draw(r, property);
          r.x += r.width;
        }
      }
    }
  }
}
