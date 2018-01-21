using System;

static public class Win32
{
    public static int LoWord(int dwValue)
    {
        return (dwValue & 0xFFFF);
    }

    public static int HiWord(Int64 dwValue)
    {
        return (int)(dwValue >> 16) & ~FAPPCOMMANDMASK;
    }

    public static ushort LowWord(uint val)
    {
        return (ushort)val;
    }

    public static ushort HighWord(uint val)
    {
        return (ushort)(val >> 16);
    }

    public static uint BuildWParam(ushort low, ushort high)
    {
        return ((uint)high << 16) | low;
    }

    // ReSharper disable InconsistentNaming
    public const int KEYBOARD_OVERRUN_MAKE_CODE = 0xFF;
    public const int WM_APPCOMMAND = 0x0319;
    private const int FAPPCOMMANDMASK = 0xF000;
    internal const int FAPPCOMMANDMOUSE = 0x8000;
    internal const int FAPPCOMMANDOEM = 0x1000;

    public const int WM_KEYDOWN = 0x0100;
    public const int WM_KEYUP = 0x0101;
    internal const int WM_SYSKEYDOWN = 0x0104;
    internal const int WM_INPUT = 0x00FF;
    internal const int WM_USB_DEVICECHANGE = 0x0219;

    internal const int VK_SHIFT = 0x10;

    internal const int RI_KEY_MAKE = 0x00;      // Key Down
    internal const int RI_KEY_BREAK = 0x01;     // Key Up
    internal const int RI_KEY_E0 = 0x02;        // Left version of the key
    internal const int RI_KEY_E1 = 0x04;        // Right version of the key. Only seems to be set for the Pause/Break key.
        
    internal const int VK_CONTROL = 0x11;
    internal const int VK_MENU = 0x12;
    internal const int VK_ZOOM = 0xFB;
    internal const int VK_LSHIFT = 0xA0;
    internal const int VK_RSHIFT = 0xA1;
    internal const int VK_LCONTROL = 0xA2;
    internal const int VK_RCONTROL = 0xA3;
    internal const int VK_LMENU = 0xA4;
    internal const int VK_RMENU = 0xA5;
        
    internal const int SC_SHIFT_R = 0x36;     
    internal const int SC_SHIFT_L = 0x2a;   
    internal const int RIM_INPUT = 0x00;
    // ReSharper restore InconsistentNaming


    public static int VirtualKeyCorrection(int virtualKey, bool isE0BitSet, int makeCode)
    {
        var correctedVKey = virtualKey;

        //if (_rawBuffer.header.hDevice == IntPtr.Zero)
        //{
        //    // When hDevice is 0 and the vkey is VK_CONTROL indicates the ZOOM key
        //    if (_rawBuffer.data.keyboard.VKey == Win32.VK_CONTROL)
        //    {
        //        correctedVKey = Win32.VK_ZOOM;
        //    }
        //}
        //else
        {
            switch (virtualKey)
            {
                // Right-hand CTRL and ALT have their e0 bit set 
                case Win32.VK_CONTROL:
                    correctedVKey = isE0BitSet ? Win32.VK_RCONTROL : Win32.VK_LCONTROL;
                    break;
                case Win32.VK_MENU:
                    correctedVKey = isE0BitSet ? Win32.VK_RMENU : Win32.VK_LMENU;
                    break;
                case Win32.VK_SHIFT:
                    correctedVKey = makeCode == Win32.SC_SHIFT_R ? Win32.VK_RSHIFT : Win32.VK_LSHIFT;
                    break;
                default:
                    correctedVKey = virtualKey;
                    break;
            }
        }

        return correctedVKey;
    }
}
