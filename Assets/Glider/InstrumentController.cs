using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using TMPro;
using UnityEngine;

public class InstrumentController : MonoBehaviour
{
	public TMP_Text airspeed;
	public TMP_Text altimeter;
	public TMP_Text absoluteSpeed;
	public TMP_Text variometer;
	public TMP_Text ratio;
	public TMP_Text climb;
	public AudioSource varioSpeaker;
	public AnimationCurve varioVolCurve;
	public float varioPitchSensitivity = 0.7f;
	List<AudioClip> playlist;
	public string playlistPath;
	bool radioOn = false;
	public AudioSource radioSpeaker;
	// Start is called before the first frame update
	void Start()
    {
		playlist = Resources.LoadAll<AudioClip>(playlistPath).ToList();
    }

	private void Update()
	{
		if (Input.GetKeyDown(KeyCode.Backslash))
		{
			if (radioOn)
				radioSpeaker.mute = true;
			else
			{
				radioSpeaker.mute = false;
				NextRadio();
				radioSpeaker.Play();
			}

			radioOn = !radioOn;
		}

		if (radioOn && Input.GetKeyDown(KeyCode.RightBracket))
		{
			radioSpeaker.Pause();
			NextRadio();
			radioSpeaker.Play();
		}
	}

	private void NextRadio()
	{
		System.Random rng = new();
		radioSpeaker.clip = playlist[rng.Next(playlist.Count)];
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
	Vector3 oldVelocity = Vector3.zero;
	Vector3 oldPosition = Vector3.zero;
	bool hasOldVario = false;
	public void UpdateVariometer(Vector3 velocity, Vector3 position)
	{
		if (!hasOldVario)
		{
			oldVelocity = velocity;
			oldPosition = position;
			hasOldVario = true;
			return;
		}

		float deltaH = (position.y - oldPosition.y) / Time.deltaTime;
		float deltaV = (velocity.magnitude - oldVelocity.magnitude) / Time.deltaTime;
		//print("delH:" + deltaH + " delV:" + deltaV);
		float compensatedClimb = deltaH +  deltaV * Mathf.Abs(deltaV) / (2 * Physics.gravity.magnitude);//delta energy / mg
		oldVelocity = velocity;
		oldPosition = position;

		variometer.text = compensatedClimb.ToString("n2") + "\nm/s";
		climb.text = deltaH.ToString("n2") + " m/s";
		if (compensatedClimb >= 0)
		{
			float normedSub = (1 - Mathf.Min(1, compensatedClimb / 5)) * 0.5f;
			variometer.color = new Color(normedSub, 1, normedSub, 1);
			varioSpeaker.pitch = 1 + compensatedClimb * varioPitchSensitivity;
		}
		else
		{
			float normedSub = (1 - Mathf.Min(1, -compensatedClimb / 5)) * 0.5f;
			variometer.color = new Color(1, normedSub, normedSub, 1);
			varioSpeaker.pitch = 1;
		}
		varioSpeaker.volume = varioVolCurve.Evaluate(compensatedClimb / 8f); //6 m/s is typically a really good climb
	}

	Vector3 oldPos = Vector3.zero;
	float highest = 0f;
	float distanceFromHighest = 0f;
	bool hasOldPos = false;
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
