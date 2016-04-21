using UnityEngine;
using System.Collections;

public interface ILeapWidget {
    void Expand();
    void Retract();
    void HoverDistance(float distance);
}
