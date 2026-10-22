using UnityEngine;
using System.Collections;

public enum GunStyles{
	nonautomatic,automatic
}

public class GunScript : MonoBehaviour {
	[Tooltip("Selects type of waepon to shoot rapidly or one bullet per click.")]
	public GunStyles currentStyle;
	[HideInInspector]
	public MouseLookScript mls;

	[Header("Player movement properties")]
	[Tooltip("Speed is determined via gun because not every gun has same properties or weights so you MUST set up your speeds here")]
	public int walkingSpeed = 3;
	[Tooltip("Speed is determined via gun because not every gun has same properties or weights so you MUST set up your speeds here")]
	public int runningSpeed = 5;


	[Header("Bullet properties")]
	[Tooltip("Preset value to tell with how many bullets will our waepon spawn aside.")]
	public float bulletsIHave = 20;
	[Tooltip("Preset value to tell with how much bullets will our waepon spawn inside rifle.")]
	public float bulletsInTheGun = 5;
	[Tooltip("Preset value to tell how much bullets can one magazine carry.")]
	public float amountOfBulletsPerLoad = 5;

	private Transform player;
	private Camera cameraComponent;
	private Transform gunPlaceHolder;

	private PlayerMovementScript pmS;

	// 마리오네트로부터 주입받는 현재 프레임의 무기 조작 상태
	private bool isAimingInput;
	private bool isFiringHoldInput;

	/*
	 * Collection the variables upon awake that we need.
	 */
	void Awake(){
		GameObject defaultPlayer = GameObject.FindGameObjectWithTag("Player");
		if (defaultPlayer != null) {
			InitializeOwner(defaultPlayer);
		}

		Transform hitMarkerTransform = transform.Find("hitMarkerSound");
		if (hitMarkerTransform != null) {
			hitMarker = hitMarkerTransform.GetComponent<AudioSource>();
		}

		startLook = mouseSensitvity_notAiming;
		startAim = mouseSensitvity_aiming;
		startRun = mouseSensitvity_running;
	}

	/// <summary>
	/// 이 무기를 소유한 마리오네트(플레이어/AI 등)의 컴포넌트 참조를 바인딩합니다.
	/// </summary>
	public void InitializeOwner(GameObject ownerPlayer){
		if (ownerPlayer == null)
			return;

		player = ownerPlayer.transform;
		mls = ownerPlayer.GetComponent<MouseLookScript>();
		pmS = ownerPlayer.GetComponent<PlayerMovementScript>();

		if (mls != null && mls.myCamera != null) {
			mainCamera = mls.myCamera;
		} else if (pmS != null && pmS.cameraMain != null) {
			mainCamera = pmS.cameraMain;
		}

		if (mainCamera != null) {
			cameraComponent = mainCamera.GetComponent<Camera>();

			Transform secondCamTransform = mainCamera.Find("Camera");
			if (secondCamTransform != null) {
				secondCamera = secondCamTransform.GetComponent<Camera>();
			}

			Transform spawnTransform = mainCamera.Find("BulletSpawn");
			if (spawnTransform != null) {
				bulletSpawnPlace = spawnTransform.gameObject;
			}

			Transform hudTransform = mainCamera.Find("HUD_bullets");
			if (hudTransform != null) {
				HUD_bullets = hudTransform.GetComponent<TextMesh>();
			}
		}

		if (secondCamera == null) {
			GameObject secondCamObj = GameObject.FindGameObjectWithTag("SecondCamera");
			if (secondCamObj != null) secondCamera = secondCamObj.GetComponent<Camera>();
		}

		if (bulletSpawnPlace == null) {
			bulletSpawnPlace = GameObject.FindGameObjectWithTag("BulletSpawn");
		}

		if (mls != null) {
			rotationLastY = mls.currentYRotation;
			rotationLastX = mls.currentCameraXRotation;
		}
	}


