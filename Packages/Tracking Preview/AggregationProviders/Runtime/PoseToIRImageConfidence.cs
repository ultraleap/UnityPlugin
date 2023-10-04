using Leap;
using Leap.Unity;
using LeapInternal;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Hand = Leap.Hand;
using Vector2 = UnityEngine.Vector2;
using Vector3 = UnityEngine.Vector3;

/// <summary>
/// Stores information about the confidence in the pose of each finger inthe hand, based on the tip location
/// </summary>
public class TipConfidences
{
    public HandFingerConfidences LeftConfidence = new HandFingerConfidences();
    public HandFingerConfidences RightConfidence = new HandFingerConfidences();
}

/// <summary>
/// Stores confidence info for a finger. 
/// </summary>
public class FingerConfidence
{
    /// <summary>
    /// A value indicating how confident we currently are in the finger pose
    /// </summary>
    public float Confidence;

    /// <summary>
    /// timestamp of the last good data (high confidence data)
    /// </summary>
    public long Timestamp;

    /// <summary>
    /// Finger pose captured when we last had confidence in the pose
    /// </summary>
    public Finger LastConfidentPositionForFinger_LeapSpace = new Finger();

    /// <summary>
    /// Location in the IR image of the sampling point when we last had high confidence in the pose
    /// </summary>
    public Vector2 LastConfidentPosition_PixelSpace;
}

public class HandFingerConfidences
{
    public FingerConfidence Thumb = new FingerConfidence();
    public FingerConfidence Index = new FingerConfidence();
    public FingerConfidence Middle = new FingerConfidence();
    public FingerConfidence Ring = new FingerConfidence();
    public FingerConfidence Pinky = new FingerConfidence();
}                                                               

public enum FingerTipName
{
    Thumb = 0,
    IndexFinger = 1,
    MiddleFinger = 2,
    RingFinger = 3,
    PinkyFinger = 4
}

public enum JointName
{
    Palm = 0,
    Wrist = 1,
    ThumbMetacarpal = 2,
    ThumbProximal = 3,
    ThumbDistal = 4,
    ThumbTip = 5,
    IndexMetacarpal = 6,
    IndexProximal = 7,
    IndexIntermediate = 8,
    IndexDistal = 9,
    IndexTip = 10,
    MiddleMetacarpal = 11,
    MiddleProximal = 12,
    MiddleIntermediate = 13,
    MiddleDistal = 14,
    MiddleTip = 15,
    RingMetacarpal = 16,
    RingProximal = 17,
    RingIntermediate = 18,
    RingDistal = 19,
    RingTip = 20,
    LittleMetacarpal = 21,
    LittleProximal = 22,
    LittleIntermediate = 23,
    LittleDistal = 24,
    LittleTip = 25,
    Elbow = 26
}


/// <summary>
/// Uses the coorelation between the hand pose and the IR image at the projected fingertup locations
/// to determine a confidence in the position of the fingers in the returned pose.
/// </summary>
public class PoseToIRImageConfidence : MonoBehaviour
{

    [Header("Inputs")]
    [SerializeField]
    public LeapImageRetriever imageRetriever;

    [SerializeField]
    public LeapServiceProvider leapProvider;

    [Header("Settings")]
    [SerializeField]
    [Range(0f, 255f)]
    [Tooltip("The average IR pixel value below which we shade the IR image in blue)")]
    public int IRThreshold = 7;

    [SerializeField]
    [Range(0f, 255f)]
    [Tooltip("The average IR pixel value below which we colour the joint as low confidence (red)")]
    public int LowerConfidencePixelThreshold = 7;

    [SerializeField]
    [Range(0f, 255f)]
    [Tooltip("The average IR pixel value above which we colour the joint as high confidence (green)")]
    public int UpperConfidencePixelThreshold = 35;

    [SerializeField]
    [Range(0f, 255f)]
    [Tooltip("The average IR pixel value above which the joint position is considered to be high confidence")]
    public int LastKnownGoodPositionConfidencePixelThreshold = 35;

    [SerializeField]
    [Range(0f, 10f)]
    [Tooltip("The distance along a vector from the fingertip to the intermediate joint to use as the sampling origin")]
    public float TipOffset_cm = 0.55f;

    [Header("Visualization")]
    [Tooltip("Generate an image with debug visuals for fingertip locations and confidences")]
    [SerializeField]
    public bool generateImageVisuals;


    [HideInInspector]
    [Tooltip("Image showing fingertip locations (coloured squares) and confidences (colour)")]
    public Texture2D IRImageWithOverlaidPosePoints;

