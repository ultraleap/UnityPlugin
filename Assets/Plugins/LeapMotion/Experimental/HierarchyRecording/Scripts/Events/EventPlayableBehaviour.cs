/******************************************************************************
 * Copyright (C) Ultraleap, Inc. 2011-2020.                                   *
 *                                                                            *
 * Use subject to the terms of the Apache License 2.0 available at            *
 * http://www.apache.org/licenses/LICENSE-2.0, or another agreement           *
 * between Ultraleap and you, your company or other organization.             *
 ******************************************************************************/

using UnityEngine;
using UnityEngine.Playables;

namespace Leap.Unity.Recording {

  public class EventPlayableBehaviour : PlayableBehaviour {
    
    public GameObject recipient;
    public string message = "MyMethod";
    public object argument;

    public void FireEvent() {
      if (recipient == null) {
        Debug.LogError("Unable to fire event: Recipient is null.");
        return;
      }
      
      if (argument == null) {
        //Debug.Log("Target: " + recipient.name + "; Sending message: " + message);

        if (Application.isPlaying) {
          recipient.SendMessage(message);
        }
      }
      else {
        //Debug.Log("Target: " + recipient.name + "; Sending message: " + message
        //        + "with arg: " + argument);

        if (Application.isPlaying) {
          recipient.SendMessage(message, argument);
        }
      }
    }

  }

}
