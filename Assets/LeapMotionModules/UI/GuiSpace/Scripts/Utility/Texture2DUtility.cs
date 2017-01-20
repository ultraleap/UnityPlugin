using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class Texture2DUtility {

  public static void AddBorder(this Texture2D texture, int pixelAmount) {
    Color[] colors = texture.GetPixels();

    int originalWidth = texture.width;
    int originalHeight = texture.height;
    int newWidth = originalWidth + pixelAmount * 2;
    int newHeight = originalHeight + pixelAmount * 2;
    texture.Resize(newWidth, newHeight);

    texture.SetPixels(pixelAmount, pixelAmount, originalWidth, originalHeight, colors);

    if (texture.wrapMode == TextureWrapMode.Clamp) {
      for (int x = 0; x < newWidth; x++) {
        for (int dy = 0; dy < pixelAmount; dy++) {
          int ix = Mathf.Clamp(x - pixelAmount, 0, originalWidth - 1);

          texture.SetPixel(x, dy, texture.GetPixel(ix, pixelAmount));
          texture.SetPixel(x, newHeight - dy, texture.GetPixel(ix, newHeight - pixelAmount));
        }
      }

      for (int y = 0; y < newWidth; y++) {
        for (int dx = 0; dx < pixelAmount; dx++) {
          int iy = Mathf.Clamp(y - pixelAmount, 0, originalHeight - 1);

          texture.SetPixel(dx, y, texture.GetPixel(pixelAmount, iy));
          texture.SetPixel(newHeight - dx, y, texture.GetPixel(newWidth - pixelAmount, iy));
        }
      }
    } else {
      throw new System.NotImplementedException();
    }

    texture.Apply();
  }

}
