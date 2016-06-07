using UnityEngine;
using UnityEngine.VR;
using Leap.Unity;
using Leap;
using System.Collections;

public class Cursor3D : MonoBehaviour {
    [Tooltip("The current Leap Data Provider for the scene.")]
    public LeapProvider LeapDataProvider;
    [Tooltip("The diameter of the visual sphere cursor.")]
    public float RenderSphereDiameter = 0.1f;
    [Tooltip("The diameter of the collider that checks for pinchable objects.")]
    public float CollisionSphereDiameter = 0.1f;
    [Tooltip("The amount of motion amplification for cursor movement.")]
    public float MotionScalingFactor = 6f;
    [Tooltip("The amount that the cursor is lerped to its previous position per frame.")]
    public float CursorSmoothingFactor = 0.2f;

    [Tooltip("The springiness of the spring joint.")]
    public float Spring = 500.0f;
    [Tooltip("The dampening of the spring joint.")]
    public float Damper = 10.5f;
    [Tooltip("The drag applied to the object while it is being dragged.")]
    public float Drag = 10.0f;
    [Tooltip("The angular drag applied to the object while it is being dragged.")]
    public float AngularDrag = 5.0f;
    [Tooltip("The amount of dead-zone in the spring joint.")]
    public float Distance = 0f;

    private Quaternion CurrentRotation;

    [SerializeField]
    private Mesh _sphereMesh;
    [SerializeField]
    private Material _sphereMaterial;

    private GameObject[] Cursors;
    private SpringJoint[,] SpringJoints;
    private bool[] prevPinching;

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

        for (int i = 0; i < Cursors.Length; i++)
        {
          Cursors[i] = new GameObject("Cursor " + i);
            Cursors[i].AddComponent<MeshFilter>().mesh = _sphereMesh;
            Cursors[i].AddComponent<MeshRenderer>().sharedMaterial = _sphereMaterial;
            Cursors[i].AddComponent<Rigidbody>().isKinematic = true;
            Cursors[i].transform.parent = transform;
            Cursors[i].transform.localScale = Vector3.one * RenderSphereDiameter;
        }

        prevPinching = new bool[2];
        SpringJoints = new SpringJoint[2,10];
	}

    //Update the Head Yaw for Calculating "Shoulder Positions"
    void Update()
    {
        Frame curFrame = LeapDataProvider.CurrentFrame.TransformedCopy(LeapTransform.Identity);

        Quaternion HeadYaw = Quaternion.Euler(0f, InputTracking.GetLocalRotation(VRNode.Head).eulerAngles.y, 0f);
        CurrentRotation = Quaternion.Slerp(CurrentRotation, HeadYaw, 0.1f);

        for (int whichHand = 0; whichHand < 2; whichHand++)
        {
            if (whichHand > curFrame.Hands.Count - 1){
              if (Cursors[whichHand].activeInHierarchy) {
                Cursors[whichHand].SetActive(false);
              }
              continue;
            } else {
              if (!Cursors[whichHand].activeInHierarchy) {
                Cursors[whichHand].SetActive(true);
              }
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

            Cursors[whichHand].transform.position = Vector3.Lerp(Cursors[whichHand].transform.position, ProjectionOrigin + (Offset * MotionScalingFactor), CursorSmoothingFactor);

            if (curFrame.Hands[whichHand].PinchDistance < 30f)
            {
                if (!prevPinching[whichHand])
                {
                    prevPinching[whichHand] = true;
                    Cursors[whichHand].GetComponent<MeshRenderer>().material.color = Color.green;

                    Collider[] Colliders = Physics.OverlapSphere(Cursors[whichHand].transform.position, CollisionSphereDiameter);

                    for(int i = 0; i < Mathf.Min(10,Colliders.Length); i++){
                      // We need to hit a rigidbody that is not kinematic
                      if (!Colliders[i].attachedRigidbody || Colliders[i].attachedRigidbody.isKinematic) {
                        return;
                      }

                      if (!SpringJoints[whichHand, i]) {
                        SpringJoints[whichHand, i] = Cursors[whichHand].AddComponent<SpringJoint>();
                        Cursors[whichHand].GetComponent<Rigidbody>().isKinematic = true;
                      }

                      SpringJoints[whichHand, i].transform.position = Cursors[whichHand].transform.position;
                      SpringJoints[whichHand, i].anchor = Vector3.zero;

                      SpringJoints[whichHand, i].spring = Spring;
                      SpringJoints[whichHand, i].damper = Damper;
                      SpringJoints[whichHand, i].maxDistance = Distance;
                      SpringJoints[whichHand, i].connectedBody = Colliders[i].attachedRigidbody;

                      StartCoroutine(DragObject(whichHand, i));
                    }
                }
            }else if (curFrame.Hands[whichHand].PinchDistance > 40f) {
                if (prevPinching[whichHand])
                {
                    prevPinching[whichHand] = false;
                    Cursors[whichHand].GetComponent<MeshRenderer>().material.color = Color.white;

                    //constraint breaks implicitly when prevPinching is set to false
                }
            }
        }
    }

    private IEnumerator DragObject(int whichHand, int i)
    {
        float oldDrag = SpringJoints[whichHand, i].connectedBody.drag;
        float oldAngularDrag = SpringJoints[whichHand, i].connectedBody.angularDrag;
        SpringJoints[whichHand, i].connectedBody.drag = Drag;
        SpringJoints[whichHand, i].connectedBody.angularDrag = AngularDrag;

        while (prevPinching[whichHand])
        {
          SpringJoints[whichHand, i].transform.position = Cursors[whichHand].transform.position;
            yield return null;
        }

        if (SpringJoints[whichHand, i].connectedBody)
        {
          SpringJoints[whichHand, i].connectedBody.drag = oldDrag;
          SpringJoints[whichHand, i].connectedBody.angularDrag = oldAngularDrag;
          SpringJoints[whichHand, i].connectedBody = null;
          Destroy(SpringJoints[whichHand, i]);
        }
    }
}
