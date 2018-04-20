/******************************************************************************
 * Copyright (C) Leap Motion, Inc. 2011-2018.                                 *
 * Leap Motion proprietary and confidential.                                  *
 *                                                                            *
 * Use subject to the terms of the Leap Motion SDK Agreement available at     *
 * https://developer.leapmotion.com/sdk_agreement, or another agreement       *
 * between Leap Motion and you, your company or other organization.           *
 ******************************************************************************/

using UnityEngine;
using UnityEngine.Playables;

namespace Leap.Unity.Recording {
  
  public class TransitionAfterPlayable : TransitionBehaviour {

    [SerializeField]
    private PlayableDirector _director;

    private bool _hasStartedPlaying = false;

    private void OnEnable() {
      _hasStartedPlaying = false;
    }
    
    private void Update() {
      if (_hasStartedPlaying) {
        if (_director.state != PlayState.Playing) {
          Transition();
        }
      } else if (_director.state == PlayState.Playing) {
        _hasStartedPlaying = true;
      }

      Debug.Log(_director.state);
    }
  }
}
