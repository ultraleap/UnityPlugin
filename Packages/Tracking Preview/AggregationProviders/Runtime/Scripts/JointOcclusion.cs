using Leap;
using Leap.Unity;
using Leap.Unity.Encoding;
using System.Collections;
using System.Collections.Generic;
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
    Color[] occlusionSphereColorsLeft;
    Color[] occlusionSphereColorsRight;
    Mesh cubeMesh;
    Material cubeMaterial;
    string layerName;

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



        cubeMesh = createCubeMesh();
        cubeMaterial = new Material(Shader.Find("Standard"));

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
                int margin = 5;
                int x0 = Mathf.Clamp((int)(screenPosCenter.x - radius - margin), 0, tex.width);
                int y0 = Mathf.Clamp((int)(screenPosCenter.y - radius - margin), 0, tex.height);
                int width = Mathf.Clamp((int)(screenPosCenter.x + radius + margin), 0, tex.width) - x0;
                int height = Mathf.Clamp((int)(screenPosCenter.y + radius + margin), 0, tex.height) - y0;

                Color[] tempPixels = tex.GetPixels(x0, y0, width, height);

                if (hand.IsLeft)
                {
                    pixelsSeenCount[key] = tempPixels.Where(x => DistanceBetweenColors(x, occlusionSphereColorsLeft[capsuleHandKey]) < 0.01f).Count();
                }
                else
                {
                    pixelsSeenCount[key] = tempPixels.Where(x => DistanceBetweenColors(x, occlusionSphereColorsRight[capsuleHandKey]) < 0.01f).Count();
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

    float DistanceBetweenColors(Color color1, Color color2)
    {
        Color colorDifference = color1 - color2;
        Vector3 diffVetor = new Vector3(colorDifference.r, colorDifference.g, colorDifference.b);
        return diffVetor.magnitude;
    }
}