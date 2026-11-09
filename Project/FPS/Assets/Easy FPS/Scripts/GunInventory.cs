using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public enum MenuStyle{
	horizontal,vertical
}

public class GunInventory : MonoBehaviour {
	[Tooltip("Current weapon gameObject.")]
	public GameObject currentGun;
	private Animator currentHAndsAnimator;
	private int currentGunCounter = 0;

	[Tooltip("Put Strings of weapon objects from Resources Folder.")]
	public List<string> gunsIHave = new List<string>();
	[Tooltip("Icons from weapons.(Fetched when you run the game)*MUST HAVE ICONS WITH CORRESPONDING NAMES IN RESOUCES FOLDER*")]
	public Texture[] icons;

	[HideInInspector]
	public float switchWeaponCooldown;

	private Player playerComponent;

	public int CurrentGunCounter => currentGunCounter;

	void Awake(){
		playerComponent = GetComponent<Player>();
	}

	public void Initialize()
	{
		if (!playerComponent)
			playerComponent = GetComponent<Player>();

		UpdateIconsFromResouces();
        StartCoroutine(Spawn(currentGunCounter));

        if (gunsIHave.Count == 0)
            print("No guns in the inventory");
    }

	IEnumerator SpawnWeaponUponStart(){
		yield return new WaitForSeconds (0.5f);
		StartCoroutine(Spawn(0));
	}

	void Update(){
		switchWeaponCooldown += 1 * Time.deltaTime;
	}

	public void SwitchWeapon(int index){
		if (gunsIHave == null || gunsIHave.Count == 0) return;
		if (index < 0 || index >= gunsIHave.Count) return;
		if (currentGunCounter == index && currentGun != null) return;
		if (switchWeaponCooldown < 0.5f) return;

		switchWeaponCooldown = 0;
		currentGunCounter = index;
		StartCoroutine(Spawn(currentGunCounter));
	}

	public void NextWeapon(){
		if (gunsIHave == null || gunsIHave.Count == 0) return;
		int next = (currentGunCounter + 1) % gunsIHave.Count;
		SwitchWeapon(next);
	}

	IEnumerator UpdateIconsFromResources(){
		yield return new WaitForEndOfFrame ();
		UpdateIconsFromResouces();
    }

	void UpdateIconsFromResouces()
	{
        icons = new Texture[gunsIHave.Count];
        for (int i = 0; i < gunsIHave.Count; i++)
        {
            icons[i] = (Texture)Resources.Load("Weap_Icons/" + gunsIHave[i].ToString() + "_img");
        }
    }

	/*
	 * This method is called upon pressing weapon change,
	 * It will check if we carry a gun and destroy it, and its then going to load a gun prefab from our Resources Folder.
	 */
	IEnumerator Spawn(int _redniBroj){
		if (weaponChanging)
			weaponChanging.Play ();

		if(currentGun){
			if(currentGun.name.Contains("Gun")){
				if (currentHAndsAnimator)
					currentHAndsAnimator.SetBool("changingWeapon", true);

				yield return new WaitForSeconds(0.4f);
				Destroy(currentGun);

				GameObject resource = (GameObject) Resources.Load(gunsIHave[_redniBroj].ToString());
				currentGun = (GameObject) Instantiate(resource, transform.position, Quaternion.identity);
				currentGun.gameObject.name = currentGun.name + "(Clone)_" + this.gameObject.name;
                AssignHandsAnimator(currentGun);
			}
			else if(currentGun.name.Contains("Sword")){
				if (currentHAndsAnimator)
					currentHAndsAnimator.SetBool("changingWeapon", true);
				yield return new WaitForSeconds(0.25f);

				if (currentHAndsAnimator)
					currentHAndsAnimator.SetBool("changingWeapon", false);

				yield return new WaitForSeconds(0.4f);
				Destroy(currentGun);

				GameObject resource = (GameObject) Resources.Load(gunsIHave[_redniBroj].ToString());
				currentGun = (GameObject) Instantiate(resource, transform.position, Quaternion.identity);
				AssignHandsAnimator(currentGun);
            }
		}
		else{
			GameObject resource = (GameObject) Resources.Load(gunsIHave[_redniBroj].ToString());
			currentGun = (GameObject) Instantiate(resource, transform.position, Quaternion.identity);
            AssignHandsAnimator(currentGun);
        }

		// 무기 생성 완료 후 Player에 의존성 갱신 알림
		if (playerComponent)
		{
			playerComponent.GunChangeSetUpdate(
				playerComponent.isPlayer ? GameManager.instance?.MainCamera : null,
				playerComponent.isPlayer ? GameManager.instance?.SecondCamera : null
			);
		}
	}

