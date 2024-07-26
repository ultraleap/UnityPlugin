using UnityEditor;

namespace Leap.PhysicalHands
{
    [CustomEditor(typeof(PhysicalHandEvents))]
    public class PhysicalHandEventsEditor : CustomEditorBase<PhysicalHandEvents>
    {
        protected override void OnEnable()
        {
            base.OnEnable();

            addPropertyToFoldout("onHoverEnter", "Any Hand Events", true);
            addPropertyToFoldout("onHover", "Any Hand Events", true);
            addPropertyToFoldout("onHoverExit", "Any Hand Events", true);
            addPropertyToFoldout("onContactEnter", "Any Hand Events", true);
            addPropertyToFoldout("onContact", "Any Hand Events", true);
            addPropertyToFoldout("onContactExit", "Any Hand Events", true);
            addPropertyToFoldout("onGrabEnter", "Any Hand Events", true);
            addPropertyToFoldout("onGrab", "Any Hand Events", true);
            addPropertyToFoldout("onGrabExit", "Any Hand Events", true);

            addPropertyToFoldout("onLeftHandHoverEnter", "Left Hand Events");
            addPropertyToFoldout("onLeftHandHover", "Left Hand Events");
            addPropertyToFoldout("onLeftHandHoverExit", "Left Hand Events");
            addPropertyToFoldout("onLeftHandContactEnter", "Left Hand Events");
            addPropertyToFoldout("onLeftHandContact", "Left Hand Events");
            addPropertyToFoldout("onLeftHandContactExit", "Left Hand Events");
            addPropertyToFoldout("onLeftHandGrabEnter", "Left Hand Events");
            addPropertyToFoldout("onLeftHandGrab", "Left Hand Events");
            addPropertyToFoldout("onLeftHandGrabExit", "Left Hand Events");

            addPropertyToFoldout("onRightHandHoverEnter", "Right Hand Events");
            addPropertyToFoldout("onRightHandHover", "Right Hand Events");
            addPropertyToFoldout("onRightHandHoverExit", "Right Hand Events");
            addPropertyToFoldout("onRightHandContactEnter", "Right Hand Events");
            addPropertyToFoldout("onRightHandContact", "Right Hand Events");
            addPropertyToFoldout("onRightHandContactExit", "Right Hand Events");
            addPropertyToFoldout("onRightHandGrabEnter", "Right Hand Events");
            addPropertyToFoldout("onRightHandGrab", "Right Hand Events");
            addPropertyToFoldout("onRightHandGrabExit", "Right Hand Events");
        }
    }
}