	[HideInInspector]
	public Vector3 currentGunPosition;
	[Header("Gun Positioning")]
	[Tooltip("Vector 3 position from player SETUP for NON AIMING values")]
	public Vector3 restPlacePosition;
	[Tooltip("Vector 3 position from player SETUP for AIMING values")]
	public Vector3 aimPlacePosition;
	[Tooltip("Time that takes for gun to get into aiming stance.")]
	public float gunAimTime = 0.1f;

	[HideInInspector]
	public bool reloading;

	private Vector3 gunPosVelocity;
	private float cameraZoomVelocity;
	private float secondCameraZoomVelocity;

	private Vector2 gunFollowTimeVelocity;

	/*
	Update loop calling for methods that are descriped below where they are initiated.
	*/
	void Update(){

		Animations();

		GiveCameraScriptMySensitvity();

		PositionGun();

		waitTillNextFire -= roundsPerSecond * Time.deltaTime;

		LockCameraWhileMelee ();

		CrossHairExpansionWhenWalking();

	}

	/*
	*Update loop calling for methods that are descriped below where they are initiated.
	*+
	*Calculation of weapon position when aiming or not aiming.
	*/
	void FixedUpdate(){
		RotationGun ();

		MeeleAnimationsStates ();

		/*
		 * Changing some values if we are aiming, like sensitity, zoom racion and position of the waepon.
		 */
		//if aiming
		if(isAimingInput && !reloading && !meeleAttack){
			gunPrecision = gunPrecision_aiming;
			recoilAmount_x = recoilAmount_x_;
			recoilAmount_y = recoilAmount_y_;
			recoilAmount_z = recoilAmount_z_;
			currentGunPosition = Vector3.SmoothDamp(currentGunPosition, aimPlacePosition, ref gunPosVelocity, gunAimTime);
			if (cameraComponent != null)
				cameraComponent.fieldOfView = Mathf.SmoothDamp(cameraComponent.fieldOfView, cameraZoomRatio_aiming, ref cameraZoomVelocity, gunAimTime);
			if (secondCamera != null)
				secondCamera.fieldOfView = Mathf.SmoothDamp(secondCamera.fieldOfView, secondCameraZoomRatio_aiming, ref secondCameraZoomVelocity, gunAimTime);
		}
		//if not aiming
		else{
			gunPrecision = gunPrecision_notAiming;
			recoilAmount_x = recoilAmount_x_non;
			recoilAmount_y = recoilAmount_y_non;
			recoilAmount_z = recoilAmount_z_non;
			currentGunPosition = Vector3.SmoothDamp(currentGunPosition, restPlacePosition, ref gunPosVelocity, gunAimTime);
			if (cameraComponent != null)
				cameraComponent.fieldOfView = Mathf.SmoothDamp(cameraComponent.fieldOfView, cameraZoomRatio_notAiming, ref cameraZoomVelocity, gunAimTime);
			if (secondCamera != null)
				secondCamera.fieldOfView = Mathf.SmoothDamp(secondCamera.fieldOfView, secondCameraZoomRatio_notAiming, ref secondCameraZoomVelocity, gunAimTime);
		}

	}

	[Header("Sensitvity of the gun")]
	[Tooltip("Sensitvity of this gun while not aiming.")]
	public float mouseSensitvity_notAiming = 10;
	[Tooltip("Sensitvity of this gun while aiming.")]
	public float mouseSensitvity_aiming = 5;
	[Tooltip("Sensitvity of this gun while running.")]
	public float mouseSensitvity_running = 4;
	/*
	 * Used to give our main camera different sensivity options for each gun.
	 */
	void GiveCameraScriptMySensitvity(){
		if (mls != null) {
			mls.mouseSensitvity_notAiming = mouseSensitvity_notAiming;
			mls.mouseSensitvity_aiming = mouseSensitvity_aiming;
		}
	}

