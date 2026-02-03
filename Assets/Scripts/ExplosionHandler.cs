using System;
using UnityEngine;

public class ExplosionHandler : MonoBehaviour
{
	void Start() 
    { 
	
	}

	IEnumerator KillRoutine()
	{
		yield return new WaitForSeconds(5f);
		Destroy(this.gameObject);
	}
}
