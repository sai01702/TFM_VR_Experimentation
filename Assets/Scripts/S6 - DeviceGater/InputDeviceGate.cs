using UnityEngine.InputSystem;
using UnityEngine.InputSystem.XR;

public static class InputDeviceGate
{
    public static void EnableDesktop(bool enable)
    {
        if (Keyboard.current != null) Toggle(Keyboard.current, enable);
        if (Mouse.current != null) Toggle(Mouse.current, enable);

        // Optional: block touchscreen/pen too
        foreach (var d in InputSystem.devices)
            if (d is Pen || d is Touchscreen) Toggle(d, enable);
    }

    public static void EnableXR(bool enable)
    {
        foreach (var d in InputSystem.devices)
        {
            // Covers HMD, controllers, generic tracked devices
            if (d is XRHMD || d is XRController || d is TrackedDevice || d.layout.Contains("XR"))
                Toggle(d, enable);
        }
    }

    static void Toggle(InputDevice device, bool enable)
    {
        if (enable) InputSystem.EnableDevice(device);
        else InputSystem.DisableDevice(device);
    }
}
