using UnityEngine;

namespace Leap.Unity.Recording {

  public abstract class LeapRecording : ScriptableObject {

    /// <summary>
    /// Returns the length of the recording measured in seconds.
    /// </summary>
    public abstract float Length { get; }

    /// <summary>
    /// Samples the recording at the given time.  Must provide a frame object
    /// that will be filled with the sampled frame data.  Returns true if
    /// the frame was filled with recording data, and false if it was not.
    /// 
    /// The user can also optionally configure if the input time is allowed 
    /// to be clamped to a valid timestamp.  When true, even if the requested
    /// time does not exist, the recording will still fill the frame with data
    /// taken from the closest valid timestamp.
    /// </summary>
    public abstract bool Sample(float time, Frame toFill, bool clampTimeToValid = true);
  }
}
