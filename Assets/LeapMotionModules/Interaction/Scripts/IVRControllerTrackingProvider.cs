using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Leap.Unity.Interaction {

  /// <summary>
  /// The interface for providing tracking data to an InteractionVRController.
  /// 
  /// It is recommended that tracking providers update their controllers' tracking
  /// data before the Interaction Manager runs every FixedUpdate to minimize latency.
  /// 
  /// For a reference implementation, refer to DefaultVRNodeTrackingProvider.
  /// </summary>
  public interface IVRControllerTrackingProvider {

    /// <summary>
    /// Returns whether or not this provider is currently tracking the controller for
    /// which it provides data.
    /// </summary>
    bool GetIsTracked();

    /// <summary>
    /// Sets up a subscription so that the provided Action is called whenever new
    /// tracking data is available for this controller.
    /// 
    /// It is recommended that tracking providers update their controllers' tracking
    /// data before the Interaction Manager runs every FixedUpdate to minimize latency.
    /// </summary>
    void Subscribe(Action<Vector3, Quaternion> onTrackingDataUpdate);

    /// <summary>
    /// Unsubscribes the provided Action from being called when new tracking data is made
    /// available. Most often this is to allow components to switch to different methods
    /// to receive tracking data, or to ensure that their methods are not
    /// double-subscribed. (If the provided Action is not subscribed, this method should
    /// be a no-op.)
    /// </summary>
    void Unsubscribe(Action<Vector3, Quaternion> onTrackingDataUpdate);

  }

}