/******************************************************************************
 * Copyright (C) Leap Motion, Inc. 2011-2017.                                 *
 * Leap Motion proprietary and  confidential.                                 *
 *                                                                            *
 * Use subject to the terms of the Leap Motion SDK Agreement available at     *
 * https://developer.leapmotion.com/sdk_agreement, or another agreement       *
 * between Leap Motion and you, your company or other organization.           *
 ******************************************************************************/

using UnityEngine;
using System.Collections;
using Leap.Unity.Attributes;

namespace Leap.Unity {
  /** 
  * The ConnectionMonitor class monitors the connection to the Leap Motion service
  * and displays a sprite in front of the camera when a connection is not
  * available. You can use the PluginLeapNotice sprites in the LeapMotion/Textures
  * folder or create your own.
*/
  [RequireComponent(typeof(SpriteRenderer))]
  public class ConnectionMonitor : MonoBehaviour {
    /** The LeapServiceProvider in the scene. */
    [AutoFind]
    [Tooltip("The scene LeapServiceProvider.")]
    public LeapServiceProvider provider;
    /** The speed to fade the sprite alpha from 0 to 1. */
    [Tooltip("How fast to make the connection notice sprite visible.")]
    [Range(0.1f, 10.0f)]
    public float fadeInTime = 1.0f;
    /** The speed to fade the sprite alpha from 1 to 0. */
    [Tooltip("How fast to fade out the connection notice sprite.")]
    [Range(0.1f, 10.0f)]
    public float fadeOutTime = 1.0f;
    /** The easing curve. */
    [Tooltip("The easing curve for the fade in and out effect.")]
    public AnimationCurve fadeCurve = AnimationCurve.EaseInOut(0.0f, 0.0f, 1.0f, 1.0f);
    /** How often to check the connection. */
    [Tooltip("How frequently to check the connection.")]
    public int monitorInterval = 2;
    /** The fully on texture tint color. */
    [Tooltip("A tint applied to the connection notice sprite when on.")]
    public Color onColor = Color.white;
    /** The distance of the notification sprite from the camera in world units. */
    [Tooltip("How far to place the sprite in front of the camera.")]
    public float distanceToCamera = 12.0f;

    private float fadedIn = 0.0f;
    private SpriteRenderer spriteRenderer;
    private bool connected = false;

    void Start() {
      spriteRenderer = GetComponent<SpriteRenderer>();
      SetAlpha(0.0f);
      StartCoroutine(Monitor());
    }

    void SetAlpha(float alpha) {
      spriteRenderer.color = Color.Lerp(Color.clear, onColor, alpha);
    }

    void Update() {
      if (fadedIn > 0) {
        Camera cam = Camera.main;
        Vector3 pos = cam.transform.position + cam.transform.forward * distanceToCamera;
        transform.position = pos;
        transform.LookAt(cam.transform);
      }
    }

    private IEnumerator Monitor() {
      yield return new WaitForSecondsRealtime(monitorInterval); //Give controller time to connect at startup
      while (true) { //forever
        connected = provider.IsConnected();
        if (connected) {
          while (fadedIn > 0.0) {
            fadedIn -= Time.deltaTime / fadeOutTime;
            fadedIn = Mathf.Clamp(fadedIn, 0.0f, 1.0f);
            SetAlpha(fadeCurve.Evaluate(fadedIn));
            yield return null;
          }
        } else {
          while (fadedIn < 1.0) {
            fadedIn += Time.deltaTime / fadeOutTime;
            fadedIn = Mathf.Clamp(fadedIn, 0.0f, 1.0f);
            SetAlpha(fadeCurve.Evaluate(fadedIn));
            yield return null;
          }
        }
        yield return new WaitForSecondsRealtime(monitorInterval);
      }
    }
  }
}
