using Leap.Unity;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using static System.Net.Mime.MediaTypeNames;

/// <summary>
/// Takes image data from the PoseToIRImageConfidence script and updates an output image (RawImage)
/// </summary>
public class ConfidenceFromIRVisualizer : MonoBehaviour
{
    [Header("Inputs")]
    [SerializeField]
    public LeapImageRetriever imageRetriever;

    [SerializeField]
    public PoseToIRImageConfidence poseToIRImageConfidence;

    [Header("Output Targets")]
    [SerializeField]
    public RawImage outputImage;

    [SerializeField]
    public RawImage outputImageWithProjectedPoints;

    // Start is called before the first frame update
    void Start()
    {
        
    }
     
    // Update is called once per frame
    void Update()
    {
        if (outputImage != null)
            outputImage.texture = imageRetriever.TextureData.TextureData.CombinedTexture;

        if (outputImageWithProjectedPoints!= null)
            outputImageWithProjectedPoints.texture = poseToIRImageConfidence.IRImageWithOverlaidPosePoints;
    }
}
