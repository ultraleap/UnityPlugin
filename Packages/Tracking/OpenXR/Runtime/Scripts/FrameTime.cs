namespace Ultraleap.Tracking.OpenXR
{
    /// <summary>
    /// Which timestamp to use for calculating the position of the hands for rendering or physics.
    /// </summary>
    public enum FrameTime
    {
        /// <summary>
        /// Time based on the previous frame's predicted display time & predicted duration.
        /// </summary>
        /// <remarks>This should be used for updates on the main thread.</remarks>
        OnUpdate = 0,

        /// <summary>
        /// Time based on the predicted display time of the current frame (for render thread).
        /// <remarks>This should be used for updates on the render thread.</remarks>
        /// </summary>
        OnBeforeRender = 0
    }
}