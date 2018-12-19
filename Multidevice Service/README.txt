To Install the Alpha Multidevice Service:
1) Use Ctrl-Alt-Delete to Stop the `LeapService` under `Services`
2) Close the Leap Control Panel in the task bar (it cannot ever be open for Multidevice to work)
3) Replace the LeapSvc.exe found at `C:\Program Files\Leap Motion\Core Services\` with the LeapSvc.exe found in this folder.
4) Add `RunMultideviceService.bat` to that directory, and run it.
	4a) You should see the serial numbers of both devices appear in the service, if not, make sure you followed the prior steps.
5) The example in UnityModules\Assets\LeapMotion\Core\Examples\Multi-Device (Desktop).unity should light up the IR LEDs in both devices
	5a) You should see hands from both devices in the scene.