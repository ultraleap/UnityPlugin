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
  
  public class TransitionAfterPlayable : TransitionBehaviour {

    #pragma warning disable 0649
    [SerializeField]
    private PlayableDirector _director;
    #pragma warning restore 0649

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
