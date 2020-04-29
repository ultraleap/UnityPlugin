/******************************************************************************
 * Copyright (C) Ultraleap, Inc. 2011-2020.                                   *
 * Ultraleap proprietary and confidential.                                    *
 *                                                                            *
 * Use subject to the terms of the Leap Motion SDK Agreement available at     *
 * https://developer.leapmotion.com/sdk_agreement, or another agreement       *
 * between Ultraleap and you, your company or other organization.             *
 ******************************************************************************/

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Playables;

namespace Leap.Unity.Recording {
  using Animation;

  public class SetPlayableDirector : MonoBehaviour {

    #pragma warning disable 0649
    [SerializeField]
    private PlayableDirector _director;

    [SerializeField]
    private PlayableAsset _playable;

    [SerializeField]
    private DirectorWrapMode _wrapMode = DirectorWrapMode.None;
    #pragma warning restore 0649

    private void OnEnable() {
      _director.time = 0;
      _director.extrapolationMode = _wrapMode;
      _director.Play(_playable);
    }

    public void PauseAndHold() {
      _director.playableGraph.GetRootPlayable(0).SetSpeed(0);
    }

    public void ResumeFromPauseAndHold() {
      _director.playableGraph.GetRootPlayable(0).SetSpeed(1);
    }
  }
}
