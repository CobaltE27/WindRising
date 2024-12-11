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
	public float elevatorStrength = 0f;
    public float controlDragCoeff;
	public float glideCoeff;
    public GameObject rightTip;
    public GameObject leftTip;
	public Vector3 facing = Vector3.forward;
    private Resistance resCoeffs = new Resistance(0.01f, 0.3f, 0.5f, 1.0f);
    private Vector3 gravity = Vector3.down * 9.8f;
    public Rigidbody rb;
    public InstrumentController instruments;
    private float ratioTimer = 20f; //not timer duration
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
        float forwardAirspeedSquared = Vector3.Dot(rb.transform.forward, -airDir) * airSpeedSquared;
		Vector3 accum = rb.GetAccumulatedForce(); //initialize forces, should basically always be 0
                                                  //Debug.Log(accum);
        ratioTimer += Time.deltaTime;
        if (ratioTimer > 20) //20s since last sample
        {
            ratioTimer -= 20;
			UpdateInstruments(rb.position, vel, Mathf.Sqrt(forwardAirspeedSquared), true);
		}
        else
		    UpdateInstruments(rb.position, vel, Mathf.Sqrt(forwardAirspeedSquared), false);

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

        Vector3 wingOffset = -0.2f * rb.transform.forward;
		Vector3 rightLift = rb.transform.up * (forwardAirspeedSquared * (0.5f * wingAreaFactor) * liftCoeff); //lift proportional to wing area, aerofoil lift coeff, and forward airspeed
        float rAileronForce = rAileronPos * forwardAirspeedSquared * aileronStrength; //account for aileron lift
		rightLift += rb.transform.up * rAileronForce; 
		rb.AddForceAtPosition(rightLift, rightTip.transform.position + wingOffset); //wing center of lift is generally behind
		Vector3 leftLift = rb.transform.up * (forwardAirspeedSquared * (0.5f * wingAreaFactor) * liftCoeff);
		float lAileronForce = lAileronPos * forwardAirspeedSquared * aileronStrength;
		leftLift += rb.transform.up * lAileronForce;
		rb.AddForceAtPosition(leftLift, leftTip.transform.position + wingOffset);
		Debug.DrawRay(rightTip.transform.position, rightLift, Color.cyan);
		Debug.DrawRay(leftTip.transform.position, leftLift, Color.cyan);
        rb.AddForceAtPosition(-rb.transform.forward * rAileronForce * controlDragCoeff, rightTip.transform.position + wingOffset);
        rb.AddForceAtPosition(-rb.transform.forward * lAileronForce * controlDragCoeff, leftTip.transform.position + wingOffset);

		Vector3 tailBias = -rb.transform.up * liftCoeff * tailBiasFactor * forwardAirspeedSquared;
        Vector3 centeringDir = (airVel - (-rb.transform.forward * airVel.magnitude * (Vector3.Dot(airVel.normalized, -rb.transform.forward)))).normalized;
		Vector3 centeringRotation = tailBias + centeringDir * forwardAirspeedSquared * tailCoeff;
        centeringRotation += rb.transform.up * forwardAirspeedSquared * tailElevatorPos * elevatorStrength;
        rb.AddForceAtPosition(centeringRotation, rb.position - 5 * rb.transform.forward); //added on the back of the craft like force from tail/rudder, just removing actual backward drag component
        Debug.DrawRay(rb.position - 5 * rb.transform.forward, centeringRotation);
		Debug.DrawRay(rb.position, -5 * rb.transform.forward);

		Debug.DrawRay(rb.position, accum, Color.black);
		rb.AddForce(accum);
    }

    void InterpretControls() //Use WASD to control elevator and ailerons
    {
        //targets are in terms of upward force applied to part of craft, not actual angle of flaps
        //aileron control
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
        float eTarget = 0f;
		if (Input.GetKey(KeyCode.W))
		{
            eTarget += 0.5f;
		}
		if (Input.GetKey(KeyCode.S))
		{
			eTarget -= 0.5f;
		}
		tailElevatorPos = tailElevatorPos + (eTarget - tailElevatorPos) * 0.1f;
	}

    void UpdateInstruments(Vector3 position, Vector3 velocity, float airspeed, bool ratioSample)
    {
        instruments.UpdateAirspeed(airspeed);
        instruments.UpdateAbsoluteSpeed(velocity.magnitude);
        instruments.UpdateVariometer(velocity.y);
        instruments.UpdateAltimeter(position.y);
        if (ratioSample)
            instruments.UpdateRatio(position);
	}
}
