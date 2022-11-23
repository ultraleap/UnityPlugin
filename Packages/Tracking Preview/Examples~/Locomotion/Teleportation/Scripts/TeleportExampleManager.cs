using Leap.Unity.Preview.Locomotion;
using UnityEngine;

public class TeleportExampleManager : MonoBehaviour
{
    public TeleportAnchor rayLatchTeleportAnchor, pinchToTeleportAnchor, jumpGemTeleportAnchor;
    public GameObject rayLatchTeleport;
    public GameObject pinchToTeleport;
    public GameObject jumpGemTeleport;

    private TeleportActionBase _rayLatchTeleportAction, _pinchToTeleportAction, _jumpGemTeleportAction;

    private void Start()
    {
        EnableTeleportAction(pinchToTeleport);
        pinchToTeleport.GetComponentInChildren<TeleportActionBase>().TeleportToAnchor(pinchToTeleportAnchor);

        _rayLatchTeleportAction = rayLatchTeleport.GetComponentInChildren<TeleportActionBase>();
        _pinchToTeleportAction = pinchToTeleport.GetComponentInChildren<TeleportActionBase>();
        _jumpGemTeleportAction = jumpGemTeleport.GetComponentInChildren<TeleportActionBase>();

        _rayLatchTeleportAction.RemoveTeleportAnchorFromFixedAnchors(_pinchToTeleportAction.freeTeleportAnchor);
        _rayLatchTeleportAction.RemoveTeleportAnchorFromFixedAnchors(_jumpGemTeleportAction.freeTeleportAnchor);

        _pinchToTeleportAction.RemoveTeleportAnchorFromFixedAnchors(_rayLatchTeleportAction.freeTeleportAnchor);
        _pinchToTeleportAction.RemoveTeleportAnchorFromFixedAnchors(_jumpGemTeleportAction.freeTeleportAnchor);

        _jumpGemTeleportAction.RemoveTeleportAnchorFromFixedAnchors(_pinchToTeleportAction.freeTeleportAnchor);
        _jumpGemTeleportAction.RemoveTeleportAnchorFromFixedAnchors(_rayLatchTeleportAction.freeTeleportAnchor);
    }

    private void OnEnable()
    {
        rayLatchTeleportAnchor.OnTeleportedTo += OnTeleportedToAnchor;
        pinchToTeleportAnchor.OnTeleportedTo += OnTeleportedToAnchor;
        jumpGemTeleportAnchor.OnTeleportedTo += OnTeleportedToAnchor;
    }

    private void OnDisable()
    {
        rayLatchTeleportAnchor.OnTeleportedTo -= OnTeleportedToAnchor;
        pinchToTeleportAnchor.OnTeleportedTo -= OnTeleportedToAnchor;
        jumpGemTeleportAnchor.OnTeleportedTo -= OnTeleportedToAnchor;
    }
    
    private void OnTeleportedToAnchor(TeleportAnchor anchor)
    {
        rayLatchTeleport.GetComponentInChildren<TeleportActionBase>().SetLastTeleportedAnchor(anchor);
        pinchToTeleport.GetComponentInChildren<TeleportActionBase>().SetLastTeleportedAnchor(anchor);
        jumpGemTeleport.GetComponentInChildren<TeleportActionBase>().SetLastTeleportedAnchor(anchor);

        if (anchor == rayLatchTeleportAnchor)
        {
            EnableTeleportAction(rayLatchTeleport);
        }
        else if(anchor == pinchToTeleportAnchor)
        {
            EnableTeleportAction(pinchToTeleport);
        } 
        else if(anchor == jumpGemTeleportAnchor)
        {
            EnableTeleportAction(jumpGemTeleport);
        }
    }

    private void EnableTeleportAction(GameObject teleportAction)
    {
        rayLatchTeleport.SetActive(false);
        pinchToTeleport.SetActive(false);
        jumpGemTeleport.SetActive(false);

        teleportAction.SetActive(true);
    }
}
