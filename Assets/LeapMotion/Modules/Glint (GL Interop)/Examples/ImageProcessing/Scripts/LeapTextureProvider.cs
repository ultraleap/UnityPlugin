using System;
using UnityEngine;

namespace Leap.Unity.Glint.Examples {
  public class LeapTextureProvider : MonoBehaviour {
    public const int WIDTH = 640;
    public const int HEIGHT = 240;

    public Material leftImageMaterial;
    public Material rightImageMaterial;

    public Material leftTextureMaterial;
    public Material rightTextureMaterial;

    RenderTexture[] leftRenderTexture = new RenderTexture[3];
    RenderTexture[] rightRenderTexture = new RenderTexture[3];

    [HideInInspector]
    public Texture2D leftTexture;
    [HideInInspector]
    public Texture2D rightTexture;

    byte[] leftPixels;
    byte[] rightPixels;

    Action onLeftDataRetrievedAction;
    Action onRightDataRetrievedAction;

    int whichImage = 0;

    // Use this for initialization
    void Start() {
      for (int i = 0; i < 3; i++) {
        leftRenderTexture[i] = new RenderTexture(WIDTH, HEIGHT, 0, RenderTextureFormat.R8, RenderTextureReadWrite.Default);
        leftRenderTexture[i].Create();
      }
      for (int i = 0; i < 3; i++) {
        rightRenderTexture[i] = new RenderTexture(WIDTH, HEIGHT, 0, RenderTextureFormat.R8, RenderTextureReadWrite.Default);
        rightRenderTexture[i].Create();
      }

      leftTexture = new Texture2D(WIDTH, HEIGHT, TextureFormat.R8, false);
      rightTexture = new Texture2D(WIDTH, HEIGHT, TextureFormat.R8, false);

      leftPixels = new byte[WIDTH * HEIGHT];
      rightPixels = new byte[WIDTH * HEIGHT];

      onLeftDataRetrievedAction = onLeftDataRetrieved;
      onRightDataRetrievedAction = onRightDataRetrieved;

      if (leftTextureMaterial != null) {
        leftTextureMaterial.mainTexture = leftTexture;
      }
      if (rightTextureMaterial != null) {
        rightTextureMaterial.mainTexture = rightTexture;
      }
    }

    //This triple-buffer scheme is used to deal with the fact that there is a deterministic three-frame delay
    //between putting images out and receiving them.  We can work-around this by ensuring that there are always
    //three textures in flight at any given moment.
    void Update() {
      if (whichImage == 0) {
        Graphics.Blit(null, leftRenderTexture[0], leftImageMaterial);
        Graphics.Blit(null, rightRenderTexture[0], rightImageMaterial);
        Glint.RequestTextureDownload(leftRenderTexture[0], leftPixels, onLeftDataRetrievedAction);
        Glint.RequestTextureDownload(rightRenderTexture[0], rightPixels, onRightDataRetrievedAction);
        whichImage++;
      } else if (whichImage == 1) {
        Graphics.Blit(null, leftRenderTexture[1], leftImageMaterial);
        Graphics.Blit(null, rightRenderTexture[1], rightImageMaterial);
        Glint.RequestTextureDownload(leftRenderTexture[1], leftPixels, onLeftDataRetrievedAction);
        Glint.RequestTextureDownload(rightRenderTexture[1], rightPixels, onRightDataRetrievedAction);
        whichImage++;
      } else if (whichImage == 2) {
        Graphics.Blit(null, leftRenderTexture[2], leftImageMaterial);
        Graphics.Blit(null, rightRenderTexture[2], rightImageMaterial);
        Glint.RequestTextureDownload(leftRenderTexture[2], leftPixels, onLeftDataRetrievedAction);
        Glint.RequestTextureDownload(rightRenderTexture[2], rightPixels, onRightDataRetrievedAction);
        whichImage = 0;
      }
    }

    // Upon receiving the callback, we look at the received data and fill it into a texture
    // to display on the "CPU" side of the scene.
    private void onLeftDataRetrieved() {
      leftTexture.LoadRawTextureData(leftPixels);
      leftTexture.Apply();
    }
    private void onRightDataRetrieved() {
      rightTexture.LoadRawTextureData(rightPixels);
      rightTexture.Apply();
    }
  }
}
