using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ThermalSampler : WindSampler
{
	public float radius;
	public float speed = 0f;
	/// <summary>
	/// Samples the wind velocity at the given position in the wind zone.
	/// </summary>
	/// <param name="position"></param>
	/// <returns></returns>
	public override Vector3 WindAt(Vector3 position)
	{
		//TODO: update to account for sloped colliders
		Vector2 horizontalDisp = new Vector2(position.x - transform.position.x, position.z - transform.position.z);
		if (horizontalDisp.magnitude > radius)
			return Vector3.zero;
		return Vector3.up * speed;
	}
}
