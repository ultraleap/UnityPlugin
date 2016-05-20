using UnityEngine;
using UnityEngine.VR;
using Leap.Unity;
using Leap;
using System.Collections;

public class Cursor3D : MonoBehaviour {
    [Tooltip("The current Leap Data Provider for the scene.")]
    public LeapProvider LeapDataProvider;
    public float RenderSphereDiameter = 0.1f;
    public float CollisionSphereDiameter = 0.1f;
    public float ScalingFactor = 6f;
    public float CursorDampingFactor = 0.8f;

    public float k_Spring = 500.0f;
    public float k_Damper = 10.5f;
    public float k_Drag = 10.0f;
    public float k_AngularDrag = 5.0f;
    public float k_Distance = 0f;

    private Quaternion CurrentRotation;

    [SerializeField]
    private Mesh _sphereMesh;
    [SerializeField]
    private Material _sphereMaterial;

    private GameObject[] Cursors;
    private SpringJoint[] SpringJoints;
    private bool[] prevPinching;

    //Can only handle one pinch per frame
    //Two colliders don't return two uniquely identifiable OnTriggerEnter's
    private SphereCollider radialcollider;
    private int justPinched = 0;

	// Use this for initialization
	void Start () {
        if (LeapDataProvider == null)
        {
            LeapDataProvider = FindObjectOfType<LeapProvider>();
            if (LeapDataProvider == null || !LeapDataProvider.isActiveAndEnabled)
            {
                Debug.LogError("Cannot use LeapImageRetriever if there is no LeapProvider!");
                enabled = false;
                return;
            }
        }

        Cursors = new GameObject[2];
        radialcollider = this.gameObject.AddComponent<SphereCollider>();
        radialcollider.radius = CollisionSphereDiameter / 2f;
        radialcollider.enabled = false;
        radialcollider.isTrigger = true;

        for (int i = 0; i < Cursors.Length; i++)
        {
            Cursors[i] = new GameObject();
            Cursors[i].AddComponent<MeshFilter>().mesh = _sphereMesh;
            Cursors[i].AddComponent<MeshRenderer>().sharedMaterial = _sphereMaterial;
            Cursors[i].transform.parent = transform;
            Cursors[i].transform.localScale = Vector3.one * RenderSphereDiameter;
        }

        prevPinching = new bool[2];
        SpringJoints = new SpringJoint[2];
	}

    //Update the Head Yaw for Calculating "Shoulder Positions"
    void Update()
    {
        Frame curFrame = LeapDataProvider.CurrentFrame.TransformedCopy(LeapTransform.Identity);

        Quaternion HeadYaw = Quaternion.Euler(0f, InputTracking.GetLocalRotation(VRNode.Head).eulerAngles.y, 0f);
        CurrentRotation = Quaternion.Slerp(CurrentRotation, HeadYaw, 0.1f);

        radialcollider.enabled = false;

        for (int whichHand = 0; whichHand < curFrame.Hands.Count; whichHand++)
        {
            if (whichHand > curFrame.Hands.Count)
            {
                continue;
            }

            Vector3 ProjectionOrigin = Vector3.zero;
            switch (curFrame.Hands[whichHand].IsRight)
            {
                case true:
                    ProjectionOrigin = InputTracking.GetLocalPosition(VRNode.Head) + CurrentRotation * new Vector3(0.15f, -0.13f, 0.1f);
                    break;
                case false:
                    ProjectionOrigin = InputTracking.GetLocalPosition(VRNode.Head) + CurrentRotation * new Vector3(-0.15f, -0.13f, 0.1f);
                    break;
            }

            Vector3 Offset = curFrame.Hands[whichHand].Fingers[1].Bone(Bone.BoneType.TYPE_METACARPAL).Center.ToVector3() - ProjectionOrigin;

            Cursors[whichHand].transform.position = Vector3.Lerp(Cursors[whichHand].transform.position, ProjectionOrigin + (Offset * ScalingFactor), 1f - CursorDampingFactor);

            if (curFrame.Hands[whichHand].PinchDistance < 30f)
            {
                if (!prevPinching[whichHand])
                {
                    prevPinching[whichHand] = true;
                    Cursors[whichHand].GetComponent<MeshRenderer>().material.color = Color.green;

                    radialcollider.center = transform.InverseTransformPoint(Cursors[whichHand].transform.position);
                    justPinched = whichHand;
                    radialcollider.enabled = true;
                }
            }else{
                if (prevPinching[whichHand])
                {
                    prevPinching[whichHand] = false;
                    Cursors[whichHand].GetComponent<MeshRenderer>().material.color = Color.white;

                    //constraint breaks implicitly when prevPinching is set to false
                }
            }
        }
    }

    void OnTriggerEnter(Collider hit)
    {
        // We need to hit a rigidbody that is not kinematic
        if (!hit.attachedRigidbody || hit.attachedRigidbody.isKinematic)
        {
            return;
        }

        if (!SpringJoints[justPinched])
        {
            var go = new GameObject("Rigidbody dragger");
            Rigidbody body = go.AddComponent<Rigidbody>();
            SpringJoints[justPinched] = go.AddComponent<SpringJoint>();
            body.isKinematic = true;
        }

        SpringJoints[justPinched].transform.position = Cursors[justPinched].transform.position;
        SpringJoints[justPinched].anchor = Vector3.zero;

        SpringJoints[justPinched].spring = k_Spring;
        SpringJoints[justPinched].damper = k_Damper;
        SpringJoints[justPinched].maxDistance = k_Distance;
        SpringJoints[justPinched].connectedBody = hit.attachedRigidbody;

        StartCoroutine("DragObject", justPinched);
    }

    private IEnumerator DragObject(int whichHand)
    {
        float oldDrag = SpringJoints[whichHand].connectedBody.drag;
        float oldAngularDrag = SpringJoints[whichHand].connectedBody.angularDrag;
        SpringJoints[whichHand].connectedBody.drag = k_Drag;
        SpringJoints[whichHand].connectedBody.angularDrag = k_AngularDrag;

        while (prevPinching[whichHand])
        {
            SpringJoints[whichHand].transform.position = Cursors[whichHand].transform.position;
            yield return null;
        }

        if (SpringJoints[whichHand].connectedBody)
        {
            SpringJoints[whichHand].connectedBody.drag = oldDrag;
            SpringJoints[whichHand].connectedBody.angularDrag = oldAngularDrag;
            SpringJoints[whichHand].connectedBody = null;
        }
    }
}