    private List<(Vector3 position, FingerTipName fingerName, Chirality hand)> tips_LeapUnits = new List<(Vector3, FingerTipName, Chirality)>();

    private Hand handWhenConfidenceEvaluated_Left = new Hand();
    private long timestampWhenConfidenceEvaluated_Left;
    private Hand handWhenConfidenceEvaluated_Right = new Hand();
    private long timestampWhenConfidenceEvaluated_Right;

    private List<(Vector2 position, FingerTipName fingerName, Chirality hand)> tipPixels = new List<(Vector2, FingerTipName, Chirality)>();

    private UnityEngine.Matrix4x4? extrinsics;
    private Controller leapController;
    private const Image.CameraType targetCamera = Image.CameraType.LEFT;

    private bool newImage = false;
    private Image image;
    private Color32[] imagePixels;

    private LeapTransform unityToLeapNonXRTransform;
    public static readonly float M_TO_MM = 1000;

    public TipConfidences TipConfidence = new TipConfidences();

    private int highestDeviceIndex = 0;

    // Start is called before the first frame update
    void Start()
    {
        if (leapProvider != null)
        {
            leapController = leapProvider.GetLeapController();

            leapController.ImageReady += LeapController_ImageReady;
            leapController.RawFrameReady += LeapController_RawFrameReady;
        }

        // Leap Y distance above controller (increasing above)
        ///     X = left / right (increasing to left)
        ///     Z = up / down (increasing up)
        unityToLeapNonXRTransform = new LeapTransform(Vector3.zero, UnityEngine.Quaternion.identity, new Vector3(M_TO_MM, M_TO_MM, M_TO_MM));
        unityToLeapNonXRTransform.MirrorZ();
    }

    private void LeapController_RawFrameReady(object sender, RawFrameEventArgs e)
    {
        tips_LeapUnits.Clear();
        tipPixels.Clear();

        if (leapProvider != null)
        {
            if (e.HasLeftHand)
            {
                Hand currentHand = leapProvider.GetHand(Chirality.Left);
                if (currentHand != null)
                {
                    handWhenConfidenceEvaluated_Left = CopyFromOtherExtensions.CopyFrom(handWhenConfidenceEvaluated_Left, currentHand); // LeapCExt.CopyFrom(handWhenConfidenceEvaluated_Left, ref h, (long)1);
                    timestampWhenConfidenceEvaluated_Left = leapProvider.CurrentFrame.Timestamp;
                    UpdateFingerTipsRaw(e.LeftHand);
                }
            }

            if (e.HasRightHand)
            {
                var currentHand = leapProvider.GetHand(Chirality.Right);

                if (currentHand != null)
                {
                    handWhenConfidenceEvaluated_Right = CopyFromOtherExtensions.CopyFrom(handWhenConfidenceEvaluated_Right, currentHand); // LeapCExt.CopyFrom(handWhenConfidenceEvaluated_Right, ref h,(long) 1);
                    timestampWhenConfidenceEvaluated_Right = leapProvider.CurrentFrame.Timestamp;
                    UpdateFingerTipsRaw(e.RightHand);
                }
            }
        }
    }

    private void UpdateFingerTipsRaw(LEAP_HAND hand)
    {
        if (leapController != null && leapController.Devices.Count >= 1 && image != null && newImage)
        {
            if (leapController.Devices.Count > highestDeviceIndex) 
            {
                extrinsics = null;
                highestDeviceIndex = leapController.Devices.Count;
            }

            if (extrinsics == null)
            {
                try
                {
                    extrinsics = leapController.LeapExtrinsicCameraMatrix(targetCamera, leapController.Devices[leapController.Devices.Count - 1]);
                }
                catch (Exception ex)
                {
                    Debug.LogException(ex);
                }
            }

            UpdateFingerTipsRawForHand(hand);
            UpdateTargetPixels(hand);
        }
    }