	/*
	 * Used to expand position of the crosshair or make it dissapear when running
	 */
	void CrossHairExpansionWhenWalking(){
		if (player == null || pmS == null)
			return;

		Rigidbody playerRb = player.GetComponent<Rigidbody>();
		if(playerRb != null && playerRb.linearVelocity.magnitude > 1 && !isFiringHoldInput){//ifnot shooting

			expandValues_crosshair += new Vector2(20, 40) * Time.deltaTime;
			if(pmS.maxSpeed < runningSpeed){ //not running
				expandValues_crosshair = new Vector2(Mathf.Clamp(expandValues_crosshair.x, 0, 10), Mathf.Clamp(expandValues_crosshair.y,0,20));
				fadeout_value = Mathf.Lerp(fadeout_value, 1, Time.deltaTime * 2);
			}
			else{//running
				fadeout_value = Mathf.Lerp(fadeout_value, 0, Time.deltaTime * 10);
				expandValues_crosshair = new Vector2(Mathf.Clamp(expandValues_crosshair.x, 0, 20), Mathf.Clamp(expandValues_crosshair.y,0,40));
			}
		}
		else{//if shooting
			expandValues_crosshair = Vector2.Lerp(expandValues_crosshair, Vector2.zero, Time.deltaTime * 5);
			expandValues_crosshair = new Vector2(Mathf.Clamp(expandValues_crosshair.x, 0, 10), Mathf.Clamp(expandValues_crosshair.y,0,20));
			fadeout_value = Mathf.Lerp(fadeout_value, 1, Time.deltaTime * 2);

		}

	}

	/// <summary>
	/// 무기의 무게(walkingSpeed/runningSpeed)와 사격/조준/근접공격 상태를 고려하여 마리오네트의 달리기 속도를 갱신합니다.
	/// </summary>
	public void SprintWithGunAction(float verticalInput, bool sprintToggleDown){
		if (pmS == null)
			return;

		bool canSprint = verticalInput > 0 && !isAimingInput && !meeleAttack && !isFiringHoldInput;
		pmS.SprintAction(canSprint, sprintToggleDown, walkingSpeed, runningSpeed);
	}

	[HideInInspector]
	public bool meeleAttack;
	[HideInInspector]
	public bool aiming;
	/*
	 * Checking if meeleAttack is already running.
	 * If we are not reloading we can trigger the MeeleAttack animation from the IENumerator.
	 */
	void MeeleAnimationsStates(){
		if (handsAnimator) {
			meeleAttack = handsAnimator.GetCurrentAnimatorStateInfo (0).IsName (meeleAnimationName);
			aiming = handsAnimator.GetCurrentAnimatorStateInfo (0).IsName (aimingAnimationName);	
		}
	}

	/// <summary>
	/// 컨트롤러/마리오네트에서 근접 공격 줄을 당겼을 때 호출됩니다.
	/// </summary>
	public void MeleeAttackAction(){
		if(!meeleAttack){			
			StartCoroutine("AnimationMeeleAttack");
		}
	}

	/*
	* Sets meele animation to play.
	*/
	IEnumerator AnimationMeeleAttack(){
		if (handsAnimator) {
			handsAnimator.SetBool("meeleAttack",true);
			yield return new WaitForSeconds(0.1f);
			handsAnimator.SetBool("meeleAttack",false);
		}
	}

	private float startLook, startAim, startRun;
	/*
	* Setting the mouse sensitvity lower when meele attack and waits till it ends.
	*/
	void LockCameraWhileMelee(){
		if (meeleAttack) {
			mouseSensitvity_notAiming = 2;
			mouseSensitvity_aiming = 1.6f;
			mouseSensitvity_running = 1;
		} else {
			mouseSensitvity_notAiming = startLook;
			mouseSensitvity_aiming = startAim;
			mouseSensitvity_running = startRun;
		}
	}


