/******************************************************************************
 * Copyright (C) Leap Motion, Inc. 2011-2017.                                 *
 * Leap Motion proprietary and  confidential.                                 *
 *                                                                            *
 * Use subject to the terms of the Leap Motion SDK Agreement available at     *
 * https://developer.leapmotion.com/sdk_agreement, or another agreement       *
 * between Leap Motion and you, your company or other organization.           *
 ******************************************************************************/

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Leap.Unity.Interaction {

  public class AnchorScoreTest : MonoBehaviour {

    public Transform centerPositionOneSecondLater;

    public Material testMaterialXY;
    public Material testMaterialXZ;
    public Material testMaterialYZ;

    [Range(0.10F, 0.70F)]
    public float maxRange = 0.30F;

    [Range(5F, 90F)]
    public float maxAngle = 60F;

    [Range(0f, 1f)]
    public float alwaysAttachDistance = 0f;

    [Header("Generated")]
    public Texture2D textureXY;
    public Texture2D textureXZ;
    public Texture2D textureYZ;

    public const int WIDTH = 64;

    void Awake() {
      textureXY = new Texture2D(WIDTH, WIDTH);
      textureXZ = new Texture2D(WIDTH, WIDTH);
      textureYZ = new Texture2D(WIDTH, WIDTH);

      testMaterialXY.mainTexture = textureXY;
      testMaterialXZ.mainTexture = textureXZ;
      testMaterialYZ.mainTexture = textureYZ;
    }

    void Update() {
      this.transform.position = Vector3.zero;

      setPixels(textureXY, Vector3.right, Vector3.up);
      setPixels(textureXZ, Vector3.right, Vector3.forward);
      setPixels(textureYZ, Vector3.up,   -Vector3.forward);
    }

    Color[] _pixels = new Color[WIDTH * WIDTH];

    private void setPixels(Texture2D tex, Vector3 dir1, Vector3 dir2) {
      float widthPerPixel = 0.5F / tex.width;
      float center = widthPerPixel * (tex.width / 2F);

      for (int k = 0; k < tex.width * tex.width; k++) {
        int i = k / tex.width;
        int j = k % tex.width;

        Vector3 anchObjPos = this.transform.position;
        Vector3 anchObjVel = centerPositionOneSecondLater.position - this.transform.position;
        Vector3 anchorPos  = this.transform.position
                           + (dir1 * i * widthPerPixel - (dir1 * center))
                           + (dir2 * j * widthPerPixel - (dir2 * center));

        float score = AnchorableBehaviour.GetAnchorScore(anchObjPos, anchObjVel, anchorPos, maxRange, maxRange * 0.40F, Mathf.Cos(maxAngle * Mathf.Deg2Rad), alwaysAttachDistance);
        Color pixel = Color.HSVToRGB(Mathf.Lerp(1F, 0F, score), Mathf.Lerp(0F, 1F, score), Mathf.Lerp(0F, 1F, score));
        _pixels[i + j * tex.width] = pixel;
      }

      tex.SetPixels(_pixels);
      tex.Apply();
    }

  }

}
