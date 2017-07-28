using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Timeline;

namespace Leap.Unity.Recording {

  [TrackColor(0.8F, 0.1F, 0.8F)]
  [TrackClipType(typeof(EventPlayableAsset))]
  public class EventTrack : TrackAsset { }

}