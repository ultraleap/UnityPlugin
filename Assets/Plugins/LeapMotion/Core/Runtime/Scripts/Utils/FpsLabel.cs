/******************************************************************************
 * Copyright (C) Ultraleap, Inc. 2011-2020.                                   *
 *                                                                            *
 * Use subject to the terms of the Apache License 2.0 available at            *
 * http://www.apache.org/licenses/LICENSE-2.0, or another agreement           *
 * between Ultraleap and you, your company or other organization.             *
 ******************************************************************************/

using UnityEngine;

namespace Leap.Unity {
  public class FpsLabel : MonoBehaviour {

    [SerializeField]
    private LeapProvider _provider;

    [SerializeField]
    private TextMesh _frameRateText;

    private SmoothedFloat _smoothedRenderFps = new SmoothedFloat();

    private void OnEnable() {
      if (_provider == null) {
        _provider = Hands.Provider;
      }

      if (_frameRateText == null) {
        _frameRateText = GetComponentInChildren<TextMesh>();
        if (_frameRateText == null) {
          Debug.LogError("Could not enable FpsLabel because no TextMesh was specified!");
          enabled = false;
        }
      }

      _smoothedRenderFps.delay = 0.3f;
      _smoothedRenderFps.reset = true;
    }

    private void Update() {
      _frameRateText.text = "";

      if (_provider != null) {
        Frame frame = _provider.CurrentFrame;

        if (frame != null) {
          _frameRateText.text += "Data FPS:" + _provider.CurrentFrame.CurrentFramesPerSecond.ToString("f2");
          _frameRateText.text += System.Environment.NewLine;
        }
      }

      if (Time.smoothDeltaTime > Mathf.Epsilon) {
        _smoothedRenderFps.Update(1.0f / Time.smoothDeltaTime, Time.deltaTime);
      }

      _frameRateText.text += "Render FPS:" + Mathf.RoundToInt(_smoothedRenderFps.value).ToString("f2");
    }
  }
}
