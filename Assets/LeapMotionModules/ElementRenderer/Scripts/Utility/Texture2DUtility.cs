using System.Collections.Generic;
using UnityEngine;

public static class Texture2DUtility {

  public static List<TextureFormat> readWriteFormats = new List<TextureFormat>() { 
    TextureFormat.ARGB32,
    TextureFormat.RGBA32,
    TextureFormat.RGB24,
    TextureFormat.Alpha8,
    TextureFormat.RGBAHalf,
    TextureFormat.RGFloat,
    TextureFormat.RHalf,
    TextureFormat.RGBAFloat,
    TextureFormat.RGFloat,
    TextureFormat.RFloat
  };

  public static void AddBorder(this Texture2D texture, int pixelAmount) {
    if (pixelAmount <= 0) return;

    texture.EnsureReadWriteEnabled();
    Color[] colors = texture.GetPixels();

    int originalWidth = texture.width;
    int originalHeight = texture.height;
    int newWidth = originalWidth + pixelAmount * 2;
    int newHeight = originalHeight + pixelAmount * 2;
    texture.Resize(newWidth, newHeight);

    texture.SetPixels(pixelAmount, pixelAmount, originalWidth, originalHeight, colors);

    if (texture.wrapMode == TextureWrapMode.Clamp) {

      //TODO: refactor this mess
      for (int x = 0; x < newWidth; x++) {
        for (int dy = 0; dy < pixelAmount; dy++) {
          int ix = Mathf.Clamp(x - pixelAmount, 0, originalWidth - 1) + pixelAmount;

          texture.SetPixel(x, dy, texture.GetPixel(ix, pixelAmount));
          texture.SetPixel(x, newHeight - dy - 1, texture.GetPixel(ix, newHeight - pixelAmount - 1));
        }
      }

      for (int y = 0; y < newHeight; y++) {
        for (int dx = 0; dx < pixelAmount; dx++) {
          int iy = Mathf.Clamp(y - pixelAmount, 0, originalHeight - 1) + pixelAmount;

          texture.SetPixel(dx, y, texture.GetPixel(pixelAmount, iy));
          texture.SetPixel(newWidth - dx - 1, y, texture.GetPixel(newWidth - pixelAmount - 1, iy));
        }
      }
    } else {
      throw new System.NotImplementedException();
    }

    texture.Apply();
  }

}
