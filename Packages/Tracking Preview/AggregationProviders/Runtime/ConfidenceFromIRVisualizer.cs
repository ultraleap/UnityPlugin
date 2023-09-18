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
    [SerializeField]
    public LeapImageRetriever imageRetriever;

    [SerializeField]
    public PoseToIRImageConfidence poseToIRImageConfidence;

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
        outputImage.texture = imageRetriever.TextureData.TextureData.CombinedTexture;
        outputImageWithProjectedPoints.texture = poseToIRImageConfidence.IRImageWithOverlaidPosePoints;
    }
}
