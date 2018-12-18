/******************************************************************************
 * Copyright (C) Leap Motion, Inc. 2011-2018.                                 *
 * Leap Motion proprietary and confidential.                                  *
 *                                                                            *
 * Use subject to the terms of the Leap Motion SDK Agreement available at     *
 * https://developer.leapmotion.com/sdk_agreement, or another agreement       *
 * between Leap Motion and you, your company or other organization.           *
 ******************************************************************************/

using UnityEngine;
using Leap.Unity.Attributes;

namespace Leap.Unity.Examples {

  public class DeinterlacingPostProcess : PostProcessProvider {

    [Tooltip("This provider will receive data from only the " +
             "device that contains this in its serial number.")]
    [EditTimeOnly]
    [SerializeField]
    protected string _specificSerialNumber;

    protected uint _thisProvidersID = 0;
    protected LeapServiceProvider _provider;
    protected Frame _retransformedFrame = new Frame();

    public void Start() {
      this._dispatchManually = true;
      _provider = _inputLeapProvider as LeapServiceProvider;
      _provider.UseInterpolation = false;

      _provider.GetLeapController().FrameReady -= dispatchFrameEvent;
      _provider.GetLeapController().FrameReady += dispatchFrameEvent;
    }

    public void Update() {
      if (_thisProvidersID == 0) {
        DeviceList devices = _provider.GetLeapController().Devices;
        for (uint i = 0; i < devices.Count; i++) {
          if (devices[(int)i].SerialNumber.Contains(_specificSerialNumber)) {
            _thisProvidersID = i + 1;
            break;
          }
        }
      }
    }

    void dispatchFrameEvent(object sender, FrameEventArgs args) {
      if (Application.isPlaying && args.frame.DeviceID == _thisProvidersID) {
        _retransformedFrame.CopyFrom(args.frame).
                            Transform(transform.GetLeapMatrix());
      }
    }

    public override void ProcessFrame(ref Frame inputFrame) {
      if (!Time.inFixedTimeStep) {
        DispatchUpdateFrameEvent(_retransformedFrame);
      } else {
        DispatchFixedFrameEvent(_retransformedFrame);
      }
    }

  }
}
