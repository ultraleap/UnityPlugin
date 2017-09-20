using System;
using UnityEngine;

namespace Leap.Unity.Glint.Internal {

  public class Request {
    //public Status status = Status.NotReady;
    public Texture gpuTexture = null;
    public float[] cpuData    = null;
    public Action onRetrieved = null;

    private int _waitTimeInFrames = 0;
    private int _framesRemaining = 0;
    public int waitTimeInFrames {
      get { return _waitTimeInFrames; }
      set {
        _waitTimeInFrames = value;
        _framesRemaining = value;
      }
    }

    public Status status = Status.Ready;

    public bool readyForRetrieval {
      get { return _framesRemaining < 0; }
    }

    // TODO: support recording profiling data
    //#region Request Profiling

    //private float[] _profilingData = new float[] { 0f, 0f, 0f };

    //public float nativeMapMs {
    //  get { return _profilingData[0]; }
    //  private set { _profilingData[0] = value; }
    //}

    //public float nativeCopyMs {
    //  get { return _profilingData[1]; }
    //  private set { _profilingData[1] = value; }
    //}

    //public float managedCopyMs {
    //  get { return _profilingData[2]; }
    //  private set { _profilingData[2] = value; }
    //}

    //#endregion

    public void Clear() {
      //status = Status.NotReady;
      gpuTexture  = null;
      cpuData     = null;
      onRetrieved = null;

      waitTimeInFrames = 0;
    }

    public void TickFrame() {
      _framesRemaining -= 1;
    }
  }

}
