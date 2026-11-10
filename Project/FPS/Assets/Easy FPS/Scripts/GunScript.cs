using UnityEngine;
using System.Collections;
using Unity.VisualScripting;

public enum GunStyles{
	nonautomatic,automatic
}
public class GunScript : MonoBehaviour {
	[Tooltip("Selects type of waepon to shoot rapidly or one bullet per click.")]
	public GunStyles currentStyle;
	[HideInInspector]
	public MouseLookScript m_cMouseLockScript;

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

	public Transform trPlayer;
    public Camera cameraComponent;
    public Transform trLevelFloor;

	private PlayerMovementScript playerMovementScript;
	private Player playerComponent;

	// 마리오네트 조준 상태
	private bool isAimingControlled = false;

	void Awake(){
		
	}

	void OnDisable()
	{
		reloading = false;
		meeleAttack = false;
		aiming = false;
		isAimingControlled = false;
	}

    public void Initialize(Transform _trPlayer, MouseLookScript _mouseLockScript, Transform trCamera, Camera _cameraSecond, GameObject objBulletBulletSpawnPlace, AudioSource asHitMarker)
    {
		this.trPlayer = _trPlayer;
        m_cMouseLockScript = _mouseLockScript;

        mainCamera = trCamera;
        secondCamera = _cameraSecond;
        if (trCamera != null)
            cameraComponent = trCamera.GetComponent<Camera>();
        else
            cameraComponent = null;

        if (trPlayer != null)
        {
            playerMovementScript = trPlayer.GetComponent<PlayerMovementScript>();
            playerComponent = trPlayer.GetComponent<Player>();
        }

        bulletSpawnPlace = objBulletBulletSpawnPlace;
		hitMarker = asHitMarker;
        startLook = mouseSensitvity_notAiming;
        startAim = mouseSensitvity_aiming;
        startRun = mouseSensitvity_running;

		if (m_cMouseLockScript)
		{
			rotationLastY = m_cMouseLockScript.currentYRotation;
			rotationLastX = m_cMouseLockScript.currentCameraXRotation;
		}
		else
		{
			rotationLastY = 0;
			rotationLastX = 0;
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

    public void Start()
    {
    }

    public void UpdateProcess()
	{
        Animations();

        GiveCameraScriptMySensitvity();

        PositionGun();

        LockCameraWhileMelee();

        CrossHairExpansionWhenWalking();

		float rps = roundsPerSecond > 0 ? roundsPerSecond : 5f;
		waitTillNextFire -= rps * Time.deltaTime;
    }


	public void FixedUpdateProcess()
	{
        RotationGun();

        MeeleAnimationsStates();

        if (isAimingControlled && !reloading && !meeleAttack)
        {
            gunPrecision = gunPrecision_aiming;
            recoilAmount_x = recoilAmount_x_;
            recoilAmount_y = recoilAmount_y_;
            recoilAmount_z = recoilAmount_z_;
            currentGunPosition = Vector3.SmoothDamp(currentGunPosition, aimPlacePosition, ref gunPosVelocity, gunAimTime);
            if (cameraComponent)
                cameraComponent.fieldOfView = Mathf.SmoothDamp(cameraComponent.fieldOfView, cameraZoomRatio_aiming, ref cameraZoomVelocity, gunAimTime);
            if (secondCamera)
                secondCamera.fieldOfView = Mathf.SmoothDamp(secondCamera.fieldOfView, secondCameraZoomRatio_aiming, ref secondCameraZoomVelocity, gunAimTime);
        }
        else
        {
            gunPrecision = gunPrecision_notAiming;
            recoilAmount_x = recoilAmount_x_non;
            recoilAmount_y = recoilAmount_y_non;
            recoilAmount_z = recoilAmount_z_non;
            currentGunPosition = Vector3.SmoothDamp(currentGunPosition, restPlacePosition, ref gunPosVelocity, gunAimTime);
            if (cameraComponent)
                cameraComponent.fieldOfView = Mathf.SmoothDamp(cameraComponent.fieldOfView, cameraZoomRatio_notAiming, ref cameraZoomVelocity, gunAimTime);
            if (secondCamera)
                secondCamera.fieldOfView = Mathf.SmoothDamp(secondCamera.fieldOfView, secondCameraZoomRatio_notAiming, ref secondCameraZoomVelocity, gunAimTime);
        }
    }

	// ==================== 마리오네트 사격/조준 API ====================

	public void SetAiming(bool aiming)
	{
		isAimingControlled = aiming;
	}

	public void TriggerShoot()
	{
		if (!meeleAttack)
		{
			ShootMethod();
		}
	}

	public void TriggerReload()
	{
		if (!reloading && !meeleAttack)
		{
			StartCoroutine("Reload_Animation");
		}
	}

	public void TriggerMelee()
	{
		if (!meeleAttack)
		{
			StartCoroutine("AnimationMeeleAttack");
		}
	}

	[Header("Sensitvity of the gun")]
	[Tooltip("Sensitvity of this gun while not aiming.")]
	public float mouseSensitvity_notAiming = 10;
	[Tooltip("Sensitvity of this gun while aiming.")]
	public float mouseSensitvity_aiming = 5;
	[Tooltip("Sensitvity of this gun while running.")]
	public float mouseSensitvity_running = 4;

	void GiveCameraScriptMySensitvity(){
		if(m_cMouseLockScript)
		{ 
			m_cMouseLockScript.mouseSensitvity_notAiming = mouseSensitvity_notAiming;
			m_cMouseLockScript.mouseSensitvity_aiming = mouseSensitvity_aiming;
        }
    }

	void CrossHairExpansionWhenWalking(){
		if (playerComponent != null && !playerComponent.isPlayer) return;

		if (!trPlayer) return;
		Rigidbody rigidbody = trPlayer.GetComponent<Rigidbody>();

        if (rigidbody && rigidbody.linearVelocity.magnitude > 1 && !isAimingControlled){

			expandValues_crosshair += new Vector2(20, 40) * Time.deltaTime;
			if(playerMovementScript && playerMovementScript.maxSpeed < runningSpeed){
				expandValues_crosshair = new Vector2(Mathf.Clamp(expandValues_crosshair.x, 0, 10), Mathf.Clamp(expandValues_crosshair.y,0,20));
				fadeout_value = Mathf.Lerp(fadeout_value, 1, Time.deltaTime * 2);
			}
			else{
				fadeout_value = Mathf.Lerp(fadeout_value, 0, Time.deltaTime * 10);
				expandValues_crosshair = new Vector2(Mathf.Clamp(expandValues_crosshair.x, 0, 20), Mathf.Clamp(expandValues_crosshair.y,0,40));
			}
		}
		else{
			expandValues_crosshair = Vector2.Lerp(expandValues_crosshair, Vector2.zero, Time.deltaTime * 5);
			expandValues_crosshair = new Vector2(Mathf.Clamp(expandValues_crosshair.x, 0, 10), Mathf.Clamp(expandValues_crosshair.y,0,20));
			fadeout_value = Mathf.Lerp(fadeout_value, 1, Time.deltaTime * 2);

		}

	}

	[HideInInspector]
	public bool meeleAttack;
	[HideInInspector]
	public bool aiming;

	void MeeleAnimationsStates(){
		if (handsAnimator) {
			meeleAttack = handsAnimator.GetCurrentAnimatorStateInfo (0).IsName (meeleAnimationName);
			aiming = handsAnimator.GetCurrentAnimatorStateInfo (0).IsName (aimingAnimationName);	
		}
	}

	IEnumerator AnimationMeeleAttack(){
		if (handsAnimator) {
			handsAnimator.SetBool("meeleAttack",true);
			yield return new WaitForSeconds(0.1f);
			handsAnimator.SetBool("meeleAttack",false);
		}
	}

	private float startLook, startAim, startRun;

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

	void PositionGun(){
		// Player(ViewPivot/Camera)의 자식 오브젝트로 귀속된 경우 로컬 좌표만으로 위치 및 반동 제어
		if (transform.parent != null)
		{
			Vector3 targetLocalPos = new Vector3(
				-(currentGunPosition.x + currentRecoilXPos),
				(currentGunPosition.y + currentRecoilYPos),
				(currentGunPosition.z + currentRecoilZPos)
			);
			transform.localPosition = Vector3.SmoothDamp(transform.localPosition, targetLocalPos, ref velV, 0f);

			if (playerMovementScript)
				playerMovementScript.cameraPosition = new Vector3(currentRecoilXPos, currentRecoilYPos, 0);

			currentRecoilZPos = Mathf.SmoothDamp(currentRecoilZPos, 0, ref velocity_z_recoil, recoilOverTime_z);
			currentRecoilXPos = Mathf.SmoothDamp(currentRecoilXPos, 0, ref velocity_x_recoil, recoilOverTime_x);
			currentRecoilYPos = Mathf.SmoothDamp(currentRecoilYPos, 0, ref velocity_y_recoil, recoilOverTime_y);
			return;
		}

		Transform refTransform = mainCamera != null ? mainCamera : trPlayer;

		if (refTransform)
		{
			transform.position = Vector3.SmoothDamp(transform.position,
				refTransform.position -
				(refTransform.right * (currentGunPosition.x + currentRecoilXPos)) +
				(refTransform.up * (currentGunPosition.y + currentRecoilYPos)) +
				(refTransform.forward * (currentGunPosition.z + currentRecoilZPos)), ref velV, 0);

            if (playerMovementScript)
                playerMovementScript.cameraPosition = new Vector3(currentRecoilXPos, currentRecoilYPos, 0);

            currentRecoilZPos = Mathf.SmoothDamp(currentRecoilZPos, 0, ref velocity_z_recoil, recoilOverTime_z);
            currentRecoilXPos = Mathf.SmoothDamp(currentRecoilXPos, 0, ref velocity_x_recoil, recoilOverTime_x);
            currentRecoilYPos = Mathf.SmoothDamp(currentRecoilYPos, 0, ref velocity_y_recoil, recoilOverTime_y);
        }
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

	void RotationGun(){
		if (transform.parent != null)
		{
			if (m_cMouseLockScript)
			{
				rotationDeltaY = m_cMouseLockScript.currentYRotation - rotationLastY;
				rotationDeltaX = m_cMouseLockScript.currentCameraXRotation - rotationLastX;

				rotationLastY = m_cMouseLockScript.currentYRotation;
				rotationLastX = m_cMouseLockScript.currentCameraXRotation;

				angularVelocityY = Mathf.Lerp(angularVelocityY, rotationDeltaY, Time.deltaTime * 5);
				angularVelocityX = Mathf.Lerp(angularVelocityX, rotationDeltaX, Time.deltaTime * 5);

				// 부모(Camera/ViewPivot)가 이미 시선 회전을 담당하므로 로컬 흔들림(Sway)만 반영
				transform.localRotation = Quaternion.Euler(
					angularVelocityX * forwardRotationAmount.x,
					angularVelocityY * forwardRotationAmount.y,
					0f
				);
			}
			else
			{
				transform.localRotation = Quaternion.identity;
			}
			return;
		}

		if (m_cMouseLockScript)
		{
			rotationDeltaY = m_cMouseLockScript.currentYRotation - rotationLastY;
			rotationDeltaX = m_cMouseLockScript.currentCameraXRotation - rotationLastX;

			rotationLastY = m_cMouseLockScript.currentYRotation;
			rotationLastX = m_cMouseLockScript.currentCameraXRotation;

			angularVelocityY = Mathf.Lerp(angularVelocityY, rotationDeltaY, Time.deltaTime * 5);
			angularVelocityX = Mathf.Lerp(angularVelocityX, rotationDeltaX, Time.deltaTime * 5);

			gunWeightX = Mathf.SmoothDamp(gunWeightX, m_cMouseLockScript.currentCameraXRotation, ref velocityGunRotate.x, rotationLagTime);
			gunWeightY = Mathf.SmoothDamp(gunWeightY, m_cMouseLockScript.currentYRotation, ref velocityGunRotate.y, rotationLagTime);

			transform.rotation = Quaternion.Euler(gunWeightX + (angularVelocityX * forwardRotationAmount.x), gunWeightY + (angularVelocityY * forwardRotationAmount.y), 0);
		}
		else if (trPlayer)
		{
			transform.rotation = trPlayer.rotation;
        }
    }

	private float currentRecoilZPos;
	private float currentRecoilXPos;
	private float currentRecoilYPos;

	public void RecoilMath(){
		currentRecoilZPos -= recoilAmount_z;
		currentRecoilXPos -= (Random.value - 0.5f) * recoilAmount_x;
		currentRecoilYPos -= (Random.value - 0.5f) * recoilAmount_y;
		if (m_cMouseLockScript)
		{
			m_cMouseLockScript.wantedCameraXRotation -= Mathf.Abs(currentRecoilYPos * gunPrecision);
			m_cMouseLockScript.wantedYRotation -= (currentRecoilXPos * gunPrecision);
		}
		else if (transform.parent == null && trPlayer)
		{
			transform.rotation = trPlayer.rotation;
		}

        expandValues_crosshair += new Vector2(6, 12);
	}

	[Header("Shooting setup - MUSTDO")]
	[HideInInspector] public GameObject bulletSpawnPlace;
	[Tooltip("Bullet prefab that this waepon will shoot.")]
	public GameObject bullet;
	[Tooltip("Rounds per second if weapon is set to automatic rafal.")]
	public float roundsPerSecond;
	private float waitTillNextFire;

	public void GetBulletSpawnTransform(out Vector3 spawnPosition, out Quaternion spawnRotation)
	{
		bool isLocalPlayer = (playerComponent == null || playerComponent.isPlayer) && mainCamera != null;

		if (isLocalPlayer)
		{
			spawnRotation = mainCamera.rotation;

			if (bulletSpawnPlace != null && bulletSpawnPlace != trPlayer.gameObject)
				spawnPosition = bulletSpawnPlace.transform.position;
			else
				spawnPosition = mainCamera.position + (mainCamera.forward * 0.4f);
		}
		else
		{
			// NPC: 총알 스폰 기본 위치 결정 (가슴/눈높이 앞)
			Vector3 forwardDir = trPlayer != null ? trPlayer.forward : transform.forward;

			if (bulletSpawnPlace != null && bulletSpawnPlace != trPlayer.gameObject)
				spawnPosition = bulletSpawnPlace.transform.position;
			else if (trPlayer != null)
				spawnPosition = trPlayer.position + (Vector3.up * 1.4f) + (forwardDir * 0.4f);
			else
				spawnPosition = transform.position + (forwardDir * 0.4f);

			// NPC의 조준 목표점(적의 중심)이 있는 경우 목표를 향한 3D 방향으로 정밀 조준
			if (playerComponent != null && playerComponent.AimTargetPosition.HasValue)
			{
				Vector3 targetPos = playerComponent.AimTargetPosition.Value;
				Vector3 aimDir = (targetPos - spawnPosition).normalized;
				if (aimDir.sqrMagnitude > 0.001f)
				{
					spawnRotation = Quaternion.LookRotation(aimDir, Vector3.up);
					return;
				}
			}

			spawnRotation = Quaternion.LookRotation(forwardDir, Vector3.up);
		}
	}

	[HideInInspector]	public float recoilAmount_z = 0.5f;
	[HideInInspector]	public float recoilAmount_x = 0.5f;
	[HideInInspector]	public float recoilAmount_y = 0.5f;
	[Header("Recoil Not Aiming")]
	public float recoilAmount_z_non = 0.5f;
	public float recoilAmount_x_non = 0.5f;
	public float recoilAmount_y_non = 0.5f;
	[Header("Recoil Aiming")]
	public float recoilAmount_z_ = 0.5f;
	public float recoilAmount_x_ = 0.5f;
	public float recoilAmount_y_ = 0.5f;
	[HideInInspector]public float velocity_z_recoil,velocity_x_recoil,velocity_y_recoil;
	[Header("")]
	public float recoilOverTime_z = 0.5f;
	public float recoilOverTime_x = 0.5f;
	public float recoilOverTime_y = 0.5f;

	[Header("Gun Precision")]
	public float gunPrecision_notAiming = 200.0f;
	public float gunPrecision_aiming = 100.0f;
	public float cameraZoomRatio_notAiming = 60;
	public float cameraZoomRatio_aiming = 40;
	public float secondCameraZoomRatio_notAiming = 60;
	public float secondCameraZoomRatio_aiming = 40;
	[HideInInspector]
	public float gunPrecision;

	[Tooltip("Audios for shootingSound, and reloading.")]
	public AudioSource shoot_sound_source, reloadSound_source;
	[Tooltip("Sound that plays after successful attack bullet hit.")]
	public static AudioSource hitMarker;

	public static void HitMarkerSound(){
		if (hitMarker)
			hitMarker.Play();
	}

	[Tooltip("Array of muzzel flashes, randmly one will appear after each bullet.")]
	public GameObject[] muzzelFlash;
	[Tooltip("Place on the gun where muzzel flash will appear.")]
	public GameObject muzzelSpawn;
	private GameObject holdFlash;
	private GameObject holdSmoke;

	public void ShootMethod(){
		bool canShootSpeed = (playerMovementScript == null || playerMovementScript.maxSpeed <= runningSpeed);

		if(waitTillNextFire <= 0 && !reloading && canShootSpeed){

			if(bulletsInTheGun > 0){

				GetBulletSpawnTransform(out Vector3 spawnPos, out Quaternion spawnRot);

				if (bullet)
				{
					Instantiate(bullet, spawnPos, spawnRot);
				}
				else
				{
					print("Missing the bullet prefab");
				}

				if (muzzelSpawn && muzzelFlash != null && muzzelFlash.Length > 0)
				{
					int randomNumberForMuzzelFlash = Random.Range(0, muzzelFlash.Length);
					if (muzzelFlash[randomNumberForMuzzelFlash] != null)
					{
						holdFlash = Instantiate(muzzelFlash[randomNumberForMuzzelFlash], muzzelSpawn.transform.position, muzzelSpawn.transform.rotation * Quaternion.Euler(0,0,90)) as GameObject;
						holdFlash.transform.parent = muzzelSpawn.transform;
					}
				}

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

	[Header("reload time after anima")]
	public float reloadChangeBulletsTime;
	IEnumerator Reload_Animation(){
		if(bulletsIHave > 0 && bulletsInTheGun < amountOfBulletsPerLoad && !reloading){

			if (reloadSound_source && reloadSound_source.isPlaying == false) {
				reloadSound_source.Play ();
			}

			if (handsAnimator) {
				handsAnimator.SetBool("reloading",true);
				yield return new WaitForSeconds(0.5f);
				handsAnimator.SetBool("reloading",false);
			}

			yield return new WaitForSeconds (reloadChangeBulletsTime - 0.5f);
			if (meeleAttack == false && playerMovementScript && playerMovementScript.maxSpeed != runningSpeed) {
				PlayerMovementScript movementScript = trPlayer ? trPlayer.GetComponent<PlayerMovementScript>() : null;

                if (movementScript && movementScript._freakingZombiesSound)
                    movementScript._freakingZombiesSound.Play ();
				
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
				if (reloadSound_source)
					reloadSound_source.Stop ();

				print ("Reload interrupted via meele attack");
			}

		}
	}

	[Tooltip("HUD bullets to display bullet count on screen. Will be find under name 'HUD_bullets' in scene.")]
	public TextMesh HUD_bullets;
	void OnGUI(){
		if (playerComponent != null && !playerComponent.isPlayer) return;
		if (cameraComponent == null) return;

		if(!HUD_bullets){
			try{
				HUD_bullets = GameObject.Find("HUD_bullets")?.GetComponent<TextMesh>();
			}
			catch(System.Exception ex){
				print("Couldnt find the HUD_Bullets ->" + ex.StackTrace.ToString());
			}
		}
		if(m_cMouseLockScript && HUD_bullets)
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

	void DrawCrosshair(){
		GUI.color = new Color(GUI.color.r, GUI.color.g, GUI.color.b, fadeout_value);
		if(!isAimingControlled){
			GUI.DrawTexture(new Rect(vec2(left_pos_crosshair).x + position_x(-expandValues_crosshair.x) + Screen.width/2,Screen.height/2 + vec2(left_pos_crosshair).y, vec2(size_crosshair_horizontal).x, vec2(size_crosshair_horizontal).y), vertical_crosshair);
			GUI.DrawTexture(new Rect(vec2(right_pos_crosshair).x + position_x(expandValues_crosshair.x) + Screen.width/2,Screen.height/2 + vec2(right_pos_crosshair).y, vec2(size_crosshair_horizontal).x, vec2(size_crosshair_horizontal).y), vertical_crosshair);

			GUI.DrawTexture(new Rect(vec2(top_pos_crosshair).x + Screen.width/2,Screen.height/2 + vec2(top_pos_crosshair).y + position_y(-expandValues_crosshair.y), vec2(size_crosshair_vertical).x, vec2(size_crosshair_vertical).y ), horizontal_crosshair);
			GUI.DrawTexture(new Rect(vec2(bottom_pos_crosshair).x + Screen.width/2,Screen.height/2 +vec2(bottom_pos_crosshair).y + position_y(expandValues_crosshair.y), vec2(size_crosshair_vertical).x, vec2(size_crosshair_vertical).y), horizontal_crosshair);
		}

	}

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

	public Animator handsAnimator;

	void Animations(){

		if(handsAnimator && playerMovementScript)
        {
			reloading = handsAnimator.GetCurrentAnimatorStateInfo(0).IsName(reloadAnimationName);

			handsAnimator.SetFloat("walkSpeed", playerMovementScript.currentSpeed);
			handsAnimator.SetBool("aiming", isAimingControlled);
			handsAnimator.SetInteger("maxSpeed", playerMovementScript.maxSpeed);
		}

	}

	[Header("Animation names")]
	public string reloadAnimationName = "Player_Reload";
	public string aimingAnimationName = "Player_AImpose";
	public string meeleAnimationName = "Character_Malee";

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawCube(this.transform.position, Vector2.one);
    }
}
