using Codice.CM.SEIDInfo;
using Leap;
using Leap.Unity;
using LeapInternal;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Vector2 = UnityEngine.Vector2;
using Vector3 = UnityEngine.Vector3;

public class TipConfidences
{
    public HandTipConfidences LeftConfidence = new HandTipConfidences();
    public HandTipConfidences RightConfidence = new HandTipConfidences();
}

public class HandTipConfidences
{
    public (float confidence, Vector3 lastGoodPosition_LeapSpace, Vector3 lastGoodPosition_PixelSpace) ThumbTip;
    public (float confidence, Vector3 lastGoodPosition_LeapSpace, Vector3 lastGoodPosition_PixelSpace) IndexTip;
    public (float confidence, Vector3 lastGoodPosition_LeapSpace, Vector3 lastGoodPosition_PixelSpace) MiddleTip;
    public (float confidence, Vector3 lastGoodPosition_LeapSpace, Vector3 lastGoodPosition_PixelSpace) RingTip;
    public (float confidence, Vector3 lastGoodPosition_LeapSpace, Vector3 lastGoodPosition_PixelSpace) PinkyTip;
}

public enum FingerTipName
{
    Thumb = 0,
    IndexFinger = 1,
    MiddleFinger = 2,
    RingFinger = 3,
    PinkyFinger = 4
}

public class PoseToIRImageConfidence : MonoBehaviour
{
    [SerializeField]
    public LeapImageRetriever imageRetriever;

    [SerializeField]
    public LeapServiceProvider leapProvider;

    [SerializeField]
    [Range(0f, 255f)]
    public int IRThreshold = 64;

    [SerializeField]
    [Range(0f, 255f)]
    public int LowerConfidencePixelThreshold = 64;

    [SerializeField]
    [Range(0f, 255f)]
    public int UpperConfidencePixelThreshold = 90;

    [SerializeField]
    [Range(0f, 255f)]
    public int LastKnownGoodPositionConfidencePixelThreshold = 90;

    [SerializeField]
    [Range(0f, 10f)]
    public float TipOffset_mm = 0.55f;

    public Texture2D IRImageWithOverlaidPosePoints;

    private List<(Vector3 position, FingerTipName fingerName, Chirality hand)> tips = new List<(Vector3, FingerTipName, Chirality)>();
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
        tips.Clear();
        tipPixels.Clear();

        if (e.HasLeftHand)
            UpdateFingerTipsRaw(e.LeftHand);

