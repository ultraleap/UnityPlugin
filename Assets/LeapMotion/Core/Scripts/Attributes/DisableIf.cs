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
using System;

namespace Leap.Unity.Attributes {

  /// <summary>
  /// Conditionally disables a property based on the value of another property.  The only condition
  /// types that are currently supported are bool types, and enum types.  The property has two arguments
  /// names 'equalTo' and 'notEqualTo'.  Exactly one of them must be specified, like so:
  /// 
  /// [DisableIf("myBoolProperty", isEqualTo: true)]
  /// [DisableIf("myEnumProperty", isNotEqualTo: MyEnum.Value)]
  /// [DisableIfAny("bool1", "bool2", isEqualTo: false)]
  /// [DisableIfAll("cond1", "cond2", "cond3", isNotEqualTo: true)]
  /// </summary>
  public abstract class DisableIfBase : CombinablePropertyAttribute, IPropertyDisabler {
    public readonly string[] propertyNames;
    public readonly object testValue;
    public readonly bool disableResult;
    public readonly bool isAndOperation;

    public DisableIfBase(object isEqualTo, object isNotEqualTo, bool isAndOperation, params string[] propertyNames) {
      this.propertyNames = propertyNames;
      this.isAndOperation = isAndOperation;

      if ((isEqualTo != null) == (isNotEqualTo != null)) {
        throw new ArgumentException("Must specify exactly one of 'equalTo' or 'notEqualTo'.");
      }

      if (isEqualTo != null) {
        testValue = isEqualTo;
        disableResult = true;
      } else if (isNotEqualTo != null) {
        testValue = isNotEqualTo;
        disableResult = false;
      }

      if (!(testValue is bool) && !(testValue is Enum)) {
        throw new ArgumentException("Only values of bool or Enum are allowed in comparisons using DisableIf.");
      }
    }

#if UNITY_EDITOR
    public bool ShouldDisable(SerializedProperty property) {
      foreach (var name in propertyNames) {
        var prop = property.serializedObject.FindProperty(name);

        bool result = shouldDisable(prop);
        if (isAndOperation) {
          if (!result) {
            return false;
          }
        } else {
          if (result) {
            return true;
          }
        }
      }

      if (isAndOperation) {
        return true;
      } else {
        return false;
      }
    }

    private bool shouldDisable(SerializedProperty property) {
      if (property.propertyType == SerializedPropertyType.Boolean) {
        return (property.boolValue == (bool)testValue) == disableResult;
      } else if (property.propertyType == SerializedPropertyType.Enum) {
        return (property.intValue == (int)testValue) == disableResult;
      } else {
        Debug.LogError("Can only conditionally disable based on boolean or enum types.");
        return false;
      }
    }
#endif
  }

  public class DisableIf : DisableIfBase {
    public DisableIf(string propertyName, object isEqualTo = null, object isNotEqualTo = null) :
      base(isEqualTo, isNotEqualTo, true, propertyName) { }
  }

  public class DisableIfAny : DisableIfBase {

    public DisableIfAny(string propertyName1, string propertyName2, object areEqualTo = null, object areNotEqualTo = null) :
      base(areEqualTo, areNotEqualTo, false, propertyName1, propertyName2) { }

    public DisableIfAny(string propertyName1, string propertyName2, string propertyName3, object areEqualTo = null, object areNotEqualTo = null) :
      base(areEqualTo, areNotEqualTo, false, propertyName1, propertyName2, propertyName3) { }

    public DisableIfAny(string propertyName1, string propertyName2, string propertyName3, string propertyName4, object areEqualTo = null, object areNotEqualTo = null) :
      base(areEqualTo, areNotEqualTo, false, propertyName1, propertyName2, propertyName3, propertyName4) { }
  }

  public class DisableIfAll : DisableIfBase {

    public DisableIfAll(string propertyName1, string propertyName2, object areEqualTo = null, object areNotEqualTo = null) :
      base(areEqualTo, areNotEqualTo, true, propertyName1, propertyName2) { }

    public DisableIfAll(string propertyName1, string propertyName2, string propertyName3, object areEqualTo = null, object areNotEqualTo = null) :
      base(areEqualTo, areNotEqualTo, true, propertyName1, propertyName2, propertyName3) { }

    public DisableIfAll(string propertyName1, string propertyName2, string propertyName3, string propertyName4, object areEqualTo = null, object areNotEqualTo = null) :
      base(areEqualTo, areNotEqualTo, true, propertyName1, propertyName2, propertyName3, propertyName4) { }
  }
}
