// Original Code:
// Unity PINVOKE interface for pastebin.com/0Szi8ga6
// Handles multiple cursors
// License: CC0
// modified by gasbank

using UnityEngine;
using System.Runtime.InteropServices;
using System;
using System.Collections.Generic;

public class MouseInputManager : MonoBehaviour
{
    [DllImport("LibRawInput")]
    private static extern bool init();
    [DllImport("LibRawInput")]
    private static extern bool kill();
    [DllImport("LibRawInput")]
    private static extern IntPtr poll();

    public bool verbose;
    public GameObject moveGameObjectPrefab;
    public float moveSpeed = 4.0f;

    public const byte RE_DEVICE_CONNECT = 0;
    public const byte RE_MOUSE = 2;
    public const byte RE_KEYBOARD = 3;
    public const byte RE_DEVICE_DISCONNECT = 1;
    public string getEventName(byte id)
    {
        switch (id)
        {
            case RE_DEVICE_CONNECT: return "RE_DEVICE_CONNECT";
            case RE_DEVICE_DISCONNECT: return "RE_DEVICE_DISCONNECT";
            case RE_MOUSE: return "RE_MOUSE";
            case RE_KEYBOARD: return "RE_KEYBOARD";
        }
        return "UNKNOWN(" + id + ")";
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct RawInputEvent
    {
        public int devHandle;
        public int x, y, wheel;
        public byte press;
        public byte release;
        public byte type;
        public byte padding0;
        public ushort keyboardFlags;
        public ushort keyboardVkey;
        public ushort keyboardMakeCode;
    }

    Dictionary<int, GameObject> moveGameObjectsByDeviceId = new Dictionary<int, GameObject>();
    Dictionary<int, Vector3> moveDeltaByDeviceId = new Dictionary<int, Vector3>();
    Dictionary<int, Dictionary<string, Boolean>> keysByDeviceId = new Dictionary<int, Dictionary<string, bool>>();
    Dictionary<int, Dictionary<string, Boolean>> downKeysByDeviceId = new Dictionary<int, Dictionary<string, bool>>();
    Dictionary<int, Dictionary<string, Boolean>> upKeysByDeviceId = new Dictionary<int, Dictionary<string, bool>>();

    void Start()
    {
        init();
    }

    void Update()
    {
        foreach (var downKeys in downKeysByDeviceId)
        {
            downKeys.Value.Clear();
        }
        foreach (var upKeys in upKeysByDeviceId)
        {
            upKeys.Value.Clear();
        }

        // Poll the events and properly update whatever we need
        IntPtr data = poll();
        int numEvents = Marshal.ReadInt32(data);
        for (int i = 0; i < numEvents; ++i)
        {
            var ev = new RawInputEvent();
            long offset = data.ToInt64() + sizeof(int) + i * Marshal.SizeOf(ev);
            ev.devHandle = Marshal.ReadInt32(new IntPtr(offset + 0));
            ev.x = Marshal.ReadInt32(new IntPtr(offset + 4));
            ev.y = Marshal.ReadInt32(new IntPtr(offset + 8));
            ev.wheel = Marshal.ReadInt32(new IntPtr(offset + 12));
            ev.press = Marshal.ReadByte(new IntPtr(offset + 16));
            ev.release = Marshal.ReadByte(new IntPtr(offset + 17));
            ev.type = Marshal.ReadByte(new IntPtr(offset + 18));
            ev.padding0 = Marshal.ReadByte(new IntPtr(offset + 19));
            ev.keyboardFlags = (ushort)Marshal.ReadInt16(new IntPtr(offset + 20));
            ev.keyboardVkey = (ushort)Marshal.ReadInt16(new IntPtr(offset + 22));
            ev.keyboardMakeCode = (ushort)Marshal.ReadInt16(new IntPtr(offset + 24));

            if (ev.type == RE_DEVICE_CONNECT)
            {
                Debug.LogFormat("Device connected: handle {0}", ev.devHandle);
            }
            else if (ev.type == RE_DEVICE_DISCONNECT)
            {
                Debug.LogFormat("Device disconnected: handle {0}", ev.devHandle);
                DestroyMoveObject(ev.devHandle);
            }
            else if (ev.type == RE_KEYBOARD)
            {
                var isBreakBitSet = ((ev.keyboardFlags & Win32.RI_KEY_BREAK) != 0);
                var isE0BitSet = ((ev.keyboardFlags & Win32.RI_KEY_E0) != 0);

                var keyName = KeyMapper.GetKeyName(Win32.VirtualKeyCorrection(ev.keyboardVkey, isE0BitSet, ev.keyboardMakeCode)).ToUpper();

                if (verbose)
                {
                    Debug.LogFormat("{0} H:{1} Key:{2} Break:{3} Flags:{4} Vkey:{5}",
                        getEventName(ev.type),
                        ev.devHandle,
                        keyName,
                        isBreakBitSet,
                        ev.keyboardFlags,
                        ev.keyboardVkey);
                }

                // Pressed keys
                if (!keysByDeviceId.ContainsKey(ev.devHandle))
                {
                    keysByDeviceId[ev.devHandle] = new Dictionary<string, bool>();
                }
                var oldPressed = IsKeyPressed(ev.devHandle, keyName);
                var newPressed = (isBreakBitSet == false);
                keysByDeviceId[ev.devHandle][keyName] = newPressed;

                // Down keys
                if (!downKeysByDeviceId.ContainsKey(ev.devHandle))
                {
                    downKeysByDeviceId[ev.devHandle] = new Dictionary<string, bool>();
                }
                if (oldPressed == false && newPressed == true)
                {
                    downKeysByDeviceId[ev.devHandle][keyName] = true;
                }

                // Up keys
                if (!upKeysByDeviceId.ContainsKey(ev.devHandle))
                {
                    upKeysByDeviceId[ev.devHandle] = new Dictionary<string, bool>();
                }
                if (oldPressed == true && newPressed == false)
                {
                    upKeysByDeviceId[ev.devHandle][keyName] = true;
                }
            }
        }
        Marshal.FreeCoTaskMem(data);

        foreach (var pk in keysByDeviceId)
        {
            var devHandle = pk.Key;
            var pressedKeys = pk.Value;

            Vector3[] pressed = new Vector3[4];
            if (IsKeyTrue(pressedKeys, "LEFT"))
            {
                pressed[0] = Vector3.left;
            }
            if (IsKeyTrue(pressedKeys, "RIGHT"))
            {
                pressed[1] = Vector3.right;
            }
            if (IsKeyTrue(pressedKeys, "UP"))
            {
                pressed[2] = Vector3.forward;
            }
            if (IsKeyTrue(pressedKeys, "DOWN"))
            {
                pressed[3] = Vector3.back;
            }
            moveDeltaByDeviceId[devHandle] = pressed[0] + pressed[1] + pressed[2] + pressed[3];
        }

        if (verbose)
        {
            foreach (var pk in downKeysByDeviceId)
            {
                foreach (var k in pk.Value)
                {
                    if (k.Value)
                    {
                        Debug.LogFormat("Dev Handle: {0} Key Down: {1}", pk.Key, k.Key);
                    }
                }
            }

            foreach (var pk in upKeysByDeviceId)
            {
                foreach (var k in pk.Value)
                {
                    if (k.Value)
                    {
                        Debug.LogFormat("Dev Handle: {0} Key Up: {1}", pk.Key, k.Key);
                    }
                }
            }
        }

        foreach (var md in moveDeltaByDeviceId)
        {

            if (md.Value != Vector3.zero)
            {
                var moveDelta = md.Value.normalized * Time.deltaTime * moveSpeed;
                GetMoveObject(md.Key).transform.Translate(moveDelta);
            }
        }
    }

    bool IsKeyPressed(int devHandle, string name)
    {
        Dictionary<string, bool> pressedKeys;
        if (keysByDeviceId.TryGetValue(devHandle, out pressedKeys))
        {
            return IsKeyTrue(pressedKeys, name);
        }
        return false;
    }

    private static bool IsKeyTrue(Dictionary<string, bool> keys, string name)
    {
        bool pressed = false;
        return keys.TryGetValue(name, out pressed) && pressed;
    }

    GameObject GetMoveObject(int h)
    {
        if (moveGameObjectsByDeviceId.ContainsKey(h) == false)
        {
            moveGameObjectsByDeviceId[h] = Instantiate(moveGameObjectPrefab) as GameObject;
        }

        return moveGameObjectsByDeviceId[h];
    }

    void DestroyMoveObject(int h)
    {
        GameObject moveObject;
        if (moveGameObjectsByDeviceId.TryGetValue(h, out moveObject))
        {
            Destroy(moveObject);
            moveGameObjectsByDeviceId.Remove(h);
        }
    }

    void OnApplicationQuit()
    {
        kill();
    }
}