	void AssignHandsAnimator(GameObject _currentGun){
		if(_currentGun.name.Contains("Gun")){
			GunScript gs = currentGun.GetComponent<GunScript>();
			if (gs) currentHAndsAnimator = gs.handsAnimator;
		}
	}

	void OnGUI(){
		if (playerComponent != null && !playerComponent.isPlayer) return;

		if (currentGun && gunsIHave != null && gunsIHave.Count > 0)
		{
			if (icons == null || icons.Length != gunsIHave.Count)
			{
				UpdateIconsFromResouces();
			}

			for (int i = 0; i < gunsIHave.Count; i++)
			{
				DrawCorrespondingImage(i);
			}
		}
	}

	[Header("GUI Gun preview variables")]
	[Tooltip("Weapon icons style to pick.")]
	public MenuStyle menuStyle = MenuStyle.horizontal;
	[Tooltip("Spacing between icons.")]
	public int spacing = 10;
	[Tooltip("Begin position in percetanges of screen.")]
	public Vector2 beginPosition;
	[Tooltip("Size of icon in percetanges of screen.")]
	public Vector2 size;

	void DrawCorrespondingImage(int _number){
		if (icons == null || _number >= icons.Length || icons[_number] == null)
			return;

		if (_number >= gunsIHave.Count)
			return;

		bool isCurrentSelected = currentGun.name.Contains(gunsIHave[_number]);

		if(menuStyle == MenuStyle.horizontal){
			if(isCurrentSelected){
				GUI.DrawTexture(new Rect(vec2(beginPosition).x +(_number*position_x(spacing)),vec2(beginPosition).y,
					vec2(size).x, vec2(size).y),
					icons[_number]);
			}
			else{			
				GUI.DrawTexture(new Rect(vec2(beginPosition).x +(_number*position_x(spacing) + 10),vec2(beginPosition).y + 10,
					vec2(size).x - 20, vec2(size).y- 20),
					icons[_number]);
			}
		}
		else if(menuStyle == MenuStyle.vertical){
			if(isCurrentSelected){
				GUI.DrawTexture(new Rect(vec2(beginPosition).x,vec2(beginPosition).y +(_number*position_y(spacing)),
					vec2(size).x, vec2(size).y),
					icons[_number]);
			}
			else{			
				GUI.DrawTexture(new Rect(vec2(beginPosition).x,vec2(beginPosition).y + 10  +(_number*position_y(spacing)),
					vec2(size).x - 20, vec2(size).y- 20),
					icons[_number]);
			}
		}
	}

	public void DeadMethod(){
		Destroy (currentGun);
		Destroy (this);
	}

	private float position_x(float var){
		return Screen.width * var / 100;
	}
	private float position_y(float var)
	{
		return Screen.height * var / 100;
	}
	private Vector2 vec2(Vector2 _vec2){
		return new Vector2(Screen.width * _vec2.x / 100, Screen.height * _vec2.y / 100);
	}

	[Header("Sounds")]
	[Tooltip("Sound of weapon changing.")]
	public AudioSource weaponChanging;
}
