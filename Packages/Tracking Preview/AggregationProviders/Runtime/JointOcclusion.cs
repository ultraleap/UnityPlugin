//#define LOGGING_RENDER_COLOURS

using Leap;
using Leap.Unity;
using Leap.Unity.Encoding;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

/// <summary>
/// class to calculate confidence values based on joint occlusion.
/// </summary>
[RequireComponent(typeof(Camera))]
public class JointOcclusion : MonoBehaviour
{
    public Shader replacementShader;
    public CapsuleHand occlusionHandLeft;
    public CapsuleHand occlusionHandRight;

    Camera cam;
    Texture2D tex;
    Rect regionToReadFrom;

    // Colours used for the capsule hand joint spheres
    Color[] occlusionSphereColorsLeft;
    Color[] occlusionSphereColorsRight;


    /// <summary>
    /// For some reason the capsule hand colours that are rendered to the occlusion camera are not the original colours, but appear darker
    /// This set is based on the colours as viewed in the render texture. The ComputMajorColour codeis used to log out the colours 
    /// found when looking for joints, when a hand is placed so all joints can be seen
    /// </summary>
    Color[] shiftedOcclusionSphereColorsLeft =
    {
         new Color(1, 0, 0, 1),
         new Color(0.9294118f, 0.003921569f, 0, 1),
         new Color(0.8627452f, 0.003921569f, 0, 1),
         new Color(0.8000001f, 0.007843138f, 0, 1),
         new Color(0.7372549f, 0.01568628f, 0, 1),
         new Color(0.6784314f, 0.01960784f, 0, 1),
         new Color(0.6235294f, 0.02745098f, 0, 1),
         new Color(0.572549f, 0.03921569f, 0, 1),
         new Color(0.0509804f, 0.5215687f, 0, 1),
         new Color(0.4745098f, 0.0627451f, 0, 1),
         new Color(0.4313726f, 0.07843138f, 0, 1),
         new Color(0.3882353f, 0.09803922f, 0, 1),
         new Color(0.0509804f, 0.5215687f, 0, 1),
         new Color(0.3098039f, 0.1372549f, 0, 1),
         new Color(0.2745098f, 0.1607843f, 0, 1),
         new Color(0.2431373f, 0.1843137f, 0, 1),
         new Color(0.2156863f, 0.2156863f, 0, 1),
         new Color(0.1843137f, 0.2431373f, 0, 1),
         new Color(0.1607843f, 0.2745098f, 0, 1),
         new Color(0.1372549f, 0.3098039f, 0, 1)
    };

    /// <summary>
    /// For some reason the capsule hand colours that are rendered to the occlusion camera are not the original colours, but appear darker
    /// This set is based on the colours as viewed in the render texture. The ComputMajorColour codeis used to log out the colours 
    /// found when looking for joints, when a hand is placed so all joints can be seen
    /// </summary>
    Color[] shiftedOcclusionSphereColorsRight =
    {
         new Color(1f, 0.8313726f, 0f, 1f),
         new Color(0.9294118f, 0.7725491f, 0.003921569f, 1f),
         new Color(0.8627452f, 0.7176471f, 0.007843138f, 1f),
         new Color(0.8000001f, 0.6666667f, 0.01176471f, 1f),
         new Color(0.7372549f, 0.6156863f, 0.01568628f, 1f),
         new Color(0.6784314f, 0.5647059f, 0.02352941f, 1f),
         new Color(0.6235294f, 0.5215687f, 0.03137255f, 1f),
         new Color(0.572549f, 0.4784314f, 0.04313726f, 1f),
         new Color(0.0509804f, 0.04313726f, 0.5294118f, 1f),
         new Color(0.4745098f, 0.3960785f, 0.07058824f, 1f),
         new Color(0.4313726f, 0.3607843f, 0.08627451f, 1f),
         new Color(0.3882353f, 0.3254902f, 0.1019608f, 1f),
         new Color(0.3490196f, 0.2901961f, 0.1215686f, 1f),
         new Color(0.3098039f, 0.2588235f, 0.145098f, 1f),
         new Color(0.2745098f, 0.2313726f, 0.1686275f, 1f),
         new Color(0.2431373f, 0.2039216f, 0.1921569f, 1f),
         new Color(0.2156863f, 0.1803922f, 0.2196079f, 1f),
         new Color(0.1843137f, 0.1568628f, 0.2509804f, 1f),
         new Color(0.1607843f, 0.1333333f, 0.282353f, 1f),
         new Color(0.1372549f, 0.1137255f, 0.3176471f, 1f),
    };

