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
    }

	Rigidbody rigidBodyHit;

    void Update () {
        Vector3 rayOrigin = transform.position + transform.forward * 0.1f;

        if (Physics.Raycast(rayOrigin, transform.forward, out hit, maxDistance, ~ignoreLayer)){
			if(decalHitWall){
				if(hit.transform.CompareTag("LevelPart")){
					Instantiate(decalHitWall, hit.point + hit.normal * floatInfrontOfWall, Quaternion.LookRotation(hit.normal));
					Destroy(gameObject);
					return;
				}
				if(hit.transform.CompareTag("Dummie") || hit.transform.CompareTag("Player")){
                    if (bloodEffect)
						Instantiate(bloodEffect, hit.point, Quaternion.LookRotation(hit.normal));
					Destroy(gameObject);
					return;
				}
				if(hit.transform.CompareTag("PhysicObject"))
				{
					Rigidbody rbHit = hit.transform.GetComponent<Rigidbody>();
                    RigidBodyPhysic(rbHit, transform.forward, 100);
				}
			}		
			Destroy(gameObject);
		}
		Destroy(gameObject, 0.1f);
	}

	void RigidBodyPhysic(Rigidbody rigidbody, Vector3 vShotDir, float power)
	{
		if (rigidbody == null)
		{
			return;
        }
		rigidbody.AddForce(vShotDir * power);
	}
}
