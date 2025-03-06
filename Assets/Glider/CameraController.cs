using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraController : MonoBehaviour
{
    Vector3 rotation;
    Vector3 lastVel = Vector3.zero;
    public Rigidbody rb;
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
        rotation.y = Mathf.Clamp(rotation.y, -60f, 60f);
        rotation.x = Mathf.Clamp(rotation.x, -30f, 30f);
		transform.localEulerAngles = rotation * speed;
	}

    void FixedUpdate()
    {
		Vector3 velChange = rb.velocity - lastVel;
        velChange = velChange.normalized * (Mathf.Min(1f, velChange.magnitude) * 0.5f);
		transform.localPosition = Vector3.Lerp(transform.localPosition, -velChange, 0.1f);
		lastVel = rb.velocity;
	}
}
