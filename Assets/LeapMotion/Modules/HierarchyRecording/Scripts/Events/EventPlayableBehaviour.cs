using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Playables;

namespace Leap.Unity.Recording {

  public class EventPlayableBehaviour : PlayableBehaviour {
    
    public GameObject recipient;
    public string message = "MyMethod";
    public object argument;

    public override void OnBehaviourPlay(Playable playable, FrameData info) {
      base.OnBehaviourPlay(playable, info);

      // Can't send messages at edit-time.
      if (!Application.isPlaying) return;

      var time = playable.GetTime();
      if (time < playable.GetDuration() / 2) {
        var target = recipient;

        if (target == null) {
          Debug.LogError("Unable to find event message recipient.");
          return;
        }

        if (argument == null) {
          target.SendMessage(message);
        }
        else {
          target.SendMessage(message, argument);
        }
      }

    }

  }

}
