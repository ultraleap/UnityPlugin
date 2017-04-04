using System;
using System.Collections.Generic;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif
using Leap.Unity.Query;

public class RendererTextureData : SceneTiedAsset {
  [SerializeField]
  private List<NamedTexture> packedTextures = new List<NamedTexture>();

#if UNITY_EDITOR
  protected override void OnAssetSaved() {
    base.OnAssetSaved();

    foreach (var pair in packedTextures) {
      if (!AssetDatabase.IsSubAsset(pair.texture)) {
        AssetDatabase.AddObjectToAsset(pair.texture, this);
      }
    }
  }
#endif

  private void OnDestroy() {
    foreach (var tex in packedTextures) {
      DestroyImmediate(tex.texture, allowDestroyingAssets: true);
    }
  }

#if UNITY_EDITOR
  public void Clear() {
    foreach (var tex in packedTextures) {
      DestroyImmediate(tex.texture, allowDestroyingAssets: true);
    }
    packedTextures.Clear();
  }

  public void AssignTextures(Texture2D[] textures, string[] propertyNames) {
    List<NamedTexture> newList = new List<NamedTexture>();
    for (int i = 0; i < textures.Length; i++) {
      newList.Add(new NamedTexture() {
        propertyName = propertyNames[i],
        texture = textures[i]
      });
    }

    foreach (var tex in packedTextures) {
      if (!newList.Query().Any(p => p.texture == tex.texture)) {
        DestroyImmediate(tex.texture, allowDestroyingAssets: true);
      }
    }

    foreach (var pair in newList) {
      if (!packedTextures.Contains(pair) && isSavedAsset) {
        AssetDatabase.AddObjectToAsset(pair.texture, this);
      }
    }

    packedTextures = newList;

    if (isSavedAsset) {
      AssetDatabase.SaveAssets();
    }
  }
#endif

  public Texture2D GetTexture(string propertyName) {
    return packedTextures.Query().
                          FirstOrDefault(p => p.propertyName == propertyName).texture;
  }

  public int Count {
    get {
      return packedTextures.Count;
    }
  }

  [Serializable]
  public struct NamedTexture {
    public string propertyName;
    public Texture2D texture;
  }
}