    Mesh cubeMesh;
    Material cubeMaterial;
    string layerName;

    private bool setup = false;
    public void Update()
    {
        if (!setup)
        {
            Setup();
        }
    }

    /// <summary>
    /// this sets everything up, so that joint occlusion works (eg. rendering layers)
    /// </summary>
    public void Setup()
    {
        List<JointOcclusion> allJointOcclusions = FindObjectsOfType<JointOcclusion>().ToList();
        layerName = "JointOcclusion" + allJointOcclusions.IndexOf(this).ToString();

        cam = GetComponent<Camera>();
        cam.SetReplacementShader(replacementShader, "RenderType");
        cam.cullingMask = LayerMask.GetMask(layerName);

        // remove the joint occlusion layer from the main camera:
        Camera.main.cullingMask &= ~(1 << LayerMask.NameToLayer(layerName));

        cam.targetTexture = new RenderTexture(cam.targetTexture);

        tex = new Texture2D(cam.targetTexture.width, cam.targetTexture.height);
        regionToReadFrom = new Rect(0, 0, cam.targetTexture.width, cam.targetTexture.height);

        occlusionHandLeft.gameObject.layer = LayerMask.NameToLayer(layerName);
        occlusionHandLeft.SetIndividualSphereColors = true;
        occlusionHandRight.gameObject.layer = LayerMask.NameToLayer(layerName);
        occlusionHandRight.SetIndividualSphereColors = true;

        occlusionSphereColorsLeft = occlusionHandLeft.SphereColors;
        occlusionSphereColorsRight = occlusionHandRight.SphereColors;

        for (int i = 0; i < occlusionSphereColorsLeft.Length; i++)
        {
            occlusionSphereColorsLeft[i] = Color.Lerp(Color.red, Color.green, (float)i / occlusionSphereColorsLeft.Length);
            occlusionSphereColorsRight[i] = Color.Lerp(Color.yellow, Color.blue, (float)i / occlusionSphereColorsRight.Length);
        }

        occlusionHandLeft.SphereColors = occlusionSphereColorsLeft;
        occlusionHandRight.SphereColors = occlusionSphereColorsRight;

        // Create a cube mesh that is used to block out the majority of the palm, so it blocks the view of the finger joints
        cubeMesh = createCubeMesh();
        cubeMaterial = new Material(Shader.Find("Standard"));

        setup = true;
    }

    private Mesh createCubeMesh()
    {
        Mesh cubeMesh = new Mesh();

        Vector3[] vertices = {
            new Vector3 (0, 0, 0),
            new Vector3 (0.5f, 0, 0),
            new Vector3 (0.5f, 0.5f, 0),
            new Vector3 (0, 0.5f, 0),
            new Vector3 (0, 0.5f, 0.5f),
            new Vector3 (0.5f, 0.5f, 0.5f),
            new Vector3 (0.5f, 0, 0.5f),
            new Vector3 (0, 0, 0.5f),
        };

        int[] triangles = {
         0, 2, 1, //face front
         0, 3, 2,
         2, 3, 4, //face top
         2, 4, 5,
         1, 2, 5, //face right
         1, 5, 6,
         0, 7, 4, //face left
         0, 4, 3,
         5, 4, 7, //face back
         5, 7, 6,
         0, 6, 7, //face bottom
         0, 1, 6
         };

        cubeMesh.vertices = vertices;
        cubeMesh.triangles = triangles;
        cubeMesh.RecalculateNormals();

        return cubeMesh;
    }

