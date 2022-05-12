using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Leap.Unity;
using System.Linq;
//using Leap.Unity.Encoding;

public class JointOcclusion : MonoBehaviour
{
    //public RenderTexture texture;
    public Shader replacementShader;
    public Texture2D tex;
    public CapsuleHand occlusionHand;

    Camera camera;

    Rect regionToReadFrom;
    Color[] occlusionSphereColors;

    // Start is called before the first frame update
    void Start()
    {
        camera = GetComponent<Camera>();
        camera.SetReplacementShader(replacementShader, "RenderType");
        camera.cullingMask = LayerMask.GetMask("JointOcclusion");

        tex = new Texture2D(camera.targetTexture.width, camera.targetTexture.height);
        regionToReadFrom = new Rect(0, 0, camera.targetTexture.width, camera.targetTexture.height);

        capsuleHand.SetIndividualSphereColors = true;
        occlusionHand.SetIndividualSphereColors = true;

        sphereColors = capsuleHand.SphereColors;
        occlusionSphereColors = occlusionHand.SphereColors;

        for (int i = 0; i < occlusionSphereColors.Length; i++)
        {
            occlusionSphereColors[i] = Color.Lerp(Color.red, Color.green, (float)i / occlusionSphereColors.Length);
        }
        occlusionHand.SphereColors = occlusionSphereColors;
    }

    public float[] Confidence_JointOcclusion(Hand hand, float[] confidences)
    {
        if (hand == null)
        {
            return;
        }

        camera.Render();
        RenderTexture.active = camera.targetTexture;

        tex.ReadPixels(regionToReadFrom, 0, 0);
        tex.Apply();


        int[] pixelCounts = new int[occlusionSphereColors.Length];

        var pixels = tex.GetPixels();
        for (int i = 0; i < occlusionSphereColors.Length; i++)
        {
            pixelCounts[i] = pixels.Where(x => DistanceBetweenColors(x, occlusionSphereColors[i]) < 0.01f).Count();
        }

        float[] jointDistances = new float[pixelCounts.Length];
        int[] jointPixelCount = new int[occlusionSphereColors.Length];
        foreach (var finger in hand.Fingers)
        {
            for (int j = 0; j < 4; j++)
            {
                //jointDistances[(int)finger.Type * 4 + j]
                float jointDistance = capsuleHand.leapProvider.transform.InverseTransformPoint(finger.Bone((Leap.Bone.BoneType)j).NextJoint.ToVector3()).y;
                //float jointDistance = Vector3.Distance(finger.Bone((Leap.Bone.BoneType)j).NextJoint.ToVector3(), capsuleHand.leapProvider.transform.position);


                // get projected sphere radius
                float jointRadius = 0.008f;
                float radius = 1f / Mathf.Tan(Mathf.Deg2Rad * camera.fieldOfView) * jointRadius / Mathf.Sqrt(jointDistance * jointDistance - jointRadius * jointRadius);

                //if ((int)finger.Type * 4 + j == 3) Debug.Log(Mathf.PI * radius * radius * tex.height / 2 * jointDistance * jointDistance * 1000f);
                jointPixelCount[(int)finger.Type * 4 + j] = (int)(Mathf.PI * radius * radius * tex.height / 2 * 1500f);
            }
        }

        for (int i = 0; i < sphereColors.Length; i++)
        {
            sphereColors[i] = Color.Lerp(Color.black, Color.white, (float)pixelCounts[i] / jointPixelCount[i]);
        }
        capsuleHand.SphereColors = sphereColors;

        Debug.Log(pixelCounts[3] + ", " + jointPixelCount[3] + ", " + ((float)pixelCounts[3] / jointPixelCount[3]));

        return confidences;
    }

