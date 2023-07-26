/******************************************************************************
 * Copyright (C) Ultraleap, Inc. 2011-2023.                                   *
 *                                                                            *
 * Use subject to the terms of the Apache License 2.0 available at            *
 * http://www.apache.org/licenses/LICENSE-2.0, or another agreement           *
 * between Ultraleap and you, your company or other organization.             *
 ******************************************************************************/

using UnityEngine;

namespace Leap.Unity
{
    /// <summary>
    /// Implements a resample-able transform history.
    /// </summary>
    public class TransformHistory
    {
        public RingBuffer<TransformData> history;
        public TransformHistory(int capacity = 32)
        {
            history = new RingBuffer<TransformData>(capacity);
        }

        //Store current Transform in History
        public void UpdateDelay(Pose curPose, long timestamp)
        {
            TransformData currentTransform =
              new TransformData()
              {
                  time = timestamp,
                  position = curPose.position,
                  rotation = curPose.rotation,
              };

            history.Add(currentTransform);
        }

        //Calculate delayed Transform
        public void SampleTransform(long timestamp, out Vector3 delayedPos, out Quaternion delayedRot)
        {
            TransformData desiredTransform = TransformData.GetTransformAtTime(history, timestamp);
            SampleTransform(timestamp, true, out delayedPos, out delayedRot);
        }

        //Calculate delayed Transform
        internal void SampleTransform(long timestamp, bool useInterpolation, out Vector3 delayedPos, out Quaternion delayedRot)
        {
            TransformData desiredTransform = TransformData.GetTransformAtTime(history, timestamp, useInterpolation);
            delayedPos = desiredTransform.position;
            delayedRot = desiredTransform.rotation;
        }


        public struct TransformData
        {
            public long time; // microseconds
            public Vector3 position; //meters
            public Quaternion rotation; //magic

            public static TransformData Lerp(TransformData from, TransformData to, long time)
            {
                if (from.time == to.time)
                {
                    return from;
                }
                float fraction = (float)(((double)(time - from.time)) / ((double)(to.time - from.time)));
                return new TransformData()
                {
                    time = time,
                    position = Vector3.Lerp(from.position, to.position, fraction),
                    rotation = Quaternion.Slerp(from.rotation, to.rotation, fraction)
                };
            }

            /// <summary>
            /// Returns a head pose transform for a given time. If interpolation is off returns the latest head pose, otherwise
            /// interpolates transform data from the adjacent samples in the history buffer
            /// </summary>
            /// <param name="history">Buffer containing a set of timestamped samples of the head pose transform data</param>
            /// <param name="desiredTime">Target time for the transform data</param>
            /// <param name="useInterplation">Should interpolation be used to interpolate the transform data. If <see langword="false"/> will 
            /// return the latest head pose</param>
            /// <returns>The transform data for the target time, intepolated from the history buffer for the target time if 
            /// interpolation is on, otherwise just returns the latest transform data</returns>
            public static TransformData GetTransformAtTime(RingBuffer<TransformData> history, long desiredTime, bool useInterplation = true)
            {
                for (int i = history.Count - 1; i > 0; i--)
                {
                    if (history.Get(i).time >= desiredTime && history.Get(i - 1).time < desiredTime)
                    {
                        if (useInterplation)
                        {
                            return Lerp(history.Get(i - 1), history.Get(i), desiredTime);
                        }
                        else
                        {
                            // Return the latest pose, do not return the closest tranform data to the desired time
                            return history.GetLatest();
                        }
                    }
                }

                if (history.Count > 0)
                {
                    return history.GetLatest();
                }
                else
                {
                    // No history data available.
                    return new TransformData()
                    {
                        time = desiredTime,
                        position = Vector3.zero,
                        rotation = Quaternion.identity
                    };
                }
            }
        }
    }
}