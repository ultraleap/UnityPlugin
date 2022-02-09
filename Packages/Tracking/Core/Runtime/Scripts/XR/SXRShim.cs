using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

#if SVR

namespace Leap.Unity
{
    public class SxrShim
    {
        private struct SxrHeadPose
        {
#pragma warning disable 0649
            public Quaternion Orientation;
            public Vector3 Position;
#pragma warning restore 0649
        };

        private struct SxrHeadPoseState
        {
#pragma warning disable 0649
            public SxrHeadPose Pose;
            public int Status;
            public long TimestampNs;
            public long FetchTimestampNs;
            public long ExpectedDisplayTimeNs;
#pragma warning restore 0649
        }

        private static class Native
        {
            [DllImport("sxrapi")]
            internal static extern float sxrGetPredictedDisplayTime();
            [DllImport("sxrapi")]
            internal static extern int sxrGetQvrDataTransform(ref Matrix4x4 transform);
            [DllImport("sxrapi")]
            internal static extern int sxrGetQvrDataInverse(ref Matrix4x4 transform);
            [DllImport("sxrapi")]
            internal static extern SxrHeadPoseState sxrGetHistoricHeadPose(long timestampNs);
            [DllImport("sxrapi")]
            internal static extern float sxrGetPredictedDisplayTimePipelined(uint pipelineDepth);
            [DllImport("sxrapi")]
            internal static extern SxrHeadPoseState sxrGetPredictedHeadPose(float predictedTimeMs);
        }

        public static float GetPredictedDisplayTime(bool isMultiThreadedRender) => Native.sxrGetPredictedDisplayTimePipelined(isMultiThreadedRender ? 2u : 1u);

        public static bool GetHistoricHeadPose(long timestampNs, out Quaternion orientation, out Vector3 position)
        {
            var headPose = Native.sxrGetHistoricHeadPose(timestampNs);
            orientation = headPose.Pose.Orientation;
            position = headPose.Pose.Position;
            return headPose.Status != 0;
        }

        public static bool GetPredictedHeadPose(float predictedTimeMs, out Quaternion orientation, out Vector3 position)
        {
            var headPose = Native.sxrGetPredictedHeadPose(predictedTimeMs);
            orientation = headPose.Pose.Orientation;
            position = headPose.Pose.Position;
            return headPose.Status != 0;
        }
    }
}

#endif