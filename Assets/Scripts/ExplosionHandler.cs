using System;
using System.Collections;
using UnityEngine;

public class ExplosionHandler : MonoBehaviour
{
	void Start() 
    {
		StartCoroutine(KillRoutine());
	}

	IEnumerator KillRoutine()
	{
		yield return new WaitForSeconds(5f);
		Destroy(this.gameObject);
	}
}
