using UnityEngine;
using UnityEngine.XR;

public static class XRHelpers
{
    // Returns true if an HMD is active/valid (OpenXR)
    public static bool IsHeadsetPresent()
    {
        return XRSettings.isDeviceActive ||
               InputDevices.GetDeviceAtXRNode(XRNode.Head).isValid;
    }
}
