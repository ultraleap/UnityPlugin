using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ChooseButtonColorOnAwake : MonoBehaviour {

  public Renderer buttonColorRenderer;

  void Awake() {
    Material matInstance = buttonColorRenderer.material;
    matInstance.color = Random.ColorHSV(0F, 1F, 0.7F, 0.6F, 1F, 0.6F, 1F, 1F);
  }

}
