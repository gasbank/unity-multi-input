using System.Collections.Generic;
using UnityEngine;

public class MultiInputExample : MonoBehaviour
{
    public GameObject moveGameObjectPrefab;
    public float moveSpeed = 4.0f;
    
    Dictionary<int, bool> inputDevices = new Dictionary<int, bool>();
    Dictionary<int, GameObject> moveGameObjectsByDeviceId = new Dictionary<int, GameObject>();
    
    void OnInputDeviceConnect(int devHandle)
    {
        inputDevices[devHandle] = true;
    }

    void OnInputDeviceDisconnect(int devHandle)
    {
        inputDevices.Remove(devHandle);
        DestroyMoveObject(devHandle);
    }

    void Update()
    {
        Dictionary<int, Vector3> moveDeltaByDeviceId = new Dictionary<int, Vector3>();

        foreach (var pk in inputDevices)
        {
            var devHandle = pk.Key;

            Vector3[] pressed = new Vector3[4];
            if (MultiInput.GetKey(devHandle, "LEFT"))
            {
                pressed[0] = Vector3.left;
            }
            if (MultiInput.GetKey(devHandle, "RIGHT"))
            {
                pressed[1] = Vector3.right;
            }
            if (MultiInput.GetKey(devHandle, "UP"))
            {
                pressed[2] = Vector3.forward;
            }
            if (MultiInput.GetKey(devHandle, "DOWN"))
            {
                pressed[3] = Vector3.back;
            }
            moveDeltaByDeviceId[devHandle] = pressed[0] + pressed[1] + pressed[2] + pressed[3];
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
}
