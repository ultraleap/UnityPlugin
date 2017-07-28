using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Playables;

namespace Leap.Unity.Recording {

  public class EventPlayable : PlayableBehaviour {

    public GameObject rootObject = null;
    public string recipientPath = "path/to/recipient";
    public string message = "MyMethod";
    public object argument;

    public override void OnBehaviourPlay(Playable playable, FrameData info) {
      base.OnBehaviourPlay(playable, info);

      // Can't send messages at edit-time.
      if (!Application.isPlaying) return;

      var time = playable.GetTime();
      if (time < playable.GetDuration() / 2) {
        var target = rootObject.transform.Find(recipientPath);

        if (target == null) {
          var totalPath = rootObject.name + "/" + recipientPath;
          Debug.LogError("Unable to find target at " + totalPath);
          return;
        }

        Debug.Log("FIRE -- " + time + " target is " + target);

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
