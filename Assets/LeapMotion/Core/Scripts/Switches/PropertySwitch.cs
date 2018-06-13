using Leap.Unity.Attributes;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace Leap.Unity.Animation {

  public class PropertySwitch : TweenSwitch {

    #region Inspector
    
    [Serializable] public class BoolEvent    : UnityEvent<bool>    { }
    [Serializable] public class FloatEvent   : UnityEvent<float>   { }
    [Serializable] public class IntEvent     : UnityEvent<int>     { }
    [Serializable] public class StringEvent  : UnityEvent<string>  { }
    [Serializable] public class Vector2Event : UnityEvent<Vector2> { }
    [Serializable] public class Vector3Event : UnityEvent<Vector3> { }

    [Header("Property")]
    public PropertyType propertyType;
    public enum PropertyType { Bool, Int, Float, String, Vector2, Vector3 }
    
    public bool onBoolValue = true;
    public bool offBoolValue = false;
    public BoolEvent updateBoolValue;
    
    public float onFloatValue = 1f;
    public float offFloatValue = 0f;
    public FloatEvent updateFloatValue;

    public int onIntValue = 1;
    public int offIntValue = 0;
    public IntEvent updateIntValue;
    
    public string onStringValue = "On";
    public string offStringValue = "Off";
    public StringEvent updateStringValue;

    public Vector2 onVector2Value = new Vector2(1f, 1f);
    public Vector2 offVector2Value = new Vector2(0f, 0f);
    public Vector2Event updateVector2Value;

    public Vector3 onVector3Value = new Vector3(1f, 1f, 1f);
    public Vector3 offVector3Value = new Vector3(0f, 0f, 0f);
    public Vector3Event updateVector3Value;

    [Header("Animation")]

    [UnitCurve]
    public AnimationCurve interpolationCurve = DefaultCurve.SigmoidUp;

    #endregion

    protected override void updateSwitch(float time, bool immediately = false) {
      if (IsInterpolatable(propertyType)) {
        if (propertyType == PropertyType.Float) {
          updateFloatValue.Invoke(Mathf.Lerp(offFloatValue, onFloatValue, interpolationCurve.Evaluate(time)));
        }
        else if (propertyType == PropertyType.Int) {
          updateIntValue.Invoke(Mathf.RoundToInt(Mathf.Lerp(offIntValue, onIntValue, interpolationCurve.Evaluate(time))));
        }
        else if (propertyType == PropertyType.Vector2) {
          updateVector2Value.Invoke(Vector2.Lerp(offVector2Value, onVector2Value, interpolationCurve.Evaluate(time)));
        }
        else if (propertyType == PropertyType.Vector3) {
          updateVector3Value.Invoke(Vector3.Lerp(offVector3Value, onVector3Value, interpolationCurve.Evaluate(time)));
        }
      }
      else {
        if (propertyType == PropertyType.Bool) {
          if (GetIsOnOrTurningOn()) {
            updateBoolValue.Invoke(onBoolValue);
          }
          else {
            updateBoolValue.Invoke(offBoolValue);
          }
        }
        else if (propertyType == PropertyType.String) {
          if (GetIsOnOrTurningOn()) {
            updateStringValue.Invoke(onStringValue);
          }
          else {
            updateStringValue.Invoke(offStringValue);
          }
        }
      }
    }

    public static bool IsInterpolatable(PropertyType propertyType) {
      return propertyType == PropertyType.Float
          || propertyType == PropertyType.Int
          || propertyType == PropertyType.Vector2
          || propertyType == PropertyType.Vector3;
    }

  }

}