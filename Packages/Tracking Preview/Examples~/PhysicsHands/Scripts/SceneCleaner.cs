using System.Collections.Generic;
using UnityEngine;

public class SceneCleaner : MonoBehaviour
{
    private List<Pose> _poses = new List<Pose>();
    private List<Rigidbody> _bodies = new List<Rigidbody>();

    private void Start()
    {
        Rigidbody[] bodies = GetComponentsInChildren<Rigidbody>(true);
        for (int i = 0; i < bodies.Length; i++)
        {
            _bodies.Add(bodies[i]);
            _poses.Add(new Pose(bodies[i].transform.position, bodies[i].transform.rotation));
        }
    }

    public void ResetObjects()
    {
        for (int i = 0; i < _bodies.Count; i++)
        {
            _bodies[i].velocity = Vector3.zero;
            _bodies[i].angularVelocity = Vector3.zero;
            _bodies[i].transform.position = _poses[i].position;
            _bodies[i].transform.rotation = _poses[i].rotation;
        }
    }
}