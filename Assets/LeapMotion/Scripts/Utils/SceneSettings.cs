/******************************************************************************
 * Copyright (C) Leap Motion, Inc. 2011-2017.                                 *
 * Leap Motion proprietary and  confidential.                                 *
 *                                                                            *
 * Use subject to the terms of the Leap Motion SDK Agreement available at     *
 * https://developer.leapmotion.com/sdk_agreement, or another agreement       *
 * between Leap Motion and you, your company or other organization.           *
 ******************************************************************************/

using UnityEngine;
using System;

namespace Leap.Unity {

  public class SceneSettings : MonoBehaviour {

    public class ToggleValue<T> {
      public bool Override;
      public T Value;
    }

    [Serializable]
    public class ToggleFloat : ToggleValue<float> { }

    [Serializable]
    public class ToggleVector3 : ToggleValue<Vector3> { }

    [SerializeField]
    private ToggleFloat _shadowDistance = new ToggleFloat();

    [SerializeField]
    private ToggleVector3 _gravity = new ToggleVector3();

    [SerializeField]
    private ToggleFloat _sleepThreshold = new ToggleFloat();

    void Reset() {
      _shadowDistance.Value = QualitySettings.shadowDistance;
      _gravity.Value = Physics.gravity;
      _sleepThreshold.Value = Physics.sleepThreshold;
    }

    void Awake() {
      if (_shadowDistance.Override) {
        QualitySettings.shadowDistance = _shadowDistance.Value;
      }

      if (_gravity.Override) {
        Physics.gravity = _gravity.Value;
      }

      if (_sleepThreshold.Override) {
        Physics.sleepThreshold = _sleepThreshold.Value;
      }
    }
  }
}
