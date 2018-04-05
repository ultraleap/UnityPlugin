/******************************************************************************
 * Copyright (C) Leap Motion, Inc. 2011-2018.                                 *
 * Leap Motion proprietary and confidential.                                  *
 *                                                                            *
 * Use subject to the terms of the Leap Motion SDK Agreement available at     *
 * https://developer.leapmotion.com/sdk_agreement, or another agreement       *
 * between Leap Motion and you, your company or other organization.           *
 ******************************************************************************/

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Leap.Unity {

  public class LeapTestProvider : LeapProvider {

    public Frame frame;
    public override Frame CurrentFrame { get { return frame; } }
    public override Frame CurrentFixedFrame { get { return frame; } }

    [Header("Runtime Basis Transforms")]

    [Tooltip("At runtime, if this Transform is non-null, the LeapTestProvider will "
           + "create a test-pose left hand at this transform's position and rotation."
           + "Setting this binding to null at runtime will cause the hand to disappear "
           + "from Frame data, as if it stopped tracking.")]
    public Transform leftHandBasis;
    private Hand _leftHand = null;
    private Hand _cachedLeftHand = null;

    [Tooltip("At runtime, if this Transform is non-null, the LeapTestProvider will "
           + "create a test-pose right hand at this transform's position and rotation."
           + "Setting this binding to null at runtime will cause the hand to disappear "
           + "from Frame data, as if it stopped tracking.")]
    public Transform rightHandBasis;
    private Hand _rightHand = null;
    private Hand _cachedRightHand = null;

    void Awake() {
      _cachedLeftHand  = TestHandFactory.MakeTestHand(isLeft: true,
                                           unitType: TestHandFactory.UnitType.UnityUnits);
      _cachedRightHand = TestHandFactory.MakeTestHand(isLeft: false,
                                           unitType: TestHandFactory.UnitType.UnityUnits);
    }

    void Update() {
      if (_leftHand == null && leftHandBasis != null) {
        _leftHand = _cachedLeftHand;
        frame.Hands.Add(_leftHand);
      }
      if (_leftHand != null && leftHandBasis == null) {
        frame.Hands.Remove(_leftHand);
        _leftHand = null;
      }
      if (_leftHand != null) {
        _leftHand.SetTransform(leftHandBasis.position, leftHandBasis.rotation);
      }

      if (_rightHand == null && rightHandBasis != null) {
        _rightHand = _cachedRightHand;
        frame.Hands.Add(_rightHand);
      }
      if (_rightHand != null && rightHandBasis == null) {
        frame.Hands.Remove(_rightHand);
        _rightHand = null;
      }
      if (_rightHand != null) {
        _rightHand.SetTransform(rightHandBasis.position, rightHandBasis.rotation);
      }

      DispatchUpdateFrameEvent(frame);
    }

    void FixedUpdate() {
      DispatchFixedFrameEvent(frame);
    }

  }

}
