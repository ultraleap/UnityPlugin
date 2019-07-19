using UnityEngine;
using Leap.Unity;
public class LeapImageTextureVisualizer : MonoBehaviour {
  public LeapImageRetriever imageRetriever;
  public Renderer meshRenderer;
  public int deviceIndex;
	void Start () {
    if (imageRetriever == null) imageRetriever = FindObjectOfType<LeapImageRetriever>();
    if (meshRenderer == null) meshRenderer = GetComponent<MeshRenderer>();
  }

  void Update() {
    if (imageRetriever.TextureData.TextureData.Count >= deviceIndex) {
      meshRenderer.material.mainTexture = imageRetriever.TextureData.TextureData[deviceIndex - 1].CombinedTexture;
    }
  }
}