	private Vector3 velV;
	[HideInInspector]
	public Transform mainCamera;
	private Camera secondCamera;
	/*
	 * Calculatin the weapon position accordingly to the player position and rotation.
	 * After calculation the recoil amount are decreased to 0.
	 */
	void PositionGun(){
		if (mainCamera == null || pmS == null)
			return;

		transform.position = Vector3.SmoothDamp(transform.position,
			mainCamera.transform.position  - 
			(mainCamera.transform.right * (currentGunPosition.x + currentRecoilXPos)) + 
			(mainCamera.transform.up * (currentGunPosition.y+ currentRecoilYPos)) + 
			(mainCamera.transform.forward * (currentGunPosition.z + currentRecoilZPos)),ref velV, 0);

		pmS.cameraPosition = new Vector3(currentRecoilXPos,currentRecoilYPos, 0);

		currentRecoilZPos = Mathf.SmoothDamp(currentRecoilZPos, 0, ref velocity_z_recoil, recoilOverTime_z);
		currentRecoilXPos = Mathf.SmoothDamp(currentRecoilXPos, 0, ref velocity_x_recoil, recoilOverTime_x);
		currentRecoilYPos = Mathf.SmoothDamp(currentRecoilYPos, 0, ref velocity_y_recoil, recoilOverTime_y);

	}


	[Header("Rotation")]
	private Vector2 velocityGunRotate;
	private float gunWeightX,gunWeightY;
	[Tooltip("The time waepon will lag behind the camera view best set to '0'.")]
	public float rotationLagTime = 0f;
	private float rotationLastY;
	private float rotationDeltaY;
	private float angularVelocityY;
	private float rotationLastX;
	private float rotationDeltaX;
	private float angularVelocityX;
	[Tooltip("Value of forward rotation multiplier.")]
	public Vector2 forwardRotationAmount = Vector2.one;
	/*
	* Rotatin the weapon according to mouse look rotation.
	* Calculating the forawrd rotation like in Call Of Duty weapon weight
	*/
	void RotationGun(){
		if (mls == null)
			return;

		rotationDeltaY = mls.currentYRotation - rotationLastY;
		rotationDeltaX = mls.currentCameraXRotation - rotationLastX;

		rotationLastY= mls.currentYRotation;
		rotationLastX= mls.currentCameraXRotation;

		angularVelocityY = Mathf.Lerp (angularVelocityY, rotationDeltaY, Time.deltaTime * 5);
		angularVelocityX = Mathf.Lerp (angularVelocityX, rotationDeltaX, Time.deltaTime * 5);

		gunWeightX = Mathf.SmoothDamp (gunWeightX, mls.currentCameraXRotation, ref velocityGunRotate.x, rotationLagTime);
		gunWeightY = Mathf.SmoothDamp (gunWeightY, mls.currentYRotation, ref velocityGunRotate.y, rotationLagTime);

		transform.rotation = Quaternion.Euler (gunWeightX + (angularVelocityX*forwardRotationAmount.x), gunWeightY + (angularVelocityY*forwardRotationAmount.y), 0);
	}

	private float currentRecoilZPos;
	private float currentRecoilXPos;
	private float currentRecoilYPos;
	/*
	 * Called from ShootMethod();, upon shooting the recoil amount will increase.
	 */
	public void RecoilMath(){
		currentRecoilZPos -= recoilAmount_z;
		currentRecoilXPos -= (Random.value - 0.5f) * recoilAmount_x;
		currentRecoilYPos -= (Random.value - 0.5f) * recoilAmount_y;
		if (mls != null) {
			mls.wantedCameraXRotation -= Mathf.Abs(currentRecoilYPos * gunPrecision);
			mls.wantedYRotation -= (currentRecoilXPos * gunPrecision);
		}

		expandValues_crosshair += new Vector2(6,12);

	}

	[Header("Shooting setup - MUSTDO")]
	[HideInInspector] public GameObject bulletSpawnPlace;
	[Tooltip("Bullet prefab that this waepon will shoot.")]
	public GameObject bullet;
	[Tooltip("Rounds per second if weapon is set to automatic rafal.")]
	public float roundsPerSecond;
	private float waitTillNextFire;

