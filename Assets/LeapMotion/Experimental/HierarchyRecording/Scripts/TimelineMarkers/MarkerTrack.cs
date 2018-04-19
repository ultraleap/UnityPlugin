using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Timeline;

namespace Leap.Unity.Recording {

  [TrackColor(0.5f, 0.1f, 0.5f)]
  [TrackClipType(typeof(MarkerClip))]
  public class MarkerTrack : TrackAsset { }
}
