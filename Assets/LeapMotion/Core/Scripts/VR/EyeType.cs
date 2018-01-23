/******************************************************************************
 * Copyright (C) Leap Motion, Inc. 2011-2017.                                 *
 * Leap Motion proprietary and  confidential.                                 *
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

namespace Leap.Unity {
  [Serializable]
  public class EyeType {
    private const string TARGET_EYE_PROPERTY_NAME = "m_TargetEye";
    private const int TARGET_EYE_LEFT_INDEX = 1;
    private const int TARGET_EYE_RIGHT_INDEX = 2;
    private const int TARGET_EYE_CENTER_INDEX = 3;

    public enum OrderType {
      LEFT = TARGET_EYE_LEFT_INDEX,
      RIGHT = TARGET_EYE_RIGHT_INDEX,
      CENTER = TARGET_EYE_CENTER_INDEX
    }

    [SerializeField]
    private OrderType _orderType = OrderType.LEFT;

    private bool _isOnFirst = false;
    private bool _hasBegun = false;

    public OrderType Type {
      get {
        return _orderType;
      }
    }

    public bool IsLeftEye {
      get {
        if (!_hasBegun) {
          throw new Exception("Cannot call IsLeftEye or IsRightEye before BeginCamera has been called!");
        }

        switch (_orderType) {
          case OrderType.LEFT: return true;
          case OrderType.RIGHT: return false;
          case OrderType.CENTER: return _isOnFirst;
          default: throw new Exception("Unexpected order type " + _orderType);
        }
      }
    }

    public bool IsRightEye {
      get {
        return !IsLeftEye;
      }
    }

    public EyeType(OrderType type) {
      _orderType = type;
    }

#if UNITY_EDITOR
    public void UpdateOrderGivenComponent(Component component) {
      if (Application.isPlaying) {
        return;
      }

      //Allow the user to specify themselves if VR is disabled.
      if (!XRSupportUtil.IsXREnabled()) {
        return;
      }

      Camera camera = component.GetComponent<Camera>();
      if (camera == null) {
        camera = component.gameObject.AddComponent<Camera>();
      }

      SerializedObject obj = new SerializedObject(camera);
      SerializedProperty targetEyeProp = obj.FindProperty(TARGET_EYE_PROPERTY_NAME);
      OrderType newOrder = (OrderType)targetEyeProp.intValue;
      if (_orderType != newOrder) {
        _orderType = newOrder;
        EditorUtility.SetDirty(component);
      }
    }
#endif

      public void BeginCamera() {
      if (!_hasBegun) {
        _isOnFirst = true;
        _hasBegun = true;
      } else {
        _isOnFirst = !_isOnFirst;
      }
    }

    public void Reset() {
      _hasBegun = false;
    }
  }
}
