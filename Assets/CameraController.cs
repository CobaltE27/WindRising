using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraController : MonoBehaviour
{
    Vector3 rotation;
    float speed = 3f;
    // Start is called before the first frame update
    void Start()
    {
        rotation = Vector2.zero;
    }

    // Update is called once per frame
    void Update()
    {
		rotation.y += Input.GetAxis("Mouse X");
		rotation.x += -Input.GetAxis("Mouse Y");
		transform.eulerAngles = transform.parent.transform.eulerAngles + rotation * speed;
	}
}
