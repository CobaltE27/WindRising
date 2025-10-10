using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ThermalSampler : WindSampler
{
	public float radius;
	public float cloudBaseHeight;
	public float speed = 0f;
	public float idealSpeed = 0f;
	Vector3 idealScale;
	public AnimationCurve strengthByDistance;
	public Vector3 prevailingWind;
	public Transform cloud;
	public bool dying;

	void Start()
	{
		Vector3 horiWind = new Vector3(prevailingWind.x, 0, prevailingWind.z);
		CapsuleCollider collider = (CapsuleCollider)WindCollider;
		float length = (cloudBaseHeight - transform.position.y) + 2 * collider.radius;
		if (horiWind.magnitude != 0)
		{
			float slope = idealSpeed / horiWind.magnitude;
			float slopeAngle = Mathf.Atan(slope);
			collider.height = length / Mathf.Sin(slopeAngle);
			transform.Rotate(90 - Mathf.Rad2Deg * slopeAngle, Vector3.SignedAngle(transform.forward, horiWind, Vector3.up), 0);
		}
		else
		{
			collider.height = length;
		}
		collider.center = new Vector3(0, collider.height / 2 - collider.radius, 0);
		cloud.localPosition = Vector3.up * (collider.height - collider.radius);
		cloud.localScale = Vector3.zero;
		idealScale = new Vector3(collider.radius * 2, collider.radius * 0.75f, collider.radius * 2);
		dying = false;
		SetSpeed(idealSpeed);
	}

	/// <summary>
	/// Samples the wind velocity at the given position in the wind zone.
	/// </summary>
	/// <param name="position"></param>
	/// <returns></returns>
	public override Vector3 WindAt(Vector3 position)
	{
		Vector3 dir = (prevailingWind + Vector3.up * speed).normalized;
		Vector3 centerToPoint = position - transform.position;
		Vector3 closestPoint = transform.position + dir * Vector3.Dot(centerToPoint, dir);
		float horizontalDist = Vector3.Distance(closestPoint, position);
		return Vector3.up * speed * strengthByDistance.Evaluate(horizontalDist / radius);
	}

	public void SetSpeed(float newSpeed, float duration = 20f)
	{
		StartCoroutine(AdjustSpeedTo(newSpeed, duration));
	}

	IEnumerator AdjustSpeedTo(float newSpeed, float duration)
	{
		float oldSpeed = speed;
		float framesNeeded = duration / Time.fixedDeltaTime;
		float iterSpeedDelta = (newSpeed - oldSpeed) / framesNeeded;
		Vector3 finalScale = idealScale;
		finalScale.Scale(new Vector3(1, newSpeed / 10, 1));
		if (newSpeed <= 0f)
			finalScale = Vector3.zero;
		Vector3 iterScaleDelta = (finalScale - cloud.localScale) / framesNeeded;
		for (int c = 0; c < framesNeeded; c++)
		{
			speed += iterSpeedDelta;
			cloud.localScale += iterScaleDelta;
			yield return new WaitForFixedUpdate();
		}
		speed = newSpeed;
		cloud.localScale = finalScale;
	}
}
