using System.Collections.Generic;
using UnityEngine;

public class RecordedAudio : MonoBehaviour {

  public AudioSource target;
  public List<ClipData> data = new List<ClipData>();

  private bool _prevWasPlaying = false;
  private float _prevTime = 0;
  private AudioClip _prevClip = null;

  private void LateUpdate() {
    bool didStartNewClip = false;

    if (target.isPlaying && !_prevWasPlaying) {
      didStartNewClip = true;
    }

    if (target.time < _prevTime) {
      didStartNewClip = true;
    }

    if (target.clip != _prevClip) {
      didStartNewClip = true;
    }

    if (didStartNewClip) {
      data.Add(new ClipData() {
        clip = target.clip,
        startTime = Time.time,
        pitch = target.pitch,
        volume = target.volume
      });
    }

    _prevWasPlaying = target.isPlaying;
    _prevTime = target.time;
    _prevClip = target.clip;
  }

  public class ClipData {
    public AudioClip clip;
    public float startTime;
    public float pitch;
    public float volume;
  }

}