    private IEnumerable<(Vector3 position, FingerTipName finger, Chirality handChirality)> UpdateFingerTipsRawForHand(LEAP_HAND hand)
    {
        Chirality chirality = hand.type == eLeapHandType.eLeapHandType_Left ? Chirality.Left : Chirality.Right;
        
        if (leapProvider != null)
        {
            if (TipOffset_cm == 0)
            {
                tips_LeapUnits.Add((AsVector3(hand.pinky.distal.next_joint), FingerTipName.PinkyFinger, chirality));
                tips_LeapUnits.Add((AsVector3(hand.ring.distal.next_joint), FingerTipName.RingFinger, chirality));
                tips_LeapUnits.Add((AsVector3(hand.middle.distal.next_joint), FingerTipName.MiddleFinger, chirality));
                tips_LeapUnits.Add((AsVector3(hand.index.distal.next_joint), FingerTipName.IndexFinger, chirality));
                tips_LeapUnits.Add((AsVector3(hand.thumb.distal.next_joint), FingerTipName.Thumb, chirality));
            }
            else
            {
                tips_LeapUnits.Add((ScaleBackFromDistal(hand.pinky.distal), FingerTipName.PinkyFinger, chirality));
                tips_LeapUnits.Add((ScaleBackFromDistal(hand.ring.distal), FingerTipName.RingFinger, chirality));
                tips_LeapUnits.Add((ScaleBackFromDistal(hand.middle.distal), FingerTipName.MiddleFinger, chirality));
                tips_LeapUnits.Add((ScaleBackFromDistal(hand.index.distal), FingerTipName.IndexFinger, chirality));
                tips_LeapUnits.Add((ScaleBackFromDistal(hand.thumb.distal), FingerTipName.Thumb, chirality));
            }
        }

        return tips_LeapUnits;

        FingerTipName FingerTypeToName(Finger.FingerType type)
        {
            switch (type)
            {
                case Finger.FingerType.TYPE_THUMB:
                    return FingerTipName.Thumb;
                    
                case Finger.FingerType.TYPE_INDEX:
                    return FingerTipName.IndexFinger;
                 
                case Finger.FingerType.TYPE_MIDDLE:
                    return FingerTipName.MiddleFinger;

                case Finger.FingerType.TYPE_RING:
                    return FingerTipName.RingFinger;

                case Finger.FingerType.TYPE_PINKY:
                    return FingerTipName.PinkyFinger;

                default:
                    throw new Exception("Unknown finger type");  
            }
        }

        Vector3 AsVector3(LEAP_VECTOR v)
        {
            return new Vector3(v.x, v.y, v.z);
        }

        Vector3 ScaleBackFromDistal(LEAP_BONE endBone)
        { 
            Vector3 boneV = AsVector3(endBone.prev_joint) - AsVector3(endBone.next_joint);
            boneV.Scale(new Vector3(TipOffset_cm, TipOffset_cm, TipOffset_cm));
            return AsVector3(endBone.next_joint) + boneV;
        }
    }


