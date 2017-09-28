using UnityEngine;
using Leap.Unity;

public class RawTextureVisualizer : MonoBehaviour {
  public LeapImageRetriever imageRetriever;
	void Update () {
    if (imageRetriever.TextureData != null && GetComponent<Renderer>().sharedMaterial.mainTexture != imageRetriever.TextureData.RawTexture.CombinedTexture) {
      GetComponent<Renderer>().sharedMaterial.mainTexture = imageRetriever.TextureData.RawTexture.CombinedTexture;
    }
  }
}
