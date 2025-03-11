using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WindNoiseController : MonoBehaviour
{
    public AnimationCurve windPitch;
    public AnimationCurve windVolume;
    public AudioSource windSpeaker;
    public void SetWindNoise(Vector3 airVel)
    {
        transform.position = airVel.normalized;
        windSpeaker.volume = windVolume.Evaluate(airVel.magnitude / 100f) * 0.5f;
        windSpeaker.pitch = 1f + windPitch.Evaluate((airVel.magnitude - 30) / 100f);
	}
}