    private void LeapController_ImageReady(object sender, ImageEventArgs e)
    {
        newImage = true;
        image = e.image;

        if (image != null)
        {
            if (IRImageWithOverlaidPosePoints == null)
            {
                IRImageWithOverlaidPosePoints = new Texture2D(e.image.Width, e.image.Height, TextureFormat.ARGB32, false);
            }

            byte[] data = image.Data(targetCamera);

            if (generateImageVisuals)
            {
                if (imagePixels == null && image != null)
                {
                    imagePixels = new Color32[image.Width * image.Height];
                }

                for (int i = 0; i < image.Width * image.Height; i++)
                {
                    if (data[i] >= IRThreshold)
                    {
                        imagePixels[i].r = data[i];
                        imagePixels[i].g = data[i];
                        imagePixels[i].b = data[i];
                        imagePixels[i].a = (byte)255;
                    }
                    else
                    {
                        imagePixels[i].r = 0;
                        imagePixels[i].b = data[i];
                        imagePixels[i].g = 0;
                        imagePixels[i].a = (byte)255;
                    }
                }

                IRImageWithOverlaidPosePoints.SetPixels32(imagePixels);
            }

            foreach (var pixel in tipPixels)
            {
                int IRBrightnessIntegral = 0;
                int count = 0;

                for (int x = Math.Max(0, (int)pixel.position.x - 1); x < Math.Min(image.Width, (int)pixel.position.x + 1); x++)
                {
                    for (int y = Math.Max(0, (int)pixel.position.y - 1); y < Math.Min(image.Height, (int)pixel.position.y + 1); y++)
                    {
                        IRBrightnessIntegral+= (int) data[y*image.Width + x];
                        count++;
                    }
                }

                float lerpRange = UpperConfidencePixelThreshold - LowerConfidencePixelThreshold;
                float averagePixelIntensity = IRBrightnessIntegral / count;
                float confidence = Mathf.Lerp(0, 1, (averagePixelIntensity - LowerConfidencePixelThreshold) / lerpRange);

                if (generateImageVisuals)
                {
                    Color confidenceColor = Color.Lerp(Color.red, Color.green, confidence);

                    for (int x = Math.Max(0, (int)pixel.position.x - 1); x < Math.Min(image.Width, (int)pixel.position.x + 1); x++)
                    {
                        for (int y = Math.Max(0, (int)pixel.position.y - 1); y < Math.Min(image.Height, (int)pixel.position.y + 1); y++)
                        {
                            IRImageWithOverlaidPosePoints.SetPixel(x, y, confidenceColor);
                        }
                    }
                }

                bool highTipConfidence = averagePixelIntensity > LastKnownGoodPositionConfidencePixelThreshold;
                Vector2? lastKnownGoodPos_PixelSpace = null;

                HandFingerConfidences handConfidences = TipConfidence.RightConfidence;
                Hand handPoseWhenConfidenceEvaluated = handWhenConfidenceEvaluated_Right;
                long timestampWhenPoseEvaluated = timestampWhenConfidenceEvaluated_Right;

                if (pixel.hand == Chirality.Left)
                {
                    handConfidences = TipConfidence.LeftConfidence;
                    handPoseWhenConfidenceEvaluated = handWhenConfidenceEvaluated_Left;
                    timestampWhenPoseEvaluated = timestampWhenConfidenceEvaluated_Left;
                }

                if (handConfidences != null && handPoseWhenConfidenceEvaluated != null)
                {
                    switch (pixel.fingerName)
                    {
                        case FingerTipName.Thumb:
                            handConfidences.Thumb.Confidence = confidence;
                            if (highTipConfidence)
                            {
                                handConfidences.Thumb.LastConfidentPosition_PixelSpace = pixel.position;
                                handConfidences.Thumb.LastConfidentPositionForFinger_LeapSpace = CopyFromOtherExtensions.CopyFrom(handConfidences.Thumb.LastConfidentPositionForFinger_LeapSpace, handPoseWhenConfidenceEvaluated.GetThumb());
                                handConfidences.Thumb.Timestamp = timestampWhenPoseEvaluated;
                            }
                            else
                            {
                                lastKnownGoodPos_PixelSpace = handConfidences.Thumb.LastConfidentPosition_PixelSpace;
                            }
                            break;

                        case FingerTipName.IndexFinger:
                            handConfidences.Index.Confidence = confidence;
                            if (highTipConfidence)
                            {
                                handConfidences.Index.LastConfidentPosition_PixelSpace = pixel.position;
                                handConfidences.Index.LastConfidentPositionForFinger_LeapSpace = CopyFromOtherExtensions.CopyFrom(handConfidences.Index.LastConfidentPositionForFinger_LeapSpace, handPoseWhenConfidenceEvaluated.GetIndex());
                                handConfidences.Index.Timestamp = timestampWhenPoseEvaluated;
                            }
                            else
                            {
                                lastKnownGoodPos_PixelSpace = handConfidences.Index.LastConfidentPosition_PixelSpace;

                            }
                            break;

                        case FingerTipName.MiddleFinger:
                            handConfidences.Middle.Confidence = confidence;
                            if (highTipConfidence)
                            {
                                handConfidences.Middle.LastConfidentPosition_PixelSpace = pixel.position;
                                handConfidences.Middle.LastConfidentPositionForFinger_LeapSpace = CopyFromOtherExtensions.CopyFrom(handConfidences.Middle.LastConfidentPositionForFinger_LeapSpace, handPoseWhenConfidenceEvaluated.GetMiddle());
                                handConfidences.Middle.Timestamp = timestampWhenPoseEvaluated;
                            }
                            else
                            {
                                lastKnownGoodPos_PixelSpace = handConfidences.Middle.LastConfidentPosition_PixelSpace;
                            }
                            break;

                        case FingerTipName.RingFinger:
                            handConfidences.Ring.Confidence = confidence;
                            if (highTipConfidence)
                            {
                                handConfidences.Ring.LastConfidentPosition_PixelSpace = pixel.position;
                                handConfidences.Ring.LastConfidentPositionForFinger_LeapSpace = CopyFromOtherExtensions.CopyFrom(handConfidences.Ring.LastConfidentPositionForFinger_LeapSpace, handPoseWhenConfidenceEvaluated.GetRing());
                                handConfidences.Ring.Timestamp= timestampWhenPoseEvaluated;
                            }
                            else
                            {
                                lastKnownGoodPos_PixelSpace = handConfidences.Ring.LastConfidentPosition_PixelSpace;
                            }
                            break;

                        case FingerTipName.PinkyFinger:
                            handConfidences.Pinky.Confidence = confidence;
                            if (highTipConfidence)
                            {
                                handConfidences.Pinky.LastConfidentPosition_PixelSpace = pixel.position;
                                handConfidences.Pinky.LastConfidentPositionForFinger_LeapSpace = CopyFromOtherExtensions.CopyFrom(handConfidences.Pinky.LastConfidentPositionForFinger_LeapSpace, handPoseWhenConfidenceEvaluated.GetPinky());
                                handConfidences.Pinky.Timestamp = timestampWhenPoseEvaluated;
                            }
                            else
                            {
                                lastKnownGoodPos_PixelSpace = handConfidences.Pinky.LastConfidentPosition_PixelSpace;
                            }
                            break;

                        default:
                            break;
                    }
                }

                if (generateImageVisuals)
                {
                    if (!highTipConfidence && lastKnownGoodPos_PixelSpace.HasValue) // && pixel.fingerName == FingerTipName.Thumb)
                    {
                        for (int x = Math.Max(0, (int)lastKnownGoodPos_PixelSpace.Value.x - 1); x < Math.Min(image.Width, (int)lastKnownGoodPos_PixelSpace.Value.x + 1); x++)
                        {
                            for (int y = Math.Max(0, (int)lastKnownGoodPos_PixelSpace.Value.y - 1); y < Math.Min(image.Height, (int)lastKnownGoodPos_PixelSpace.Value.y + 1); y++)
                            {
                                IRImageWithOverlaidPosePoints.SetPixel(x, y, Color.yellow);
                            }
                        }
                    }
                }

            }

            if (generateImageVisuals)
            {
                IRImageWithOverlaidPosePoints.Apply();
            }
        }
    }