    /// <summary>
    /// return an array of joint confidences that is determined by joint occlusion.
    /// It uses a capsule hand rendered on a camera sitting at the deviceOrigin.
    /// Note that as the capsule hand doesn't have metacarpal bones, their corresponding confidence will be zero)
    /// </summary>
    public float[] Confidence_JointOcclusion(float[] confidences, Transform deviceOrigin, Hand hand)
    {
        if (confidences == null)
        {
            confidences = new float[VectorHand.NUM_JOINT_POSITIONS];
        }

        if (hand == null)
        {
            return Leap.Unity.Utils.Fill(confidences, 0);
        }

        // draw a cube where the palm is, so that joints cannot be seen 'through' the palm
        // the following values are determined by experimenting with different cube sizes, positions and rotations and picking one 
        // that fills out the palm of the capsule hand well.
        Vector3 posOffset = new Vector3(-0.03f, 0.005f, -0.045f);
        Quaternion rotOffset = Quaternion.Euler(-5.366f, 0, 0);
        Vector3 scale = new Vector3(0.1f, 0.01f, 0.13f);
        Graphics.DrawMesh(cubeMesh, Matrix4x4.TRS(hand.PalmPosition + hand.Direction * posOffset.z + hand.PalmNormal * posOffset.y + Vector3.Cross(hand.Direction, hand.PalmNormal) * posOffset.x, hand.Rotation * rotOffset, scale), cubeMaterial, LayerMask.NameToLayer(layerName));

        RenderTexture.active = cam.targetTexture;

        tex.ReadPixels(regionToReadFrom, 0, 0);
        tex.Apply();

        // loop through all joints that are visible (all joints that are rendered on a capsule hand),
        // and save how many pixels of a joint can be seen (in pixelsSeenCount)
        // and how many pixels of a joint would be seen if the joint was not occluded at all (in optimalPixelsCount)
        int[] pixelsSeenCount = new int[confidences.Length];
        int[] optimalPixelsCount = new int[confidences.Length];

        Color[] majorColour = new Color[occlusionSphereColorsLeft.Length];

        foreach (var finger in hand.Fingers)
        {
            for (int j = 0; j < 4; j++)
            {
                // as the capsule hands doesn't render metacarpal bones, the indexing of capsule hand colors is different
                // from the indexing of the jointPositions on a VectorHand (which is used for confidence indexing)
                int key = (int)finger.Type * 5 + j + 1;
                int capsuleHandKey = (int)finger.Type * 4 + j;

                float jointRadius = 0.008f;

                // get the joint position from the given hand and use it to calculate the screen position of the joint's center and 
                // a point on the outside border of the joint (both in pixel coordinates)
                Vector3 jointPos = finger.Bone((Leap.Bone.BoneType)j).NextJoint;
                Vector3 screenPosCenter = cam.WorldToScreenPoint(jointPos);
                Vector3 screenPosSphereOutside = cam.WorldToScreenPoint(jointPos + cam.transform.right * jointRadius);

                // the sphere radius (in pixels) is given by the distance between the screenPosCenter and the screenPosOutside
                float radius = new Vector2(screenPosSphereOutside.x - screenPosCenter.x, screenPosSphereOutside.y - screenPosCenter.y).magnitude;
                optimalPixelsCount[key] = (int)(Mathf.PI * radius * radius);

                // only count pixels around where the sphere is supposed to be (+5 pixel margin)
                int margin = 6;
                int x0 = Mathf.Clamp((int)(screenPosCenter.x - radius - margin), 0, tex.width);
                int y0 = Mathf.Clamp((int)(screenPosCenter.y - radius - margin), 0, tex.height);
                int width = Mathf.Clamp((int)(screenPosCenter.x + radius + margin), 0, tex.width) - x0;
                int height = Mathf.Clamp((int)(screenPosCenter.y + radius + margin), 0, tex.height) - y0;

                Color[] tempPixels = tex.GetPixels(x0, y0, width, height);

                if (hand.IsLeft)
                {
                    pixelsSeenCount[key] = tempPixels.Where(x => DistanceBetweenColors(x, shiftedOcclusionSphereColorsLeft[capsuleHandKey]) < 0.01f).Count();

#if LOGGING_RENDER_COLOURS
                    majorColour[capsuleHandKey] = FindDominantColourInTarget(tempPixels);

                    Debug.Log($"{capsuleHandKey} Original sphere colour {occlusionSphereColorsLeft[capsuleHandKey].r},{occlusionSphereColorsLeft[capsuleHandKey].g},{occlusionSphereColorsLeft[capsuleHandKey].b}, {occlusionSphereColorsLeft[capsuleHandKey].a} " +
                        $"Dominant colour actually found {majorColour[capsuleHandKey].r}, {majorColour[capsuleHandKey].g}, {majorColour[capsuleHandKey].b}, {majorColour[capsuleHandKey].a} " +
                        $"Difference {DistanceBetweenColors(majorColour[capsuleHandKey], occlusionSphereColorsLeft[capsuleHandKey])}");
#endif 
                }
                else
                {
                    pixelsSeenCount[key] = tempPixels.Where(x => DistanceBetweenColors(x, shiftedOcclusionSphereColorsRight[capsuleHandKey]) < 0.01f).Count();

#if LOGGING_RENDER_COLOURS
                    majorColour[capsuleHandKey] = FindDominantColourInTarget(tempPixels);
                    Debug.Log($"{capsuleHandKey}  Original sphere colour {occlusionSphereColorsLeft[capsuleHandKey].r},{occlusionSphereColorsLeft[capsuleHandKey].g},{occlusionSphereColorsLeft[capsuleHandKey].b}, {occlusionSphereColorsLeft[capsuleHandKey].a} " +
                        $"Dominant colour actually found {majorColour[capsuleHandKey].r}, {majorColour[capsuleHandKey].g}, {majorColour[capsuleHandKey].b}, {majorColour[capsuleHandKey].a} " +
                        $"Difference {DistanceBetweenColors(majorColour[capsuleHandKey], occlusionSphereColorsLeft[capsuleHandKey])}");
#endif                 
                }
            }
        }

        for (int i = 0; i < confidences.Length; i++)
        {
            if (optimalPixelsCount[i] != 0)
            {
                confidences[i] = (float)pixelsSeenCount[i] / optimalPixelsCount[i];
            }
        }

        return confidences;
    }

    Color FindDominantColourInTarget(Color[] targetPixelRegion)
    {
        Dictionary<Color, int> colourStats = new Dictionary<Color, int>();

        // Can get both black defined as 0,0,0,0 and 0,0,0,1
        Color black = new Color(0, 0, 0, 0);

        foreach (Color pixel in targetPixelRegion) 
        {
            // Screen out white and black pixel(s)
            if (pixel != Color.white && pixel != black && pixel != Color.black)
            {
                if (colourStats.ContainsKey(pixel))
                {
                    colourStats[pixel] = colourStats[pixel] + 1;
                }
                else
                {
                    colourStats.Add(pixel, 1);
                }
            }
        }

        int maxCount = 0;
        Color mostCommonColor = Color.black;

        foreach (Color color in colourStats.Keys) 
        {
            if (colourStats[color] > maxCount)
            {
                maxCount = colourStats[color];
                mostCommonColor = color;
            }
        }

        return mostCommonColor;   
    }

    float DistanceBetweenColors(Color color1, Color color2)
    {
        Color colorDifference = color1 - color2;
        Vector3 diffVetor = new Vector3(colorDifference.r, colorDifference.g, colorDifference.b);
        return diffVetor.magnitude;
    }
}