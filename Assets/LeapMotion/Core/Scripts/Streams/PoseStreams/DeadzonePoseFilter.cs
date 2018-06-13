using Leap.Unity.Attributes;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Leap.Unity.Streams {

  [AddComponentMenu("Leap/Streams/Pose/Deadzone Pose Filter")]
  public class DeadzonePoseFilter : MonoBehaviour,
                                    IStream<Pose>,
                                    IStreamReceiver<Pose> {

    [Tooltip("Poses whose position lengths are less than this radius will not be "
           + "streamed.")]
    [MinValue(0f)]
    public float deadzoneRadius = 0.02f;
    public float deadzoneRadiusSqr { get { return deadzoneRadius * deadzoneRadius; } }

    public event Action OnOpen = () => { };
    public event Action<Pose> OnSend = (pose) => { };
    public event Action OnClose = () => { };

    public void Close() { OnClose(); }

    public void Open() { OnOpen(); }

    public void Receive(Pose data) {
      if (data.position.sqrMagnitude > deadzoneRadiusSqr) {
        OnSend(data);
      }
    }
  }

}
