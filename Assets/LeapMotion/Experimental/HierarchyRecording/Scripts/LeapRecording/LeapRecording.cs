/******************************************************************************
 * Copyright (C) Leap Motion, Inc. 2011-2018.                                 *
 * Leap Motion proprietary and confidential.                                  *
 *                                                                            *
 * Use subject to the terms of the Leap Motion SDK Agreement available at     *
 * https://developer.leapmotion.com/sdk_agreement, or another agreement       *
 * between Leap Motion and you, your company or other organization.           *
 ******************************************************************************/

using System.Collections.Generic;
using UnityEngine;

namespace Leap.Unity.Recording {

  public abstract class LeapRecording : ScriptableObject {

    /// <summary>
    /// Converts nanoseconds to seconds. Leap frames store timestamps in nanoseconds.
    /// </summary>
    public const double NS_TO_S = 1e-6;

    /// <summary>
    /// Converts seconds to nanoseconds. Leap frames store timestamps in nanoseconds.
    /// </summary>
    public const double S_TO_NS = 1e6;

    /// <summary>
    /// Returns the length of the recording in seconds.
    /// </summary>
    public abstract float length { get; }

    /// <summary>
    /// Loads this recording with data from the provided TEMPORARY list of frames. These
    /// frames reflect raw recorded Leap data. The actual frame storage scheme utilized
    /// is up to the implementation, but if the implementation wishes to store data from
    /// these frames, that data must be copied!
    /// 
    /// If this recording already has frame data, a call to this method should overwrite
    /// that data.
    /// </summary>
    public abstract void LoadFrames(List<Frame> frames);

    /// <summary>
    /// Samples the recording at the given time.  Caller must provide a frame object that
    /// will be filled with the sampled frame data.  Returns true if the frame was filled
    /// with recording data, and false if it was not.
    /// 
    /// The user can also optionally configure if the input time is allowed to be clamped 
    /// to a valid timestamp.  When true, even if the requested time does not exist, the
    /// recording will still fill the frame with data taken from the closest valid
    /// timestamp.
    /// </summary>
    public abstract bool Sample(float time, Frame toFill, bool clampTimeToValid = true);

  }

}
