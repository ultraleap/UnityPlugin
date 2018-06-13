using Leap.Unity.Attributes;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Leap.Unity.Streams {

  [ExecuteInEditMode]
  public class TransformPoseStream : MonoBehaviour, IStream<Pose> {

    [Tooltip("Enable and disable the script to open and close the pose stream.")]
    [OnEditorChange("checkEditTimeStreamState")]
    public bool streamDuringEditMode = false;

    public event Action OnOpen = () => { };
    public event Action<Pose> OnSend = (pose) => { };
    public event Action OnClose = () => { };

    private bool _isStreamOpen = false;

    void OnEnable() {
      if ((Application.isPlaying || streamDuringEditMode) && !_isStreamOpen) {
        OnOpen();

        _isStreamOpen = true;
      }
    }

    void Update() {
      if (Application.isPlaying || streamDuringEditMode) {
        if (!_isStreamOpen) {
          OnOpen();

          _isStreamOpen = true;
        }

        OnSend(this.transform.ToWorldPose());
      }
    }

    void OnDisable() {
      if ((Application.isPlaying || streamDuringEditMode) && _isStreamOpen) {
        OnClose();

        _isStreamOpen = false;
      }
    }

    private void checkEditTimeStreamState() {
      if (!Application.isPlaying && !streamDuringEditMode && _isStreamOpen) {
        OnClose();
      }
    }

  }


}