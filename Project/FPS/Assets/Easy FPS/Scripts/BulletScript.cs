using UnityEngine;
using System.Collections;

public class BulletScript : MonoBehaviour {

	[Tooltip("Furthest distance bullet will look for target")]
	public float maxDistance = 1000000;
	RaycastHit hit;
	[Tooltip("Prefab of wall damange hit. The object needs 'LevelPart' tag to create decal on it.")]
	public GameObject decalHitWall;
	[Tooltip("Decal will need to be sligtly infront of the wall so it doesnt cause rendeing problems so for best feel put from 0.01-0.1.")]
	public float floatInfrontOfWall;
	[Tooltip("Blood prefab particle this bullet will create upoon hitting enemy")]
	public GameObject bloodEffect;
	[Tooltip("Put Weapon layer and Player layer to ignore bullet raycast.")]
	public LayerMask ignoreLayer;

    private void Start()
    {
		Debug.Log($"{gameObject.name}.Start()");
    }
	/*
	* Uppon bullet creation with this script attatched,
	* bullet creates a raycast which searches for corresponding tags.
	* If raycast finds somethig it will create a decal of corresponding tag.
	*/
	Rigidbody rigidBodyHit;

    void Update () {
      

        if (Physics.Raycast(transform.position, transform.forward,out hit, maxDistance, ~ignoreLayer)){
			if(decalHitWall){
				string name = "empty";
				if(hit.transform.tag == "LevelPart"){
					name = Instantiate(decalHitWall, hit.point + hit.normal * floatInfrontOfWall, Quaternion.LookRotation(hit.normal)).name;
					Destroy(gameObject);
				}
				if(hit.transform.tag == "Dummie"){
                    name = Instantiate(bloodEffect, hit.point, Quaternion.LookRotation(hit.normal)).name;
					Destroy(gameObject);
				}
				if(hit.transform.tag == "PhysicObject")
				{
					Rigidbody rigidBodyHit = hit.transform.GetComponent<Rigidbody>();

                    RigidBodyPhysic(rigidBodyHit, transform.forward, 100);

                    Debug.Log($"BulletScript: {hit.transform.gameObject.name}");
				}
				Debug.Log($"{name}");
			}		
			Destroy(gameObject);
		}
		Destroy(gameObject, 0.1f);


	}


	void RigidBodyPhysic(Rigidbody rigidbody, Vector3 vShotDir, float power)
	{
		if (rigidbody == null)
		{
			Debug.Log("RigidBodyPhysic is null");
			return;
        }
		rigidbody.AddForce(vShotDir * power);
	}

}
