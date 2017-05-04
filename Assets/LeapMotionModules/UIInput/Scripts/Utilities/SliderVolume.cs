/******************************************************************************
 * Copyright (C) Leap Motion, Inc. 2011-2017.                                 *
 * Leap Motion proprietary and  confidential.                                 *
 *                                                                            *
 * Use subject to the terms of the Leap Motion SDK Agreement available at     *
 * https://developer.leapmotion.com/sdk_agreement, or another agreement       *
 * between Leap Motion and you, your company or other organization.           *
 ******************************************************************************/

using UnityEngine;
using UnityEngine.UI;
using System.Collections;

namespace Leap.Unity.InputModule {
  public class SliderVolume : MonoBehaviour {
    public AudioSource source;
    float volume = 0f;
    float currentValue = -1f;
    float previousValue = -1f;
    float maxValue = 0f;
    float TimeLastSlid = 0f;

    void Start() {
      maxValue = GetComponent<Slider>().maxValue;
    }

    void Update() {
      volume = Mathf.Lerp(volume, Mathf.Abs(currentValue - previousValue) * 40f, 0.4f);
      previousValue = currentValue;
      source.volume = volume;

      if (Time.time - TimeLastSlid > 0.5f) {
        source.Stop();
      } else if (!source.isPlaying) {
        source.Play();
      }
    }

    public void setSliderSoundVolume(float sliderposition) {
      currentValue = sliderposition / maxValue;
      TimeLastSlid = Time.time;
    }
  }
}