	/// <summary>
	/// 컨트롤러/마리오네트에서 정조준(ADS) 상태를 설정합니다.
	/// </summary>
	public void AimAction(bool isAiming){
		isAimingInput = isAiming;
	}

	/// <summary>
	/// 컨트롤러/마리오네트에서 방아쇠 상태를 전달받아 단발/연사 사격을 수행합니다.
	/// </summary>
	public void FireAction(bool isFireHold, bool isFireDown){
		isFiringHoldInput = isFireHold;

		if (!meeleAttack) {
			if (currentStyle == GunStyles.nonautomatic) {
				if (isFireDown) {
					ShootMethod ();
				}
			}
			if (currentStyle == GunStyles.automatic) {
				if (isFireHold) {
					ShootMethod ();
				}
			}
		}
	}

	/// <summary>
	/// 컨트롤러/마리오네트에서 재장전을 요청했을 때 호출됩니다.
	/// </summary>
	public void ReloadAction(){
		if(pmS != null && pmS.maxSpeed < runningSpeed && !reloading && !meeleAttack){
			StartCoroutine("Reload_Animation");
		}
	}


	[HideInInspector]	public float recoilAmount_z = 0.5f;
	[HideInInspector]	public float recoilAmount_x = 0.5f;
	[HideInInspector]	public float recoilAmount_y = 0.5f;
	[Header("Recoil Not Aiming")]
	[Tooltip("Recoil amount on that AXIS while NOT aiming")]
	public float recoilAmount_z_non = 0.5f;
	[Tooltip("Recoil amount on that AXIS while NOT aiming")]
	public float recoilAmount_x_non = 0.5f;
	[Tooltip("Recoil amount on that AXIS while NOT aiming")]
	public float recoilAmount_y_non = 0.5f;
	[Header("Recoil Aiming")]
	[Tooltip("Recoil amount on that AXIS while aiming")]
	public float recoilAmount_z_ = 0.5f;
	[Tooltip("Recoil amount on that AXIS while aiming")]
	public float recoilAmount_x_ = 0.5f;
	[Tooltip("Recoil amount on that AXIS while aiming")]
	public float recoilAmount_y_ = 0.5f;
	[HideInInspector]public float velocity_z_recoil,velocity_x_recoil,velocity_y_recoil;
	[Header("")]
	[Tooltip("The time that takes weapon to get back on its original axis after recoil.(The smaller number the faster it gets back to original position)")]
	public float recoilOverTime_z = 0.5f;
	[Tooltip("The time that takes weapon to get back on its original axis after recoil.(The smaller number the faster it gets back to original position)")]
	public float recoilOverTime_x = 0.5f;
	[Tooltip("The time that takes weapon to get back on its original axis after recoil.(The smaller number the faster it gets back to original position)")]
	public float recoilOverTime_y = 0.5f;

	[Header("Gun Precision")]
	[Tooltip("Gun rate precision when player is not aiming. THis is calculated with recoil.")]
	public float gunPrecision_notAiming = 200.0f;
	[Tooltip("Gun rate precision when player is aiming. THis is calculated with recoil.")]
	public float gunPrecision_aiming = 100.0f;
	[Tooltip("FOV of first camera when NOT aiming(ONLY SECOND CAMERA RENDERS WEAPONS")]
	public float cameraZoomRatio_notAiming = 60;
	[Tooltip("FOV of first camera when aiming(ONLY SECOND CAMERA RENDERS WEAPONS")]
	public float cameraZoomRatio_aiming = 40;
	[Tooltip("FOV of second camera when NOT aiming(ONLY SECOND CAMERA RENDERS WEAPONS")]
	public float secondCameraZoomRatio_notAiming = 60;
	[Tooltip("FOV of second camera when aiming(ONLY SECOND CAMERA RENDERS WEAPONS")]
	public float secondCameraZoomRatio_aiming = 40;
	[HideInInspector]
	public float gunPrecision;

