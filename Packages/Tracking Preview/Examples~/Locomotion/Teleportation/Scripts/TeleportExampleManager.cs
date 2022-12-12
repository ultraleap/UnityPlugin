using Leap.Unity.Preview.Locomotion;
using UnityEngine;

public class TeleportExampleManager : MonoBehaviour
{
    public TeleportAnchor pinchToTeleportAnchor, jumpGemTeleportAnchor;
    public GameObject pinchToTeleport;
    public GameObject jumpGemTeleport;

    private TeleportActionBase _pinchToTeleportAction, _jumpGemTeleportAction;

    private void Start()
    {
        EnableTeleportAction(pinchToTeleport);
        pinchToTeleport.GetComponentInChildren<TeleportActionBase>().TeleportToAnchor(pinchToTeleportAnchor);

        _pinchToTeleportAction = pinchToTeleport.GetComponentInChildren<TeleportActionBase>();
        _jumpGemTeleportAction = jumpGemTeleport.GetComponentInChildren<TeleportActionBase>();

        _pinchToTeleportAction.RemoveTeleportAnchorFromFixedAnchors(_jumpGemTeleportAction.freeTeleportAnchor);

        _jumpGemTeleportAction.RemoveTeleportAnchorFromFixedAnchors(_pinchToTeleportAction.freeTeleportAnchor);
    }

    private void OnEnable()
    {
        pinchToTeleportAnchor.OnTeleportedTo += OnTeleportedToAnchor;
        jumpGemTeleportAnchor.OnTeleportedTo += OnTeleportedToAnchor;
    }

    private void OnDisable()
    {
        pinchToTeleportAnchor.OnTeleportedTo -= OnTeleportedToAnchor;
        jumpGemTeleportAnchor.OnTeleportedTo -= OnTeleportedToAnchor;
    }
    
    private void OnTeleportedToAnchor(TeleportAnchor anchor)
    {
        pinchToTeleport.GetComponentInChildren<TeleportActionBase>().SetLastTeleportedAnchor(anchor);
        jumpGemTeleport.GetComponentInChildren<TeleportActionBase>().SetLastTeleportedAnchor(anchor);

        if(anchor == pinchToTeleportAnchor)
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
        pinchToTeleport.SetActive(false);
        jumpGemTeleport.SetActive(false);

        teleportAction.SetActive(true);
    }
}
