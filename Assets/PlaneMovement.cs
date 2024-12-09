using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlaneMovement : MonoBehaviour
{
    public struct Resistance
    {
        public Resistance(float forw, float bac, float sid, float topBot)
        {
            forward = forw;
            back = bac;
            sides = sid;
            topBottom = topBot;
        }

        public float forward { get; }
        public float back { get; }
        public float sides { get; }
		public float topBottom { get; }
	}

	// Start is called before the first frame update
    public float wingAreaFactor = 0f;
    public float tailCoeff = 0f;
    public float tailBiasFactor = 0f;
    public float liftCoeff = 0f;
    public float lAileronPos = 0f;
    public float rAileronPos = 0f;
    public float tailElevatorPos = 0f;
	public float aileronStrength = 0f;
	public float glideCoeff;
    public GameObject rightTip;
    public GameObject leftTip;
	public Vector3 facing = Vector3.forward;
    private Resistance resCoeffs = new Resistance(0.1f, 0.3f, 0.5f, 2.0f);
    private Vector3 gravity = Vector3.down * 9.8f;
    public Rigidbody rb;
    void Start()
    {
        rb.velocity = rb.transform.forward * 20;
    }

    // Update is called once per frame
    void FixedUpdate()
    {
        InterpretControls();

		Debug.DrawLine(rb.position, rightTip.transform.position);
		Debug.DrawLine(rb.position, leftTip.transform.position);

		Vector3 vel = rb.velocity;
        Vector3 facing = rb.transform.forward;
        Vector3 staticWind = -3 * Vector3.forward; //sample from weather sim
        Vector3 airVel = -vel + staticWind; //account for windspeed and travel through air
		Debug.DrawRay(rb.position, airVel, Color.green);
		Vector3 airDir = airVel.normalized;
        float airSpeedSquared = airVel.sqrMagnitude;
		Vector3 accum = rb.GetAccumulatedForce(); //initialize forces, should basically always be 0
        //Debug.Log(accum);

        Debug.DrawRay(rb.position, facing);
		accum += gravity * rb.mass; //apply gravity

        Vector3 drag = Vector3.zero;
        Dictionary<Vector3, float> coeffs = new Dictionary<Vector3, float>();
        coeffs.Add(rb.transform.forward, resCoeffs.forward);
		coeffs.Add(-rb.transform.forward, resCoeffs.back);
		coeffs.Add(rb.transform.up, resCoeffs.topBottom);
		coeffs.Add(-rb.transform.up, resCoeffs.topBottom);
		coeffs.Add(rb.transform.right, resCoeffs.sides);
		coeffs.Add(-rb.transform.right, resCoeffs.sides);
        foreach (KeyValuePair<Vector3, float> dir in coeffs)
        {
            float relevence = Vector3.Dot(dir.Key, -airDir); //how much this side of the aircraft is facing the wind
            if (relevence <= 0) //this side faces away from the wind
                continue;
            drag += -dir.Key * (airSpeedSquared * relevence * dir.Value); //drag proportinal to airspeed squared
        }
        accum += drag;
		Debug.DrawRay(rb.position, drag, Color.yellow);
		Debug.Log("drag force: " + drag);

		Vector3 glideForce = facing * (Mathf.Abs(Vector3.Dot(rb.transform.up, drag.normalized)) * drag.magnitude) * glideCoeff; //glideforce proportional to upward drag force
		accum += glideForce;
		Debug.DrawRay(rb.position, glideForce, Color.magenta);

        Vector3 colOffset = -0.2f * rb.transform.forward;
		Vector3 rightLift = rb.transform.up * (Vector3.Dot(rb.transform.forward, -airDir) * (0.5f * wingAreaFactor) * liftCoeff * airSpeedSquared); //lift proportional to wing area, aerofoil lift coeff, and forward airspeed
        rightLift += rb.transform.up * rAileronPos * airSpeedSquared * aileronStrength;
		rb.AddForceAtPosition(rightLift, rightTip.transform.position + colOffset); //wing center of lift is generally behind
		Vector3 leftLift = rb.transform.up * (Vector3.Dot(rb.transform.forward, -airDir) * (0.5f * wingAreaFactor) * liftCoeff * airSpeedSquared);
		leftLift += rb.transform.up * lAileronPos * airSpeedSquared * aileronStrength;
		rb.AddForceAtPosition(leftLift, leftTip.transform.position + colOffset);
		Debug.DrawRay(rightTip.transform.position, rightLift, Color.cyan);
		Debug.DrawRay(leftTip.transform.position, leftLift, Color.cyan);

		Vector3 tailBias = -rb.transform.up * liftCoeff * tailBiasFactor * airSpeedSquared;
        Vector3 centeringDir = (airVel - (-rb.transform.forward * airVel.magnitude * (Vector3.Dot(airVel.normalized, -rb.transform.forward)))).normalized;
		Vector3 centeringRotation = tailBias + centeringDir * airSpeedSquared * tailCoeff;
        rb.AddForceAtPosition(centeringRotation, rb.position - 5 * rb.transform.forward); //added on the back of the craft like force from tail/rudder, just removing actual backward drag component
        Debug.DrawRay(rb.position - 5 * rb.transform.forward, centeringRotation);
		Debug.DrawRay(rb.position, -5 * rb.transform.forward);

		Debug.DrawRay(rb.position, accum, Color.black);
		rb.AddForce(accum);
    }

    void InterpretControls()
    {
		float lTarget = 0f;
		float rTarget = 0f;
		if (Input.GetKey(KeyCode.A))
        {
            lTarget -= 0.5f;
            rTarget += 0.5f;
        }
		if (Input.GetKey(KeyCode.D))
		{
			lTarget += 0.5f;
			rTarget -= 0.5f;
		}
        lAileronPos = lAileronPos + (lTarget - lAileronPos) * 0.1f; //lerp toward target values
        rAileronPos = rAileronPos + (rTarget - rAileronPos) * 0.1f;

        //elevator control
	}
}
