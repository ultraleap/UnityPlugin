using Leap;
using Leap.Unity;
using Leap.Unity.InputModule;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SquishyPointer : MonoBehaviour
{
    [SerializeField] private UIInputModule module;
    [SerializeField] PointerElement pointerElement;
    [SerializeField] Chirality chirality;
    [SerializeField] MeshRenderer meshRenderer;

    LeapProvider leapDataProvider;

    public float lerpSpeed = 10;

    private void Awake()
    {
        leapDataProvider = module.LeapDataProvider;
        meshRenderer.material = new Material(Shader.Find("Unlit/Color"));
    }

    private void Update()
    {
        var hand = leapDataProvider.CurrentFrame.GetHand(chirality);

        if (hand == null) return;

        transform.position = Vector3.Lerp(meshRenderer.transform.position, hand.GetPinchPosition(), Time.deltaTime * lerpSpeed);
        transform.localScale = Vector3.Lerp(Vector3.one, new Vector3(1, 0.5f, 0.5f), hand.PinchStrength);
        transform.LookAt(pointerElement.transform);
        meshRenderer.material.color = Color.Lerp(Color.white, Color.green, hand.PinchStrength);
    }
}