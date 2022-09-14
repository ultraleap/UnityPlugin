using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;
using Leap.Unity;

public class MenuWheel : MonoBehaviour
{
    public Transform segmentPrefab;
    public Transform borderPrefab;
    public Transform titlePrefab;

    public Transform wheelCanvas;

    public WheelSegment[] wheelSegments;

    int currentSegmentIndex = 0;

    private void Awake()
    {
        for(int i = 0; i < wheelSegments.Length; i++)
        {
            wheelSegments[i].segmentTransform = Instantiate(segmentPrefab, wheelCanvas);
            wheelSegments[i].borderTransform = Instantiate(borderPrefab, wheelCanvas);
            wheelSegments[i].titleTransform = Instantiate(titlePrefab, wheelCanvas);

            wheelSegments[i].img = wheelSegments[i].segmentTransform.GetComponent<Image>();

            float fillPercent = (float)(i + 1) / wheelSegments.Length;
            float fillDegrees = 360 * fillPercent;

            wheelSegments[i].img.fillAmount = fillPercent;

            wheelSegments[i].borderTransform.rotation = Quaternion.Euler(wheelSegments[i].borderTransform.eulerAngles.x, wheelSegments[i].borderTransform.eulerAngles.y, wheelSegments[i].borderTransform.eulerAngles.z - fillDegrees);
            wheelSegments[i].titleTransform.rotation = Quaternion.Euler(wheelSegments[i].titleTransform.eulerAngles.x, wheelSegments[i].titleTransform.eulerAngles.y, wheelSegments[i].titleTransform.eulerAngles.z + fillDegrees - ((360 * (1f / wheelSegments.Length))/2));

            wheelSegments[i].titleTransform.GetComponent<TextMeshProUGUI>().text = wheelSegments[i].segmentName;

            wheelSegments[i].maxAngle = fillDegrees;
        }

        for (int i = 0; i < wheelSegments.Length; i++)
        {
            wheelSegments[i].segmentTransform.SetAsFirstSibling();
        }
    }

    public void ShowMenu()
    {
        transform.position = Hands.Right.GetPinchPosition();
        transform.up = ((Camera.main.transform.position + Vector3.up * 0.1f) - transform.position).normalized;
    }

    public void SelectCurrent()
    {
        for(int i = 0; i < wheelSegments.Length; i++)
        {
            if(i != currentSegmentIndex)
            {
                foreach(var go in wheelSegments[i].associatedGameObjects)
                {
                    go.SetActive(false);
                }
            }
        }

        foreach (var go in wheelSegments[currentSegmentIndex].associatedGameObjects)
        {
            go.SetActive(true);
        }
    }

    private void Update()
    {
        if(Hands.Right != null)
        {
            float angle = GetAngleToPinchPos();
            bool set = false;

            currentSegmentIndex = 0;

            foreach(var segment in wheelSegments)
            {
                if(!set && angle <= segment.maxAngle)
                {
                    set = true;
                    segment.img.color = segment.segmentColor;
                }
                else
                {
                    segment.img.color = Color.gray;
                }

                if (!set)
                    currentSegmentIndex++;
            }
        }
    }

    float GetAngleToPinchPos()
    {
        Vector3 flatPinchPos = Hands.Right.GetPinchPosition();
        flatPinchPos.y = 0;

        Vector3 flatPos = transform.position;
        flatPos.y = 0;

        Vector3 targetDir = flatPinchPos - flatPos;
        float angle = Vector3.Angle(targetDir, -transform.forward);
        float angle2 = Vector3.Angle(targetDir, transform.right);

        if (angle2 > 90)
        {
            angle = 360 - angle;
        }

        return angle;
    }

    [System.Serializable]
    public struct WheelSegment
    {
        public string segmentName;
        public Color segmentColor;

        [HideInInspector]
        public Transform segmentTransform;
        [HideInInspector]
        public Transform borderTransform;
        [HideInInspector]
        public Transform titleTransform;

        [HideInInspector]
        public float maxAngle;

        [HideInInspector]
        public Image img;

        public GameObject[] associatedGameObjects;
    }
}