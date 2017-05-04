/******************************************************************************
 * Copyright (C) Leap Motion, Inc. 2011-2017.                                 *
 * Leap Motion proprietary and  confidential.                                 *
 *                                                                            *
 * Use subject to the terms of the Leap Motion SDK Agreement available at     *
 * https://developer.leapmotion.com/sdk_agreement, or another agreement       *
 * between Leap Motion and you, your company or other organization.           *
 ******************************************************************************/

using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Assertions;
using Leap.Unity.Query;

namespace Leap.Unity.GraphicalRenderer {

  [Serializable]
  public class RendererTextureData {
    [SerializeField]
    private List<NamedTexture> packedTextures = new List<NamedTexture>();

    public void Clear() {
      foreach (var tex in packedTextures) {
        UnityEngine.Object.DestroyImmediate(tex.texture);
      }
      packedTextures.Clear();
    }

    public void AssignTextures(Texture2D[] textures, string[] propertyNames) {
      List<NamedTexture> newList = new List<NamedTexture>();
      Assert.AreEqual(textures.Length, propertyNames.Length);

      for (int i = 0; i < textures.Length; i++) {
        newList.Add(new NamedTexture() {
          propertyName = propertyNames[i],
          texture = textures[i]
        });
      }

      foreach (var tex in packedTextures) {
        if (!newList.Query().Any(p => p.texture == tex.texture)) {
          UnityEngine.Object.DestroyImmediate(tex.texture);
        }
      }

      packedTextures = newList;
    }

    public Texture2D GetTexture(string propertyName) {
      return packedTextures.Query().
                            FirstOrDefault(p => p.propertyName == propertyName).texture;
    }

    public int Count {
      get {
        return packedTextures.Count;
      }
    }

    public void Validate(LeapRenderingMethod renderingMethod) {
      for (int i = packedTextures.Count; i-- != 0;) {
        NamedTexture nt = packedTextures[i];
        Texture2D tex = nt.texture;
        if (tex == null) {
          packedTextures.RemoveAt(i);
          continue;
        }

        renderingMethod.PreventDuplication(ref tex);
        nt.texture = tex;
        packedTextures[i] = nt;
      }
    }

    [Serializable]
    public struct NamedTexture {
      public string propertyName;
      public Texture2D texture;
    }
  }
}
