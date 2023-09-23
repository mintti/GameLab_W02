using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Shooting : MonoBehaviour
{
	[SerializeField]
	private LayerMask mask;

	[SerializeField]
	private float damage;


	private void Update()
	{
		if (Input.GetMouseButtonDown(0))
		{
			Shoot();
		}
	}

	private void Shoot()
	{
		RaycastHit hit;
		Vector3 rayDirection = transform.forward;
		Debug.DrawRay(transform.position, rayDirection * Mathf.Infinity, Color.red); // 여기서 Color.red는 그려지는 Ray의 색상입니다.

		if (Physics.Raycast(transform.position, rayDirection, out hit, Mathf.Infinity, mask))
		{
			EnemyAI ai = hit.collider.GetComponent<EnemyAI>();
			if (ai != null)
			{
				ai.TakeDamage(damage);
			}
		}
	}
}
