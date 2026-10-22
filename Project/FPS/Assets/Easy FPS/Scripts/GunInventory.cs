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

	/// <summary>
	/// 현재 장착된 무기의 GunScript 컴포넌트 참조입니다.
	/// </summary>
	public GunScript CurrentGunScript { get; private set; }

	[Tooltip("Put Strings of weapon objects from Resources Folder.")]
	public List<string> gunsIHave = new List<string>();
	[Tooltip("Icons from weapons.(Fetched when you run the game)*MUST HAVE ICONS WITH CORRESPONDING NAMES IN RESOUCES FOLDER*")]
	public Texture[] icons;

	[HideInInspector]
	public float switchWeaponCooldown;

	/*
	 * Calling the method that will update the icons of our guns if we carry any upon start.
	 * Also will spawn a weapon upon start.
	 */
	void Awake(){
		StartCoroutine("UpdateIconsFromResources");

		StartCoroutine ("SpawnWeaponUponStart");//to start with a gun

		if (gunsIHave.Count == 0)
			print ("No guns in the inventory");
	}

	/*
	*Waits some time then calls for a waepon spawn
	*/
	IEnumerator SpawnWeaponUponStart(){
		yield return new WaitForSeconds (0.5f);
		StartCoroutine("Spawn",0);
	}

	/* 
	 * Calculation switchWeaponCoolDown so it does not allow us to change weapons millions of times per second,
	 * and at some point we will change the switchWeaponCoolDown to a negative value so we have to wait until it
	 * overcomes 0.0f. 
	 */
	void Update(){
		switchWeaponCooldown += 1 * Time.deltaTime;
	}

	/*
	 * Grabing the icons from the Resources/Weapo_Icons/ -> gun name of the image.
	 */
	IEnumerator UpdateIconsFromResources(){
		yield return new WaitForEndOfFrame ();

		icons = new Texture[gunsIHave.Count];
		for(int i = 0; i < gunsIHave.Count; i++){
			icons[i] = (Texture) Resources.Load("Weap_Icons/" + gunsIHave[i].ToString() + "_img");
		}
	}

	/// <summary>
	/// 현재 무기를 교체할 수 있는 상태인지 확인합니다.
	/// </summary>
	public bool CanSwitchWeapon(bool isSprintHeld = false){
		return switchWeaponCooldown > 1.2f && !isSprintHeld && gunsIHave.Count > 0;
	}

	/// <summary>
	/// 이전/다음 무기로 순환 교체합니다. (direction > 0: 다음 무기, direction < 0: 이전 무기)
	/// </summary>
	public bool CycleWeaponAction(int direction, bool isSprintHeld = false){
		if (direction == 0 || !CanSwitchWeapon(isSprintHeld))
			return false;

		switchWeaponCooldown = 0;
		if (direction > 0) {
			currentGunCounter++;
			if (currentGunCounter > gunsIHave.Count - 1) {
				currentGunCounter = 0;
			}
		} else {
			currentGunCounter--;
			if (currentGunCounter < 0) {
				currentGunCounter = gunsIHave.Count - 1;
			}
		}

		StartCoroutine("Spawn", currentGunCounter);
		return true;
	}

	/// <summary>
	/// 특정 슬롯 인덱스(0부터 시작)의 무기로 교체합니다.
	/// </summary>
	public bool SelectWeaponAction(int slotIndex, bool isSprintHeld = false){
		if (slotIndex < 0 || slotIndex >= gunsIHave.Count || slotIndex == currentGunCounter || !CanSwitchWeapon(isSprintHeld))
			return false;

		switchWeaponCooldown = 0;
		currentGunCounter = slotIndex;
		StartCoroutine("Spawn", currentGunCounter);
		return true;
	}

	/*
	 * This method is called upon weapon switching,
	 * It will check if we carry a gun and destroy it, and its then going to load a gun prefab from our Resources Folder.
	 */
	IEnumerator Spawn(int _redniBroj){
		if (weaponChanging)
			weaponChanging.Play ();
		else
			print ("Missing Weapon Changing music clip.");
		if(currentGun){
			if(currentGun.name.Contains("Gun")){

				if (currentHAndsAnimator)
					currentHAndsAnimator.SetBool("changingWeapon", true);

				yield return new WaitForSeconds(0.8f);
				Destroy(currentGun);
				CurrentGunScript = null;

				GameObject resource = (GameObject) Resources.Load(gunsIHave[_redniBroj].ToString());
				currentGun = (GameObject) Instantiate(resource, transform.position, Quaternion.identity);
				AssignHandsAnimator(currentGun);
			}
			else if(currentGun.name.Contains("Sword")){
				if (currentHAndsAnimator)
					currentHAndsAnimator.SetBool("changingWeapon", true);
				yield return new WaitForSeconds(0.25f);

				if (currentHAndsAnimator)
					currentHAndsAnimator.SetBool("changingWeapon", false);

				yield return new WaitForSeconds(0.6f);
				Destroy(currentGun);
				CurrentGunScript = null;

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
	}


	/*
	* Assigns Animator to the script so we can use it in other scripts of a current gun.
	*/
	void AssignHandsAnimator(GameObject _currentGun){
		if(_currentGun && _currentGun.name.Contains("Gun")){
			CurrentGunScript = currentGun.GetComponent<GunScript>();
			if (CurrentGunScript != null) {
				CurrentGunScript.InitializeOwner(gameObject);
				currentHAndsAnimator = CurrentGunScript.handsAnimator;
			}
		}
	}

	/*
	 * Unity buil-in method to draw GUI.
	 * From here I am listing thourhg guns I have and drawing corresponding images on the sceen.
	 */
	void OnGUI(){

		if(currentGun){
			for(int i = 0; i < gunsIHave.Count; i++){
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
	/*
	 * Passing the image number and gun list have the same sort,
	 * so it will fitthe gun image to our current gun or guns we have.
	 * The curent gun selected image has their image slightly enlared for some value.
	 */
	void DrawCorrespondingImage(int _number){

		string deleteCloneFromName = currentGun.name.Substring(0,currentGun.name.Length - 7);

		if(menuStyle == MenuStyle.horizontal){
			if(deleteCloneFromName == gunsIHave[_number]){
				GUI.DrawTexture(new Rect(vec2(beginPosition).x +(_number*position_x(spacing)),vec2(beginPosition).y,//position variables
					vec2(size).x, vec2(size).y),//size
					icons[_number]);
			}
			else{			
				GUI.DrawTexture(new Rect(vec2(beginPosition).x +(_number*position_x(spacing) + 10),vec2(beginPosition).y + 10,//position variables
					vec2(size).x - 20, vec2(size).y- 20),//size
					icons[_number]);
			}
		}
		else if(menuStyle == MenuStyle.vertical){
			if(deleteCloneFromName == gunsIHave[_number]){
				GUI.DrawTexture(new Rect(vec2(beginPosition).x,vec2(beginPosition).y +(_number*position_y(spacing)),//position variables
					vec2(size).x, vec2(size).y),//size
					icons[_number]);
			}
			else{			
				GUI.DrawTexture(new Rect(vec2(beginPosition).x,vec2(beginPosition).y + 10  +(_number*position_y(spacing)),//position variables
					vec2(size).x - 20, vec2(size).y- 20),//size
					icons[_number]);
			}
		}
	}

	/*
	 * Call this method when player dies.
	 */
	public void DeadMethod(){
		Destroy (currentGun);
		Destroy (this);
	}


	//#####		RETURN THE SIZE AND POSITION for GUI images
	//(we pass in the percentage and it returns some number to appear in that percentage on the sceen) ##################
	private float position_x(float var){
		return Screen.width * var / 100;
	}
	private float position_y(float var)
	{
		return Screen.height * var / 100;
	}
	private float size_x(float var)
	{
		return Screen.width * var / 100;
	}
	private float size_y(float var)
	{
		return Screen.height * var / 100;
	}
	private Vector2 vec2(Vector2 _vec2){
		return new Vector2(Screen.width * _vec2.x / 100, Screen.height * _vec2.y / 100);
	}
	//######################################################

	/*
	 * Sounds
	 */
	[Header("Sounds")]
	[Tooltip("Sound of weapon changing.")]
	public AudioSource weaponChanging;
}
