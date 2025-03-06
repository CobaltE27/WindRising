using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class WindSampler : MonoBehaviour
{
	public Collider WindCollider;

	/// <summary>
	/// Samples the wind velocity at the given position in the wind zone.
	/// </summary>
	/// <param name="position"></param>
	/// <returns></returns>
	public abstract Vector3 WindAt(Vector3 position);
}
