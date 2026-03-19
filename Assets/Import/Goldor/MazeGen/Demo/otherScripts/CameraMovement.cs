using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraMovement : MonoBehaviour
{
    public float speed = 20;
    public float sensitivity = 1;
    public float speedBoost = 10;
    public float superSpeedBoost = 100;
    
    void LateUpdate()
    {
        Vector3 move = new Vector3();
        if (Input.GetKey(KeyCode.Z))
        {
            move.z++;
        }
        if (Input.GetKey(KeyCode.S))
        {
            move.z--;
        }
        if (Input.GetKey(KeyCode.D))
        {
            move.x++;
        }
        if (Input.GetKey(KeyCode.Q))
        {
            move.x--;
        }
        if (Input.GetKey(KeyCode.Space))
        {
            move.y++;
        }
        if (Input.GetKey(KeyCode.LeftShift))
        {
            move.y--;
        }
        
        move *= speed * Time.deltaTime;

        if (Input.GetKey(KeyCode.LeftAlt))
        {
            move *= speedBoost;
        }

        if (Input.GetKey(KeyCode.LeftControl))
        {
            move *= superSpeedBoost;
        }


        transform.Translate(move,transform);

        float rotationX = -Input.GetAxis("Mouse Y") * sensitivity;
        float rotationY = Input.GetAxis("Mouse X") * sensitivity;
        
        var rot = transform.rotation.eulerAngles;
        transform.rotation = Quaternion.Euler(rot.x + rotationX, rot.y + rotationY, 0);
    }
}
