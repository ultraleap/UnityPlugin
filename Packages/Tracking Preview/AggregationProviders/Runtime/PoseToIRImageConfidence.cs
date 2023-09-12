using Leap;
using Leap.Unity;
using LeapInternal;
using PlasticGui.WorkspaceWindow.Home.Repositories;
using PlasticGui.WorkspaceWindow.PendingChanges.Changelists;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using UnityEditor.MemoryProfiler;
using UnityEngine;
using static UnityEngine.Rendering.VirtualTexturing.Debugging;
using Vector2 = UnityEngine.Vector2;
using Vector3 = UnityEngine.Vector3;

public class PoseToIRImageConfidence : MonoBehaviour
{
    [SerializeField]
    public LeapImageRetriever imageRetriever;

    [SerializeField]
    public LeapServiceProvider leapProvider;

    public Texture2D IRImageWithOverlaidPosePoints;

    private List<Vector3> tips = new List<Vector3>();
    private UnityEngine.Matrix4x4? extrinsics;
    private Controller leapController;
    private const Image.CameraType targetCamera = Image.CameraType.LEFT;
    private List<Vector2> tipPixels = new List<Vector2>();

    private bool newImage = false;
    private Image image;
    private Color32[] imagePixels;

    private LeapTransform unityToLeapNonXRTransform;
    public static readonly float M_TO_MM = 1000;



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

    private IEnumerable<Vector3> UpdateFingerTipsRawForHand(LEAP_HAND hand)
    {
        if (leapProvider != null)
        {
            tips.Add(AsVector3(hand.pinky.distal.next_joint));
            tips.Add(AsVector3(hand.ring.distal.next_joint));
            tips.Add(AsVector3(hand.middle.distal.next_joint));
            tips.Add(AsVector3(hand.index.distal.next_joint));
            tips.Add(AsVector3(hand.thumb.distal.next_joint));
        }

        return tips;

        Vector3 AsVector3(LEAP_VECTOR v)
        {
            return new Vector3(v.x, v.y, v.z);
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
                imagePixels[i].r = data[i];
                imagePixels[i].g = data[i];
                imagePixels[i].b = data[i];
                imagePixels[i].a = (byte)128;
            }

            IRImageWithOverlaidPosePoints.SetPixels32(imagePixels);
            

            foreach (var pixel in tipPixels)
            {
                for (int x = Math.Max(0, (int)pixel.x - 1); x < Math.Min(image.Width, (int)pixel.x + 1); x++)
                {
                    for (int y = Math.Max(0, (int)pixel.y - 1); y < Math.Min(image.Height, (int)pixel.y + 1); y++)
                    {
                        IRImageWithOverlaidPosePoints.SetPixel(x, y, Color.red);
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
                var transformed = extrinsics.Value.MultiplyPoint(tip);

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

                tipPixels.Add(new Vector2(pixel.x, pixel.y));
            }
        }

        
    }

    private IEnumerable<Vector3> GetFingerTips()
    { 
        tips.Clear();
        
        if (leapProvider != null)
        {
            //UpdateFingerTips(leapProvider.GetHand(Chirality.Left));
            UpdateFingerTips(leapProvider.GetHand(Chirality.Right));
        }

        return tips;

        void UpdateFingerTips(Hand hand)
        {
            bool first = true;
            if (hand != null)
            {
                tips.Add(UnityToLeap(hand.PalmPosition, first));

                foreach (var finger in hand.Fingers)
                {
                    
                    //tips.Add(UnityToLeap(finger.TipPosition, first));
                    first = false;
                }
            }
        }
    }

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