    // Update is called once per frame
    void Update()
    {
        //if (Input.GetKeyDown(KeyCode.Space))
        //{
        //float startTime = Time.time;


        camera.Render();
        RenderTexture.active = camera.targetTexture;

        tex.ReadPixels(regionToReadFrom, 0, 0);
        tex.Apply();



        Leap.Hand hand = capsuleHand.leapProvider.CurrentFrame.GetHand(Chirality.Left);

        if (hand == null) return;

        int[] pixelCounts = new int[occlusionSphereColors.Length];

        var pixels = tex.GetPixels();
        for (int i = 0; i < occlusionSphereColors.Length; i++)
        {
            pixelCounts[i] = pixels.Where(x => DistanceBetweenColors(x, occlusionSphereColors[i]) < 0.01f).Count();
        }

        float[] jointDistances = new float[pixelCounts.Length];
        int[] jointPixelCount = new int[occlusionSphereColors.Length];
        foreach (var finger in hand.Fingers)
        {
            for (int j = 0; j < 4; j++)
            {
                //jointDistances[(int)finger.Type * 4 + j]
                float jointDistance = capsuleHand.leapProvider.transform.InverseTransformPoint(finger.Bone((Leap.Bone.BoneType)j).NextJoint.ToVector3()).y;
                //float jointDistance = Vector3.Distance(finger.Bone((Leap.Bone.BoneType)j).NextJoint.ToVector3(), capsuleHand.leapProvider.transform.position);


                // get projected sphere radius
                float jointRadius = 0.008f;
                float radius = 1f / Mathf.Tan(Mathf.Deg2Rad * camera.fieldOfView) * jointRadius / Mathf.Sqrt(jointDistance * jointDistance - jointRadius * jointRadius);

                //if ((int)finger.Type * 4 + j == 3) Debug.Log(Mathf.PI * radius * radius * tex.height / 2 * jointDistance * jointDistance * 1000f);
                jointPixelCount[(int)finger.Type * 4 + j] = (int)(Mathf.PI * radius * radius * tex.height / 2 * 1500f);
            }
        }

        for (int i = 0; i < sphereColors.Length; i++)
        {
            sphereColors[i] = Color.Lerp(Color.black, Color.white, (float)pixelCounts[i] / jointPixelCount[i]);
        }
        capsuleHand.SphereColors = sphereColors;

        Debug.Log(pixelCounts[3] + ", " + jointPixelCount[3] + ", " + ((float)pixelCounts[3] / jointPixelCount[3]));

        //Texture2D[] singleJointTextures = new Texture2D[26];
        //float[] percentages = new float[26];

        //int[] jointIndices = new int[] {1, 2, 3, 5, 6, 7, 9, 10, 11, 13, 14, 15, 17, 18, 19 };

        //for (int i = 0; i < 26; i++)
        //{

        //    //if (!jointIndices.Contains(i)) continue;

        //    int layerIdx = 6 + i;

        //    capsuleHand.drawSingleSphereToLayer(i, layerIdx); // LayerMask.NameToLayer("JointOcclusion"));

        //    camera.cullingMask = LayerMask.GetMask(LayerMask.LayerToName(layerIdx)); // LayerMask.GetMask("JointOcclusion");

        //    camera.Render();
        //    RenderTexture.active = camera.targetTexture;

        //    Texture2D tempTex = new Texture2D(camera.targetTexture.width, camera.targetTexture.height);
        //    tempTex.ReadPixels(regionToReadFrom, 0, 0);
        //    tempTex.Apply();
        //    singleJointTextures[i] = tempTex;


        //    float percentage = CompareTextures(tex, tempTex);
        //    percentages[i] = percentage;

        //    if (i == 7) Debug.Log(percentage);

        //    sphereColors[i] = Color.Lerp(Color.black, Color.white, percentage);
        //}
        //capsuleHand.SphereColors = sphereColors;

        //camera.cullingMask = -1;

        //debugCube.material.SetTexture("_MainTex", singleJointTextures[0]);
        //debugCube2.material.SetTexture("_MainTex", tex);

        //Debug.Log("Time taken: " + (Time.time - startTime));
        //}
    }

    float DistanceBetweenColors(Color color1, Color color2)
    {
        Color colorDifference = color1 - color2;
        Vector3 diffVetor = new Vector3(colorDifference.r, colorDifference.g, colorDifference.b);
        return diffVetor.magnitude;
    }

}
