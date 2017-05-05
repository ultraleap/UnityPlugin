using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Leap.Unity.GraphicalRenderer;

public class ExampleArray : MonoBehaviour {

  public LeapGraphic template;
  public float radius = 1;
  public float spacing = 1;
  public bool update = false;

  private void OnValidate() {
    if (Application.isPlaying) {
      return;
    }

    if (!update) {
      return;
    }
    update = false;

    for (int i = 0; i < transform.childCount; i++) {
      InternalUtility.Destroy(transform.GetChild(i).gameObject);
    }

    if (spacing <= 0) {
      return;
    }

    float edge = spacing * ((int)(radius / spacing) + 1);
    for (float dx = -edge; dx <= edge; dx += spacing) {
      for (float dy = -edge; dy <= edge; dy += spacing) {
        if (new Vector2(dx, dy).magnitude < radius) {
          var graphic = Instantiate(template);

          graphic.transform.SetParent(transform);
          graphic.transform.localPosition = new Vector3(dx, dy, 0);
          graphic.gameObject.SetActive(true);
        }
      }
    }
  }

  private List<Vector3> _originalPositions = new List<Vector3>();

  private void Start() {
    for (int i = 0; i < transform.childCount; i++) {
      var graphic = transform.GetChild(i).GetComponent<LeapGraphic>();
      _originalPositions.Add(graphic.transform.localPosition);
    }
  }

  private void Update() {
    Random.InitState(0);
    for (int i = 0; i < transform.childCount; i++) {
      var graphic = transform.GetChild(i).GetComponent<LeapGraphic>();

      float offset = graphic.transform.localPosition.magnitude;

      Vector3 axis = _originalPositions[i].normalized;
      graphic.transform.localPosition = _originalPositions[i] + axis * Mathf.PerlinNoise(offset * 1.235f + Time.time * 0.01f, offset * 2.1231234f + Time.time * 0.01f) * 0.5f;
    }
  }


}
