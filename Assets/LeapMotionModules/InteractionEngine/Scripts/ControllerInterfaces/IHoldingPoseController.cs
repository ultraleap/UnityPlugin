using UnityEngine;

namespace Leap.Unity.Interaction {

  /**
  * IHoldingPoseController defines the interface used by the Interaction Engine
  * to request the best pose for an object when it is held.
  *
  * The Interaction Engine provides the HoldingPoseControllerKabsh implementation for
  * this controller.
  * @since 4.1.4
  */
  public abstract class IHoldingPoseController : IControllerBase {
    /**
    * Add the specified hand to the pose calculation.
    * @param hand The Leap.Hand object containing the reported tracking data.
    * @since 4.1.4
    */
    public abstract void AddHand(Hand hand);
    /**
    * Reports that a hand has been re-identified and that you should replace the
    * old hand with the new hand data.
    * @param oldId the previous Leap.Hand.Id value
    * @param newId the replacement Leap.Hand.Id
    * @since 4.1.4
    */
    public abstract void TransferHandId(int oldId, int newId);
    /**
    * Remove the specified hand from the pose calculation.
    * @param hand The Leap.Hand object to be removed.
    * @since 4.1.4
    */
    public abstract void RemoveHand(Hand hand);
    /**
    * Calculate the best holding pose given the current state of the hands and 
    * interactable object.
    * @param hands the list of hands with the current tracking data.
    * @param position A Vector3 object to be filled with the disred object position.
    * @param rotation A Quaternion object to be filled with the desired rotation.
    * @since 4.1.4
    */
    public abstract void GetHoldingPose(ReadonlyList<Hand> hands, out Vector3 position, out Quaternion rotation);
  }
}
