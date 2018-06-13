using Leap.Unity.Query;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Leap.Unity.Animation {

  [CustomEditor(typeof(PropertySwitch), editorForChildClasses: true)]
  public class PropertySwitchEditor : TweenSwitchEditor {

    public new PropertySwitch[] targets {
      get {
        return base.targets.Query().Cast<PropertySwitch>().ToArray();
      }
    }

    protected override void OnEnable() {
      base.OnEnable();

      specifyCustomDecorator("propertyType", decorateWhenNonInterpolatable);

      specifyConditionalDrawing(shouldDrawBool, "onBoolValue", "offBoolValue", "updateBoolValue");
      specifyConditionalDrawing(shouldDrawFloat, "onFloatValue", "offFloatValue", "updateFloatValue");
      specifyConditionalDrawing(shouldDrawInt, "onIntValue", "offIntValue", "updateIntValue");
      specifyConditionalDrawing(shouldDrawString, "onStringValue", "offStringValue", "updateStringValue");
      specifyConditionalDrawing(shouldDrawVector2, "onVector2Value", "offVector2Value", "updateVector2Value");
      specifyConditionalDrawing(shouldDrawVector3, "onVector3Value", "offVector3Value", "updateVector3Value");

      specifyConditionalDrawing(shouldDrawInterpolationCurve, "interpolationCurve");

      //deferProperty("_eventTable");
      //specifyCustomDrawer("_eventTable", drawEventTable);
    }

    private void decorateWhenNonInterpolatable(SerializedProperty property) {
      bool anyTargetingNonInterpolatableValue = targets.Query().Any(t => !PropertySwitch.IsInterpolatable(t.propertyType));
      if (anyTargetingNonInterpolatableValue) {
        EditorGUILayout.HelpBox("Non-interpolatable property types such as Bool or String "
                              + "will ignore the Tween Time property.", MessageType.Info);
      }
    }

    private bool shouldDrawBool() {
      return targets.Query().Any(t => t.propertyType == PropertySwitch.PropertyType.Bool);
    }
    private bool shouldDrawFloat() {
      return targets.Query().Any(t => t.propertyType == PropertySwitch.PropertyType.Float);
    }
    private bool shouldDrawInt() {
      return targets.Query().Any(t => t.propertyType == PropertySwitch.PropertyType.Int);
    }
    private bool shouldDrawString() {
      return targets.Query().Any(t => t.propertyType == PropertySwitch.PropertyType.String);
    }
    private bool shouldDrawVector2() {
      return targets.Query().Any(t => t.propertyType == PropertySwitch.PropertyType.Vector2);
    }
    private bool shouldDrawVector3() {
      return targets.Query().Any(t => t.propertyType == PropertySwitch.PropertyType.Vector3);
    }

    private bool shouldDrawInterpolationCurve() {
      return targets.Query().Any(t => PropertySwitch.IsInterpolatable(t.propertyType));
    }

    // TODO: Delete
    //#region Event Table

    //private EnumEventTableEditor _tableEditor;

    //private void drawEventTable(SerializedProperty property) {
    //  if (_tableEditor == null) {
    //    _tableEditor = new EnumEventTableEditor(property, typeof(PropertySwitch.PropertyType));
    //  }

    //  _tableEditor.DoGuiLayout();
    //}

    //#endregion

  }

}
