/******************************************************************************
 * Copyright (C) Leap Motion, Inc. 2011-2017.                                 *
 * Leap Motion proprietary and  confidential.                                 *
 *                                                                            *
 * Use subject to the terms of the Leap Motion SDK Agreement available at     *
 * https://developer.leapmotion.com/sdk_agreement, or another agreement       *
 * between Leap Motion and you, your company or other organization.           *
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
