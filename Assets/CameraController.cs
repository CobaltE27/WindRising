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
        //Transform craftTrans = transform.parent.transform;
		rotation.y += Input.GetAxis("Mouse X");
		rotation.x += -Input.GetAxis("Mouse Y");
		rotation.y = Mathf.Clamp(rotation.y, -60f, 60f);
		rotation.x = Mathf.Clamp(rotation.x, -30f, 30f);
		Debug.Log(rotation);
		transform.localEulerAngles = rotation * speed;
	}
}
