using Leap.Unity.Attributes;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Leap.Unity.Streams {

  public class DeltaPoseFilter : MonoBehaviour,
                                 IStream<Pose>,
                                 IStreamReceiver<Pose> {

    [Tooltip("Input poses will be transformed into this transform's local pose space. "
           + "Poses do not encode any scale information, only position and rotation. "
           + "Changes in position due to the transform's parent scale will propagate, "
           + "however.")]
    public Transform referencePoseSource = null;
    
    public Pose referencePose { get { return referencePoseSource.ToPose(); } }

    public event Action OnOpen = () => { };
    public event Action<Pose> OnSend = (pose) => { };
    public event Action OnClose = () => { };

    public void Close() { OnClose(); }

    public void Open() { OnOpen(); }

    public void Receive(Pose data) {
      var deltaPose = referencePose.inverse * data;
      OnSend(deltaPose);
    }
  }

}