        if (e.HasRightHand)
            UpdateFingerTipsRaw(e.RightHand);
    }

    private void UpdateFingerTipsRaw(LEAP_HAND hand)
    {
        if (leapController != null && leapController.Devices.Count == 1 && image != null && newImage)
        {
            if (extrinsics == null)
            {
                extrinsics = leapController.LeapExtrinsicCameraMatrix(targetCamera, leapController.Devices[0]);
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
            if (TipOffset_mm == 0)
            {
                tips.Add((AsVector3(hand.pinky.distal.next_joint), FingerTipName.PinkyFinger, chirality));
                tips.Add((AsVector3(hand.ring.distal.next_joint), FingerTipName.RingFinger, chirality));
                tips.Add((AsVector3(hand.middle.distal.next_joint), FingerTipName.MiddleFinger, chirality));
                tips.Add((AsVector3(hand.index.distal.next_joint), FingerTipName.IndexFinger, chirality));
                tips.Add((AsVector3(hand.thumb.distal.next_joint), FingerTipName.Thumb, chirality));
            }
            else
            {
                tips.Add((ScaleBackFromDistal(hand.pinky.distal), FingerTipName.PinkyFinger, chirality));
                tips.Add((ScaleBackFromDistal(hand.ring.distal), FingerTipName.RingFinger, chirality));
                tips.Add((ScaleBackFromDistal(hand.middle.distal), FingerTipName.MiddleFinger, chirality));
                tips.Add((ScaleBackFromDistal(hand.index.distal), FingerTipName.IndexFinger, chirality));
                tips.Add((ScaleBackFromDistal(hand.thumb.distal), FingerTipName.Thumb, chirality));
            }
            
        }

        return tips;

        Vector3 AsVector3(LEAP_VECTOR v)
        {
            return new Vector3(v.x, v.y, v.z);
        }

        Vector3 ScaleBackFromDistal(LEAP_BONE endBone)
        { 
            Vector3 boneV = AsVector3(endBone.prev_joint) - AsVector3(endBone.next_joint);
            boneV.Scale(new Vector3(TipOffset_mm, TipOffset_mm, TipOffset_mm));
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

            if (imagePixels == null && image != null)
            {
                imagePixels = new Color32[image.Width * image.Height];
            }

            byte[] data = image.Data(targetCamera);

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
                Color confidenceColor = Color.Lerp(Color.red, Color.green, confidence);

                for (int x = Math.Max(0, (int)pixel.position.x - 1); x < Math.Min(image.Width, (int)pixel.position.x + 1); x++)
                {
                    for (int y = Math.Max(0, (int)pixel.position.y - 1); y < Math.Min(image.Height, (int)pixel.position.y + 1); y++)
                    {
                        IRImageWithOverlaidPosePoints.SetPixel(x, y, confidenceColor);
                    }
                }

                bool highTipConfidence = averagePixelIntensity > LastKnownGoodPositionConfidencePixelThreshold;
                Vector2? lastKnownGoodPos = null;

                HandTipConfidences handConfidences = TipConfidence.RightConfidence;
                if (pixel.hand == Chirality.Left)
                    handConfidences = TipConfidence.LeftConfidence;

                switch (pixel.fingerName)
                {
                    case FingerTipName.Thumb:
                        handConfidences.ThumbTip.confidence = confidence;
                        if (highTipConfidence)
                            handConfidences.ThumbTip.lastGoodPosition_PixelSpace = pixel.position;
                        else
                            lastKnownGoodPos = handConfidences.ThumbTip.lastGoodPosition_PixelSpace;
                        break;
                    case FingerTipName.IndexFinger:
                        handConfidences.IndexTip.confidence = confidence;
                        if (highTipConfidence)
                            handConfidences.IndexTip.lastGoodPosition_PixelSpace = pixel.position;
                        else
                            lastKnownGoodPos = handConfidences.ThumbTip.lastGoodPosition_PixelSpace;
                        break;
                    case FingerTipName.MiddleFinger:
                        handConfidences.MiddleTip.confidence = confidence;
                        if (highTipConfidence)
                            handConfidences.MiddleTip.lastGoodPosition_PixelSpace = pixel.position;
                        else
                            lastKnownGoodPos = handConfidences.ThumbTip.lastGoodPosition_PixelSpace;
                        break;
                    case FingerTipName.RingFinger:
                        handConfidences.RingTip.confidence = confidence;
                        if (highTipConfidence)
                            handConfidences.RingTip.lastGoodPosition_PixelSpace = pixel.position;
                        else
                            lastKnownGoodPos = handConfidences.ThumbTip.lastGoodPosition_PixelSpace;
                        break;
                    case FingerTipName.PinkyFinger:
                        handConfidences.PinkyTip.confidence = confidence;
                        if (highTipConfidence)
                            handConfidences.PinkyTip.lastGoodPosition_PixelSpace = pixel.position;
                        else
                            lastKnownGoodPos = handConfidences.ThumbTip.lastGoodPosition_PixelSpace;
                        break;

                    default:
                        break;
                }

                if (!highTipConfidence && lastKnownGoodPos.HasValue)
                {
                    for (int x = Math.Max(0, (int)lastKnownGoodPos.Value.x - 1); x < Math.Min(image.Width, (int)lastKnownGoodPos.Value.x + 1); x++)
                    {
                        for (int y = Math.Max(0, (int) lastKnownGoodPos.Value.y - 1); y < Math.Min(image.Height, (int)lastKnownGoodPos.Value.y + 1); y++)
                        {
                            IRImageWithOverlaidPosePoints.SetPixel(x, y, Color.yellow);
                        }
                    }
                }

            }

            IRImageWithOverlaidPosePoints.Apply();
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
        if (tips.Any() && extrinsics != null)
        {
            foreach (var tip in tips)
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