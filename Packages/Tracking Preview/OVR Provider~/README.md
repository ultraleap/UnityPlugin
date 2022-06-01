# Ultraleap OVRProvider

This is an experimental compatibility layer that allows to use Quest hand tracking with interactions built using the Ultraleap Tracking Plugin.

## Setup

1. Download the [Oculus Integration](https://assetstore.unity.com/packages/tools/integration/oculus-integration-82022) from the Asset Store into a project
2. Open `.\Examples\OVR Leap Provider Example.unity`

## Known issues

1. When using Ultraleap Tracking package Examples, users may see compile errors relating to Anchor. To resolve this, explicitly use the Ultraleap namespace by replacing 'Anchor' with 'Leap.Unity.Interaction.Anchor' in SimpleAnchorFeedback.cs

## Thanks to

GitHub users:
- @julienkay
- @mrchantey