    // Update is called once per frame
    void Update()
    {
        //UpdateFingerTips();
    }

    //private void UpdateFingerTips()
    //{
    //    if (leapController != null && leapController.Devices.Count == 1 && image != null && newImage)
    //    {
    //        if (extrinsics == null)
    //        {
    //            extrinsics = leapController.LeapExtrinsicCameraMatrix(targetCamera, leapController.Devices[0]);
    //        }

    //        GetFingerTips();
    //        UpdateTargetPixels();
    //    }
    //}

    private void UpdateTargetPixels(LEAP_HAND hand)
    {
        if (tips_LeapUnits.Any() && extrinsics != null)
        {
            foreach (var tip in tips_LeapUnits)
            {
                var transformed = extrinsics.Value.MultiplyPoint(tip.position);

                // The point falls behind the image plane that don't need to be projected
                if (transformed.y <= 0)
                {
                    break;
                }

                transformed = transformed / transformed.y;

                // We're swapping co-ordinate systems from Leap to OpenCV space here
                Vector3 v_in_opencv_normalised = new Vector3(-transformed.x, transformed.z, 1.0f);

                /* here we are applying the following pipeline:
                  1) transform hand points from their (world) coordinate system to the camera
                  coordinates. 2) Perform homogenous division (by v.y) to transform points to
                  rectilinear rays. 3) Call LeapRectilinearToPixel which handles projection model
                  details to transform rays to points on distorted Pixel image.

                  LeapRectilinearToPixel expects points in OpenCV space, and returns the pixel
                  co-ords in OpenCV space
                */

                Vector3 pixel = leapController.RectilinearToPixel(targetCamera, v_in_opencv_normalised);

                tipPixels.Add((new Vector2(pixel.x, pixel.y), tip.fingerName, tip.hand));
            }
        }
    }

    //private IEnumerable<(Vector3 position, FingerTipName fingerName)> GetFingerTips()
    //{ 
    //    tips.Clear();
        
    //    if (leapProvider != null)
    //    {
    //        //UpdateFingerTips(leapProvider.GetHand(Chirality.Left));
    //        UpdateFingerTips(leapProvider.GetHand(Chirality.Right));
    //    }

    //    return tips;

    //    void UpdateFingerTips(Hand hand)
    //    {
    //        bool first = true;
    //        if (hand != null)
    //        {
    //            tips.Add((UnityToLeap(hand.PalmPosition, first),));

    //            foreach (var finger in hand.Fingers)
    //            {
                    
    //                //tips.Add(UnityToLeap(finger.TipPosition, first));
    //                first = false;
    //            }
    //        }
    //    }
    //}

    private Vector3 UnityToLeap(Vector3 tipPosition, bool first = false)
    {
        string s = tipPosition.ToString();
        //return new Vector3(-tipPosition.x * M_TO_MM, - tipPosition.y * M_TO_MM, tipPosition.z * M_TO_MM);
        //unityToLeapNonXRTransform.TransformPoint(tipPosition);

        s += " > " + tipPosition.ToString();
        if (first) 
            Debug.Log(s);

        return tipPosition;
    }
}