	[Tooltip("Audios for shootingSound, and reloading.")]
	public AudioSource shoot_sound_source, reloadSound_source;
	[Tooltip("Sound that plays after successful attack bullet hit.")]
	public static AudioSource hitMarker;

	/*
	* Sounds that is called upon hitting the target.
	*/
	public static void HitMarkerSound(){
		if (hitMarker != null)
			hitMarker.Play();
	}

	[Tooltip("Array of muzzel flashes, randmly one will appear after each bullet.")]
	public GameObject[] muzzelFlash;
	[Tooltip("Place on the gun where muzzel flash will appear.")]
	public GameObject muzzelSpawn;
	private GameObject holdFlash;
	private GameObject holdSmoke;
	/*
	 * Called from FireAction();
	 * Creates bullets and muzzle flashes and calls for Recoil.
	 */
	private void ShootMethod(){
		if(waitTillNextFire <= 0 && !reloading && pmS != null && pmS.maxSpeed < runningSpeed){

			if(bulletsInTheGun > 0){

				int randomNumberForMuzzelFlash = 0;// Random.Range(0,5);
				if (bullet)
				{
					Instantiate(bullet, bulletSpawnPlace.transform.position, bulletSpawnPlace.transform.rotation);
				}
				else
					print("Missing the bullet prefab");
				holdFlash = Instantiate(muzzelFlash[randomNumberForMuzzelFlash], muzzelSpawn.transform.position /*- muzzelPosition*/, muzzelSpawn.transform.rotation * Quaternion.Euler(0,0,90) ) as GameObject;
				holdFlash.transform.parent = muzzelSpawn.transform;
				if (shoot_sound_source)
					shoot_sound_source.Play ();
				else
					print ("Missing 'Shoot Sound Source'.");

				RecoilMath();

				waitTillNextFire = 1;
				bulletsInTheGun -= 1;
			}
				
			else{
				StartCoroutine("Reload_Animation");
			}

		}

	}



	/*
	* Reloading, setting the reloading to animator,
	* Waiting for 2 seconds and then seeting the reloaded clip.
	*/
	[Header("reload time after anima")]
	[Tooltip("Time that passes after reloading. Depends on your reload animation length, because reloading can be interrupted via meele attack or running. So any action before this finishes will interrupt reloading.")]
	public float reloadChangeBulletsTime;
	IEnumerator Reload_Animation(){
		if(bulletsIHave > 0 && bulletsInTheGun < amountOfBulletsPerLoad && !reloading){

			if (reloadSound_source != null && reloadSound_source.isPlaying == false) {
				reloadSound_source.Play ();
			}
		

			if (handsAnimator) {
				handsAnimator.SetBool("reloading",true);
				yield return new WaitForSeconds(0.5f);
				handsAnimator.SetBool("reloading",false);
			} else {
				yield return new WaitForSeconds(0.5f);
			}

			yield return new WaitForSeconds (reloadChangeBulletsTime - 0.5f);
			if (meeleAttack == false && pmS != null && pmS.maxSpeed != runningSpeed) {
				if (pmS._freakingZombiesSound)
					pmS._freakingZombiesSound.Play ();
				else
					print ("Missing Freaking Zombies Sound");
				
				if (bulletsIHave - amountOfBulletsPerLoad >= 0) {
					bulletsIHave -= amountOfBulletsPerLoad - bulletsInTheGun;
					bulletsInTheGun = amountOfBulletsPerLoad;
				} else if (bulletsIHave - amountOfBulletsPerLoad < 0) {
					float valueForBoth = amountOfBulletsPerLoad - bulletsInTheGun;
					if (bulletsIHave - valueForBoth < 0) {
						bulletsInTheGun += bulletsIHave;
						bulletsIHave = 0;
					} else {
						bulletsIHave -= valueForBoth;
						bulletsInTheGun += valueForBoth;
					}
				}
			} else {
				if (reloadSound_source != null)
					reloadSound_source.Stop ();

				print ("Reload interrupted via meele attack");
			}

		}
	}

