/******************************************************************************
 * Copyright (C) Ultraleap, Inc. 2011-2024.                                   *
 *                                                                            *
 * Use subject to the terms of the Apache License 2.0 available at            *
 * http://www.apache.org/licenses/LICENSE-2.0, or another agreement           *
 * between Ultraleap and you, your company or other organization.             *
 ******************************************************************************/

using System.Collections.Generic;
using UnityEngine;

namespace Leap
{
    /// <summary>
    /// Takes a sourceDevice and aligns a targetDevice to it by transforming the targetDevice 
    /// until all bones from both hands align to within the alignmentVariance value
    /// 
    /// Can be forced to re-align by calling ReAlignProvider()
    /// </summary>
    public class MultideviceAlignment : MonoBehaviour
    {
        public LeapProvider sourceDevice;
        public LeapProvider targetDevice;

        [Tooltip("The maximum variance in bone positions allowed to consider the provider aligned. (in metres)")]
        public float alignmentVariance = 0.02f;

        List<Vector3> sourceHandPoints = new List<Vector3>();
        List<Vector3> targetHandPoints = new List<Vector3>();

        bool positioningComplete = false;

        KabschSolver solver = new KabschSolver();

        public void ReAlignProvider()
        {
            targetDevice.transform.position = Vector3.zero;
            positioningComplete = false;
        }

        void Update()
        {
            if (!positioningComplete)
            {
                foreach (var sourceHand in sourceDevice.CurrentFrame.Hands)
                {
                    var targetHand = targetDevice.CurrentFrame.GetHand(sourceHand.IsLeft ? Chirality.Left : Chirality.Right);

                    if (targetHand != null)
                    {
                        for (int j = 0; j < 5; j++)
                        {
                            for (int k = 0; k < 4; k++)
                            {
                                sourceHandPoints.Add(sourceHand.fingers[j].bones[k].Center);
                                targetHandPoints.Add(targetHand.fingers[j].bones[k].Center);
                            }
                        }

                        // This is temporary while we check if any of the hands points are not close enough to eachother
                        positioningComplete = true;

                        for (int i = 0; i < sourceHandPoints.Count; i++)
                        {
                            if (Vector3.Distance(sourceHandPoints[i], targetHandPoints[i]) > alignmentVariance)
                            {
                                // we are already as aligned as we need to be, we can exit the alignment stage
                                positioningComplete = false;
                                break;
                            }
                        }

                        if (positioningComplete)
                        {
                            return;
                        }

                        Matrix4x4 deviceToOriginDeviceMatrix =
                          solver.SolveKabsch(targetHandPoints, sourceHandPoints, 200);

                        // transform the targetDevice.transform by the deviceToOriginDeviceMatrix
                        Matrix4x4 newTransform = deviceToOriginDeviceMatrix * targetDevice.transform.localToWorldMatrix;
                        targetDevice.transform.position = newTransform.GetVector3Kabsch();
                        targetDevice.transform.rotation = newTransform.GetQuaternionKabsch();
                        targetDevice.transform.localScale = Vector3.Scale(targetDevice.transform.localScale, deviceToOriginDeviceMatrix.lossyScale);


                        targetHandPoints.Clear();
                        sourceHandPoints.Clear();
                        return;
                    }
                }
            }
        }
    }
}