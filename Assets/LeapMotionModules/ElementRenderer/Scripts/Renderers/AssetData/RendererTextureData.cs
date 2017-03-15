using System.Collections;
using System.Collections.Generic;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

public class RendererTextureData : ScriptableObject {
  [SerializeField]
  private List<Texture2D> packedTextures = new List<Texture2D>();

  private void OnDestroy() {
    foreach (var tex in packedTextures) {
      DestroyImmediate(tex, allowDestroyingAssets: true);
    }
  }

#if UNITY_EDITOR
  public void Clear() {
    foreach (var texture in packedTextures) {
      DestroyImmediate(texture, allowDestroyingAssets: true);
    }
    packedTextures.Clear();
  }

  public void AssignTextures(Texture2D[] textures) {
    List<Texture2D> newList = new List<Texture2D>();
    newList.AddRange(textures);

    foreach (var tex in packedTextures) {
      if (!newList.Contains(tex)) {
        DestroyImmediate(tex, allowDestroyingAssets: true);
      }
    }

    foreach (var tex in newList) {
      if (!packedTextures.Contains(tex)) {
        AssetDatabase.AddObjectToAsset(tex, this);
      }
    }

    packedTextures = newList;
    AssetDatabase.SaveAssets();
  }
#endif

  public int Count {
    get {
      return packedTextures.Count;
    }
  }

  public Texture2D this[int index] {
    get {
      return packedTextures[index];
    }
  }
}
