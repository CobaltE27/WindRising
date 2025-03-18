using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ThermalSampler : WindSampler
{
	public float radius;
	public float cloudBaseHeight;
	public float speed = 0f;
	public AnimationCurve strengthByDistance;
	public Vector3 prevailingWind;

	void Start()
	{
		Vector3 horiWind = new Vector3(prevailingWind.x, 0, prevailingWind.z);
		CapsuleCollider collider = (CapsuleCollider)WindCollider;
		if (horiWind.magnitude != 0)
		{
			float slope = speed / horiWind.magnitude;
			float slopeAngle = Mathf.Atan(slope);
			collider.height = (cloudBaseHeight - transform.position.y) / Mathf.Sin(slopeAngle);
			transform.Rotate(90 - Mathf.Rad2Deg * slopeAngle, Vector3.SignedAngle(transform.forward, horiWind, Vector3.up), 0);
		}
		else
		{
			collider.height = cloudBaseHeight - transform.position.y;
		}
		collider.center = new Vector3(0, collider.height / 2, 0);
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
}
