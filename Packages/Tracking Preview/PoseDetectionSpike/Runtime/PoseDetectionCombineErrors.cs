using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PoseDetectionCombineErrors : MonoBehaviour
{
    public PoseDetectionBase detectionBase;
    public float defaultThresholdPitch;
    public float defaultThresholdYaw;
    //public bool averageFingers;
    public float[] thresholdsPitch;
    public float[] thresholdsYaw;

    public bool poseWithinThreshold;

    public Renderer cubeRenderer;


    float[] boneErrorsPitch;
    float[] boneErrorsYaw;

    // Start is called before the first frame update
    void Start()
    {
        boneErrorsPitch = detectionBase.boneErrorPitch;
    }

    // Update is called once per frame
    void Update()
    {
        cubeRenderer.material.color = poseWithinThreshold ? Color.green : Color.red;

        if (boneErrorsPitch == null || boneErrorsPitch.Length == 0)
        {
            boneErrorsPitch = detectionBase.boneErrorPitch;
        }
        if (boneErrorsYaw == null || boneErrorsYaw.Length == 0)
        {
            boneErrorsYaw = detectionBase.boneErrorYaw;
        }
        if (thresholdsPitch == null || thresholdsPitch.Length == 0)
        {
            thresholdsPitch = new float[boneErrorsPitch.Length];
            for(int i = 0; i < thresholdsPitch.Length; i++)
            {
                thresholdsPitch[i] = defaultThresholdPitch;
            }
        }
        if (thresholdsYaw == null || thresholdsYaw.Length == 0)
        {
            thresholdsYaw = new float[boneErrorsYaw.Length];
            for (int i = 0; i < thresholdsYaw.Length; i++)
            {
                thresholdsYaw[i] = defaultThresholdYaw;
            }
        }



        //if (!averageFingers)
        //{
        for (int i = 0; i < boneErrorsPitch.Length; i++)
        {
            if(Mathf.Abs(boneErrorsPitch[i]) > thresholdsPitch[i] || Mathf.Abs(boneErrorsYaw[i]) > thresholdsYaw[i])
            {
                poseWithinThreshold = false;
                return;
            }
        }
        poseWithinThreshold = true;
        //}
        //else
        //{
        //    for(int i = 0; i < 5; i++)
        //    {
        //        float fingerErrorSumPitch = 0;
        //        float fingerErrorSumYaw = 0;
        //        for (int j = 0; j < 4; j++)
        //        {
        //            fingerErrorSumPitch += Mathf.Abs(boneErrorsPitch[i * 4 + j]);
        //            fingerErrorSumYaw += Mathf.Abs(boneErrorsYaw[i * 4 + j]);
        //        }

        //        float fingerErrorAveragePitch = fingerErrorSumPitch / 4f;
        //        float fingerErrorAverageYaw = fingerErrorSumYaw / 4f;

                

        //        if(i == 0) // thumb
        //        {
        //            //Debug.Log(boneErrors[0] + ", " + boneErrors[1]);
        //            fingerErrorAveragePitch = (fingerErrorSumPitch - Mathf.Abs(boneErrorsPitch[0])) / 3f;
        //            fingerErrorAverageYaw = (fingerErrorSumYaw - Mathf.Abs(boneErrorsYaw[0])) / 3f;
        //        }

        //        if (fingerErrorAveragePitch > thresholdPitch || fingerErrorAverageYaw > thresholdYaw)
        //        {
        //            poseWithinThreshold = false;
        //            return;
        //        }
        //    }
        //    poseWithinThreshold = true;
        //}

        
    }
}
