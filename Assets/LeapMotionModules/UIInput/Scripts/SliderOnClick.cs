using UnityEngine;
using UnityEngine.Events;

public class SliderOnClick : MonoBehaviour {
    public UnityEvent OnClick;

    float lastClicked = 0f;
    bool prevClicked = false;

    public void Dragging()
    {
        if (Time.time - lastClicked > 0.1f) {
            OnClick.Invoke();
            prevClicked = true;
            lastClicked = Time.time;
        } else {
            prevClicked = true;
            lastClicked = Time.time;
        }
    }
}