	/*
	 * Setting the number of bullets to the hud UI gameobject if there is one.
	 * And drawing CrossHair from here.
	 */
	[Tooltip("HUD bullets to display bullet count on screen. Will be find under name 'HUD_bullets' in scene.")]
	public TextMesh HUD_bullets;
	void OnGUI(){
		if(!HUD_bullets){
			try{
				GameObject hudObj = GameObject.Find("HUD_bullets");
				if (hudObj != null)
					HUD_bullets = hudObj.GetComponent<TextMesh>();
			}
			catch(System.Exception ex){
				print("Couldnt find the HUD_Bullets ->" + ex.StackTrace.ToString());
			}
		}
		if(mls && HUD_bullets)
			HUD_bullets.text = bulletsIHave.ToString() + " - " + bulletsInTheGun.ToString();

		DrawCrosshair();
	}

	[Header("Crosshair properties")]
	public Texture horizontal_crosshair, vertical_crosshair;
	public Vector2 top_pos_crosshair, bottom_pos_crosshair, left_pos_crosshair, right_pos_crosshair;
	public Vector2 size_crosshair_vertical = new Vector2(1,1), size_crosshair_horizontal = new Vector2(1,1);
	[HideInInspector]
	public Vector2 expandValues_crosshair;
	private float fadeout_value = 1;
	/*
	 * Drawing the crossHair.
	 */
	void DrawCrosshair(){
		GUI.color = new Color(GUI.color.r, GUI.color.g, GUI.color.b, fadeout_value);
		if(!isAimingInput){//if not aiming draw
			GUI.DrawTexture(new Rect(vec2(left_pos_crosshair).x + position_x(-expandValues_crosshair.x) + Screen.width/2,Screen.height/2 + vec2(left_pos_crosshair).y, vec2(size_crosshair_horizontal).x, vec2(size_crosshair_horizontal).y), vertical_crosshair);//left
			GUI.DrawTexture(new Rect(vec2(right_pos_crosshair).x + position_x(expandValues_crosshair.x) + Screen.width/2,Screen.height/2 + vec2(right_pos_crosshair).y, vec2(size_crosshair_horizontal).x, vec2(size_crosshair_horizontal).y), vertical_crosshair);//right

			GUI.DrawTexture(new Rect(vec2(top_pos_crosshair).x + Screen.width/2,Screen.height/2 + vec2(top_pos_crosshair).y + position_y(-expandValues_crosshair.y), vec2(size_crosshair_vertical).x, vec2(size_crosshair_vertical).y ), horizontal_crosshair);//top
			GUI.DrawTexture(new Rect(vec2(bottom_pos_crosshair).x + Screen.width/2,Screen.height/2 +vec2(bottom_pos_crosshair).y + position_y(expandValues_crosshair.y), vec2(size_crosshair_vertical).x, vec2(size_crosshair_vertical).y), horizontal_crosshair);//bottom
		}

	}

	//#####		RETURN THE SIZE AND POSITION for GUI images ##################
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
	//#

	public Animator handsAnimator;
	/*
	* Fetching if any current animation is running.
	*/
	void Animations(){

		if(handsAnimator && pmS != null){

			reloading = handsAnimator.GetCurrentAnimatorStateInfo(0).IsName(reloadAnimationName);

			handsAnimator.SetFloat("walkSpeed",pmS.currentSpeed);
			handsAnimator.SetBool("aiming", isAimingInput);
			handsAnimator.SetInteger("maxSpeed", pmS.maxSpeed);
		}

	}

	[Header("Animation names")]
	public string reloadAnimationName = "Player_Reload";
	public string aimingAnimationName = "Player_AImpose";
	public string meeleAnimationName = "Character_Malee";
}
