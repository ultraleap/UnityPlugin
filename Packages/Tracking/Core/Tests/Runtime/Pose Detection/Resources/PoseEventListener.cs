using UnityEngine;

namespace Leap.Testing
{
    /// <summary>
    /// Listens to events from the PoseDetector and captures the current state
    /// </summary>
    public class PoseEventListener : MonoBehaviour
    {
        public bool poseDetected = false;
        public bool whilePoseDetected = false;
        public bool poseLost = false;

        public void PoseDetected()
        {
            poseDetected = true;
        }

        public void WhilePoseDetected()
        {
            whilePoseDetected = true;
        }

        public void PoseLost()
        {
            poseLost = true;
        }
    }
}