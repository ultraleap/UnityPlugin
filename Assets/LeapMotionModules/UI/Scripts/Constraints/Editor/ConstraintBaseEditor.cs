using Leap.Unity;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using System.Reflection;
using System;

namespace Leap.UI.Constraints {

  [CustomEditor(typeof(ConstraintBase), true)]
  public class ConstraintBaseEditor : CustomEditorBase {

    protected override void OnEnable() {
      base.OnEnable();

      var debugConstraintProperty = serializedObject.FindProperty("debugConstraint");
      Func<bool> getDebugConstraintPropertyChecked = () => debugConstraintProperty.boolValue;

      var iterator = serializedObject.GetIterator();
      while (iterator.NextVisible(true)) {
        string propertyName = iterator.name;
        FieldInfo info = target.GetType().GetField(propertyName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.FlattenHierarchy);
        if (info == null) continue;
        bool hasConstrainDebuggingAttribute = info.GetCustomAttributes(typeof(ConstraintDebuggingAttribute), true).Length != 0;
        if (hasConstrainDebuggingAttribute) {
          specifyConditionalDrawing(getDebugConstraintPropertyChecked, propertyName);
        }
      }
    }

  }



}