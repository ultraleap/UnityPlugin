using UnityEngine;
using UnityEngine.Events;

using Leap.Unity;

public class ShortcutTrigger : MonoBehaviour
{
    public Chirality chirality;

    public LeapProvider _provider;
    public float triggerThreshold = 0.8f;
    MeshRenderer _meshRenderer;

    float FULL_SCALE = 0.03f;
    float MIN_SCALE = 0.006f;

    public UnityEvent OnShortcutTriggered;
    public UnityEvent OnShortcutReleased;

    bool triggered = false;

    private void OnEnable()
    {
        _provider.OnPostUpdateFrame += PositionShorcuts;
        _meshRenderer = GetComponent<MeshRenderer>();
        OnShortcutReleased?.Invoke();
        triggered = false;
        _meshRenderer.enabled = true;
    }

    private void OnDisable()
    {
        _provider.OnPostUpdateFrame -= PositionShorcuts;
        OnShortcutReleased?.Invoke();
    }

    void PositionShorcuts(Leap.Frame frame)
    {
        foreach (Leap.Hand hand in frame.Hands)
        {
            if (chirality == Chirality.Left ? hand.IsLeft : hand.IsRight)
            {
                Vector3 thumbTip = hand.GetThumb().TipPosition;
                Vector3 indexTip = hand.GetIndex().TipPosition;

                transform.position = (thumbTip + indexTip) / 2;
                transform.up = thumbTip - indexTip;
                transform.localScale = new Vector3(transform.localScale.x, 
                                                    FULL_SCALE * Mathf.Clamp01(1-hand.PinchStrength + MIN_SCALE), 
                                                    transform.localScale.z);

                if(hand.PinchStrength > triggerThreshold)
                {
                    _meshRenderer.enabled = false;

                    if (!triggered)
                        OnShortcutTriggered?.Invoke();

                    triggered = true;
                }
                else
                {
                    _meshRenderer.enabled = true;

                    if (triggered)
                        OnShortcutReleased?.Invoke();

                    triggered = false;
                }

                if(hand.PinchStrength < 0.01f)
                {
                    _meshRenderer.enabled = false;
                }
            }
        }
    }
}