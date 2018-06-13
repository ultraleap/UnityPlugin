using Leap.Unity.Attributes;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Leap.Unity.Streams {

  public class PoseStreamTransformFollower : MonoBehaviour, IStreamReceiver<Pose> {

    public virtual void Open() {

    }

    public virtual void Receive(Pose data) {
      transform.SetPose(data);
    }

    public virtual void Close() {

    }

  }


}