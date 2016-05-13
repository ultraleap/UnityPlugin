using UnityEngine;
using UnityEngine.Events;

namespace Leap.Unity.InputModule
{
    public class SliderOnClick : MonoBehaviour
    {
        public UnityEvent OnClick;
        float lastClicked = 0f;

        public void Dragging()
        {
            if (Time.time - lastClicked > 0.1f) {
                OnClick.Invoke();
            }
            lastClicked = Time.time;
        }
    }
}