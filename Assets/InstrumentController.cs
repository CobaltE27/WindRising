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
	bool hasOldPos = false;
	// Start is called before the first frame update
	void Start()
    {
        
    }

    public void UpdateAirspeed(float value)
    {
		airspeed.text = value + "m/s";
    }

	public void UpdateAltimeter(float value)
	{
		altimeter.text = value + "m";
	}

	public void UpdateAbsoluteSpeed(float value)
	{
		absoluteSpeed.text = value + "m/s";
	}

	//provide value as m/s
	public void UpdateVariometer(float value)
	{
		float displayValue = value * 60;
		variometer.text = displayValue + "m/min";
		if (displayValue >= 0)
		{
			float normedSub = 1 - Mathf.Min(1, value / 5);
			variometer.color = new Color(normedSub, 1, normedSub, 1);		
		}
		else
		{
			float normedSub = 1 - Mathf.Min(1, -value / 5);
			variometer.color = new Color(1, normedSub, normedSub, 1);
		}
	}

	public void UpdateRatio(Vector3 position)
	{
		if (hasOldPos)
		{
			Vector3 displacement = position - oldPos;
			bool gain = true;
			float delY = displacement.y;
			if (delY < 0)
			{
				delY *= -1;
				gain = false;
			}
			if (gain)
			{
				ratio.text = "+:1";
				return;
			}
			displacement.y = 0;
			int delH = (int)displacement.magnitude;

			float rate = delH / delY;
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
