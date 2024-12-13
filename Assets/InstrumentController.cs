using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class InstrumentController : MonoBehaviour
{
	public TMP_Text airspeed;
	public TMP_Text altimeter;
	public TMP_Text absoluteSpeed;
	public TMP_Text variometer;
	public TMP_Text ratio;
	Vector3 oldPos = Vector3.zero;
	float highest = 0f;
	float distanceFromHighest = 0f;
	bool hasOldPos = false;
	// Start is called before the first frame update
	void Start()
    {
        
    }

    public void UpdateAirspeed(float value)
    {
		airspeed.text = value.ToString("n1") + "\nm/s";
    }

	public void UpdateAltimeter(float value)
	{
		altimeter.text = MathF.Truncate(value) + "\nm";
	}

	public void UpdateAbsoluteSpeed(float value)
	{
		absoluteSpeed.text = value.ToString("n1") + "\nm/s";
	}

	//provide value as m/s
	public void UpdateVariometer(float value)
	{
		float displayValue = value * 60;
		variometer.text = MathF.Truncate(displayValue) + "\nm/min";
		if (displayValue >= 0)
		{
			float normedSub = (1 - Mathf.Min(1, value / 5)) * 0.5f;
			variometer.color = new Color(normedSub, 1, normedSub, 1);		
		}
		else
		{
			float normedSub = (1 - Mathf.Min(1, -value / 5)) * 0.5f;
			variometer.color = new Color(1, normedSub, normedSub, 1);
		}
	}

	public void UpdateRatio(Vector3 position)
	{
		if (position.y > highest)
		{
			highest = position.y;
			distanceFromHighest = 0f;
			ratio.text = "+:1";
			oldPos = position;
			return;
		}

		if (hasOldPos)
		{
			Vector3 displacement = position - oldPos;
			oldPos = position;
			displacement.y = 0;
			distanceFromHighest += displacement.magnitude;
			int delH = (int)displacement.magnitude;

			float rate = distanceFromHighest / (highest - position.y);
			if (rate >= 1)
				ratio.text = (int)rate + ":1";
			else
				ratio.text = "1:" + (int)(1 / rate);
		}
		else
		{
			hasOldPos = true;
			oldPos = position;
			ratio.text = "-:-";
		}
	}
}
