using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public enum MenuStyle{
	horizontal,vertical
}

public class GunInventory : MonoBehaviour {
	[Tooltip("Current weapon gameObject (Synced from Player).")]
	public GameObject currentGun;
	private int currentGunCounter = 0;

	[Tooltip("Put Strings of weapon objects from Resources Folder.")]
	public List<string> gunsIHave = new List<string>();
	[Tooltip("Icons from weapons.(Fetched when you run the game)*MUST HAVE ICONS WITH CORRESPONDING NAMES IN RESOUCES FOLDER*")]
	public Texture[] icons;

	[HideInInspector]
	public float switchWeaponCooldown;

	private Player playerComponent;

	public int CurrentGunCounter => playerComponent != null ? playerComponent.CurrentWeaponIndex : currentGunCounter;

	void Awake(){
		playerComponent = GetComponent<Player>();
	}

	public void Initialize()
	{
		if (!playerComponent)
			playerComponent = GetComponent<Player>();

		UpdateIconsFromResouces();

		if (playerComponent && playerComponent.Gun)
		{
			currentGunCounter = playerComponent.CurrentWeaponIndex;
			currentGun = playerComponent.Gun.gameObject;
		}
	}

	/// <summary>
	/// Player(마리오네트)에서 무기가 전환될 때 HUD 표시용 참조를 동기화합니다.
	/// </summary>
	public void SyncWeaponState(int weaponIndex, GameObject activeGunObj)
	{
		currentGunCounter = weaponIndex;
		currentGun = activeGunObj;
	}

	void Update(){
		switchWeaponCooldown += 1 * Time.deltaTime;
	}

	public void SwitchWeapon(int index){
		if (!playerComponent)
			playerComponent = GetComponent<Player>();

		if (playerComponent)
		{
			playerComponent.SwitchWeapon(index);
		}
	}

	public void NextWeapon(){
		if (!playerComponent)
			playerComponent = GetComponent<Player>();

		if (playerComponent)
		{
			playerComponent.NextWeapon();
		}
	}

	void UpdateIconsFromResouces()
	{
		if (gunsIHave == null) return;

		icons = new Texture[gunsIHave.Count];
		for (int i = 0; i < gunsIHave.Count; i++)
		{
			icons[i] = (Texture)Resources.Load("Weap_Icons/" + gunsIHave[i].ToString() + "_img");
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

		bool isCurrentSelected = (CurrentGunCounter == _number);

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
