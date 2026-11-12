using System.Collections;
using UnityEngine;
using UnityEngine.Serialization;

public enum GunStyles
{
    nonautomatic,
    automatic
}

public class GunScript : MonoBehaviour
{
    [Tooltip("Selects type of weapon to shoot rapidly or one bullet per click.")]
    [FormerlySerializedAs("currentStyle")]
    public GunStyles m_eCurrentStyle;

    [HideInInspector]
    [FormerlySerializedAs("m_cMouseLockScript")]
    public MouseLookScript m_cMouseLook;

    [Header("Player movement properties")]
    [Tooltip("Speed is determined via gun because not every gun has same properties or weights so you MUST set up your speeds here")]
    [FormerlySerializedAs("walkingSpeed")]
    public int m_nWalkingSpeed = 3;

    [Tooltip("Speed is determined via gun because not every gun has same properties or weights so you MUST set up your speeds here")]
    [FormerlySerializedAs("runningSpeed")]
    public int m_nRunningSpeed = 5;

    [Header("Bullet properties")]
    [Tooltip("Preset value to tell with how many bullets will our weapon spawn aside.")]
    [FormerlySerializedAs("bulletsIHave")]
    public float m_fBulletsIHave = 20f;

    [Tooltip("Preset value to tell with how much bullets will our weapon spawn inside rifle.")]
    [FormerlySerializedAs("bulletsInTheGun")]
    public float m_fBulletsInTheGun = 5f;

    [Tooltip("Preset value to tell how much bullets can one magazine carry.")]
    [FormerlySerializedAs("amountOfBulletsPerLoad")]
    public float m_fAmountOfBulletsPerLoad = 5f;

    [FormerlySerializedAs("trPlayer")]
    public Transform m_trPlayer;

    [FormerlySerializedAs("cameraComponent")]
    public Camera m_camMain;

    [FormerlySerializedAs("trLevelFloor")]
    public Transform m_trLevelFloor;

    private PlayerMovementScript m_cPlayerMovement;
    private Player m_cPlayer;

    // 마리오네트 조준 상태
    private bool m_bIsAimingControlled = false;

    [HideInInspector]
    [FormerlySerializedAs("currentGunPosition")]
    public Vector3 m_vCurrentGunPosition;

    [Header("Gun Positioning")]
    [Tooltip("Vector 3 position from player SETUP for NON AIMING values")]
    [FormerlySerializedAs("restPlacePosition")]
    public Vector3 m_vRestPlacePosition;

    [Tooltip("Vector 3 position from player SETUP for AIMING values")]
    [FormerlySerializedAs("aimPlacePosition")]
    public Vector3 m_vAimPlacePosition;

    [Tooltip("Time that takes for gun to get into aiming stance.")]
    [FormerlySerializedAs("gunAimTime")]
    public float m_fGunAimTime = 0.1f;

    [HideInInspector]
    [FormerlySerializedAs("reloading")]
    public bool m_bIsReloading;

    private Vector3 m_vGunPosVelocity;
    private float m_fCameraZoomVelocity;
    private float m_fSecondCameraZoomVelocity;

    [Header("Sensitivity of the gun")]
    [Tooltip("Sensitivity of this gun while not aiming.")]
    [FormerlySerializedAs("mouseSensitvity_notAiming")]
    public float m_fMouseSensitivityNotAiming = 10f;

    [Tooltip("Sensitivity of this gun while aiming.")]
    [FormerlySerializedAs("mouseSensitvity_aiming")]
    public float m_fMouseSensitivityAiming = 5f;

    [Tooltip("Sensitivity of this gun while running.")]
    [FormerlySerializedAs("mouseSensitvity_running")]
    public float m_fMouseSensitivityRunning = 4f;

    private float m_fStartLook, m_fStartAim, m_fStartRun;

    [HideInInspector]
    [FormerlySerializedAs("meeleAttack")]
    public bool m_bIsMeleeAttack;

    [HideInInspector]
    [FormerlySerializedAs("aiming")]
    public bool m_bIsAiming;

    private Vector3 m_vVelV;

    [HideInInspector]
    [FormerlySerializedAs("mainCamera")]
    public Transform m_trMainCamera;

    private Camera m_camSecond;

    [Header("Rotation")]
    private Vector2 m_vVelocityGunRotate;
    private float m_fGunWeightX, m_fGunWeightY;

    [Tooltip("The time weapon will lag behind the camera view best set to '0'.")]
    [FormerlySerializedAs("rotationLagTime")]
    public float m_fRotationLagTime = 0f;

    private float m_fRotationLastY;
    private float m_fRotationDeltaY;
    private float m_fAngularVelocityY;
    private float m_fRotationLastX;
    private float m_fRotationDeltaX;
    private float m_fAngularVelocityX;

    [Tooltip("Value of forward rotation multiplier.")]
    [FormerlySerializedAs("forwardRotationAmount")]
    public Vector2 m_vForwardRotationAmount = Vector2.one;

    private float m_fCurrentRecoilZPos;
    private float m_fCurrentRecoilXPos;
    private float m_fCurrentRecoilYPos;

    [Header("Shooting setup - MUSTDO")]
    [HideInInspector]
    [FormerlySerializedAs("bulletSpawnPlace")]
    public GameObject m_objBulletSpawnPlace;

    [Tooltip("Bullet prefab that this weapon will shoot.")]
    [FormerlySerializedAs("bullet")]
    public GameObject m_prefabBullet;

    [Tooltip("Rounds per second if weapon is set to automatic rafal.")]
    [FormerlySerializedAs("roundsPerSecond")]
    public float m_fRoundsPerSecond;

    private float m_fWaitTillNextFire;

    [HideInInspector] [FormerlySerializedAs("recoilAmount_z")] public float m_fRecoilAmountZ = 0.5f;
    [HideInInspector] [FormerlySerializedAs("recoilAmount_x")] public float m_fRecoilAmountX = 0.5f;
    [HideInInspector] [FormerlySerializedAs("recoilAmount_y")] public float m_fRecoilAmountY = 0.5f;

    [Header("Recoil Not Aiming")]
    [FormerlySerializedAs("recoilAmount_z_non")] public float m_fRecoilAmountZNon = 0.5f;
    [FormerlySerializedAs("recoilAmount_x_non")] public float m_fRecoilAmountXNon = 0.5f;
    [FormerlySerializedAs("recoilAmount_y_non")] public float m_fRecoilAmountYNon = 0.5f;

    [Header("Recoil Aiming")]
    [FormerlySerializedAs("recoilAmount_z_")] public float m_fRecoilAmountZAim = 0.5f;
    [FormerlySerializedAs("recoilAmount_x_")] public float m_fRecoilAmountXAim = 0.5f;
    [FormerlySerializedAs("recoilAmount_y_")] public float m_fRecoilAmountYAim = 0.5f;

    [HideInInspector] [FormerlySerializedAs("velocity_z_recoil")] public float m_fVelocityZRecoil;
    [HideInInspector] [FormerlySerializedAs("velocity_x_recoil")] public float m_fVelocityXRecoil;
    [HideInInspector] [FormerlySerializedAs("velocity_y_recoil")] public float m_fVelocityYRecoil;

    [Header("Recoil Recovery Time")]
    [FormerlySerializedAs("recoilOverTime_z")] public float m_fRecoilOverTimeZ = 0.5f;
    [FormerlySerializedAs("recoilOverTime_x")] public float m_fRecoilOverTimeX = 0.5f;
    [FormerlySerializedAs("recoilOverTime_y")] public float m_fRecoilOverTimeY = 0.5f;

    [Header("Gun Precision")]
    [FormerlySerializedAs("gunPrecision_notAiming")] public float m_fGunPrecisionNotAiming = 200.0f;
    [FormerlySerializedAs("gunPrecision_aiming")] public float m_fGunPrecisionAiming = 100.0f;
    [FormerlySerializedAs("cameraZoomRatio_notAiming")] public float m_fCameraZoomRatioNotAiming = 60f;
    [FormerlySerializedAs("cameraZoomRatio_aiming")] public float m_fCameraZoomRatioAiming = 40f;
    [FormerlySerializedAs("secondCameraZoomRatio_notAiming")] public float m_fSecondCameraZoomRatioNotAiming = 60f;
    [FormerlySerializedAs("secondCameraZoomRatio_aiming")] public float m_fSecondCameraZoomRatioAiming = 40f;

    [HideInInspector]
    [FormerlySerializedAs("gunPrecision")]
    public float m_fGunPrecision;

    [Tooltip("Audios for shootingSound, and reloading.")]
    [FormerlySerializedAs("shoot_sound_source")]
    public AudioSource m_asShootSound;

    [FormerlySerializedAs("reloadSound_source")]
    public AudioSource m_asReloadSound;

    [Tooltip("Sound that plays after successful attack bullet hit.")]
    private AudioSource m_asHitMarker;

    [Tooltip("Array of muzzle flashes, randomly one will appear after each bullet.")]
    [FormerlySerializedAs("muzzelFlash")]
    public GameObject[] m_arrPrefabMuzzleFlash;

    [Tooltip("Place on the gun where muzzle flash will appear.")]
    [FormerlySerializedAs("muzzelSpawn")]
    public GameObject m_objMuzzleSpawn;

    private GameObject m_objHoldFlash;

    [Header("Reload time after anim")]
    [FormerlySerializedAs("reloadChangeBulletsTime")]
    public float m_fReloadChangeBulletsTime;

    [Tooltip("HUD bullets to display bullet count on screen.")]
    [FormerlySerializedAs("HUD_bullets")]
    public TextMesh m_tmHudBullets;

    [Header("Crosshair properties")]
    [FormerlySerializedAs("horizontal_crosshair")] public Texture m_texHorizontalCrosshair;
    [FormerlySerializedAs("vertical_crosshair")] public Texture m_texVerticalCrosshair;
    [FormerlySerializedAs("top_pos_crosshair")] public Vector2 m_vTopPosCrosshair;
    [FormerlySerializedAs("bottom_pos_crosshair")] public Vector2 m_vBottomPosCrosshair;
    [FormerlySerializedAs("left_pos_crosshair")] public Vector2 m_vLeftPosCrosshair;
    [FormerlySerializedAs("right_pos_crosshair")] public Vector2 m_vRightPosCrosshair;
    [FormerlySerializedAs("size_crosshair_vertical")] public Vector2 m_vSizeCrosshairVertical = new Vector2(1f, 1f);
    [FormerlySerializedAs("size_crosshair_horizontal")] public Vector2 m_vSizeCrosshairHorizontal = new Vector2(1f, 1f);

    [HideInInspector]
    [FormerlySerializedAs("expandValues_crosshair")]
    public Vector2 m_vExpandValuesCrosshair;

    private float m_fFadeoutValue = 1f;

    [FormerlySerializedAs("handsAnimator")]
    public Animator m_animHands;

    [Header("Animation names")]
    [FormerlySerializedAs("reloadAnimationName")] public string m_strReloadAnimationName = "Player_Reload";
    [FormerlySerializedAs("aimingAnimationName")] public string m_strAimingAnimationName = "Player_AImpose";
    [FormerlySerializedAs("meeleAnimationName")] public string m_strMeleeAnimationName = "Character_Malee";

    // ==================== 외부 공개 프로퍼티 ====================
    public GunStyles CurrentStyle => m_eCurrentStyle;
    public float BulletsIHave => m_fBulletsIHave;
    public float BulletsInTheGun => m_fBulletsInTheGun;
    public bool IsReloading => m_bIsReloading;
    public bool IsMeleeAttack => m_bIsMeleeAttack;
    public bool IsAiming => m_bIsAiming;
    public Animator HandsAnimator => m_animHands;

    private void OnDisable()
    {
        m_bIsReloading = false;
        m_bIsMeleeAttack = false;
        m_bIsAiming = false;
        m_bIsAimingControlled = false;
    }

    public void Initialize(
        Transform _trPlayer,
        MouseLookScript _cMouseLook,
        Transform _trCamera,
        Camera _camSecond,
        GameObject _objBulletSpawnPlace,
        AudioSource _asHitMarker,
        TextMesh _tmHudBullets = null)
    {
        m_trPlayer = _trPlayer;
        m_cMouseLook = _cMouseLook;

        m_trMainCamera = _trCamera;
        m_camSecond = _camSecond;
        m_camMain = (_trCamera != null) ? _trCamera.GetComponent<Camera>() : null;

        if (m_trPlayer != null)
        {
            m_cPlayerMovement = m_trPlayer.GetComponent<PlayerMovementScript>();
            m_cPlayer = m_trPlayer.GetComponent<Player>();
        }

        m_objBulletSpawnPlace = _objBulletSpawnPlace;
        m_asHitMarker = _asHitMarker;
        if (_tmHudBullets != null)
            m_tmHudBullets = _tmHudBullets;

        m_fStartLook = m_fMouseSensitivityNotAiming;
        m_fStartAim = m_fMouseSensitivityAiming;
        m_fStartRun = m_fMouseSensitivityRunning;

        if (m_cMouseLook)
        {
            m_fRotationLastY = m_cMouseLook.CurrentYRotation;
            m_fRotationLastX = m_cMouseLook.CurrentCameraXRotation;
        }
        else
        {
            m_fRotationLastY = 0f;
            m_fRotationLastX = 0f;
        }
    }

    public void PlayHitMarkerSound()
    {
        if (m_asHitMarker != null)
            m_asHitMarker.Play();
    }

    public void UpdateProcess()
    {
        UpdateAnimatorParameters();
        ApplyCameraSensitivity();
        UpdateGunPosition();
        UpdateCameraSensitivityWhileMelee();
        UpdateCrosshairExpansion();

        float fRps = m_fRoundsPerSecond > 0f ? m_fRoundsPerSecond : 5f;
        m_fWaitTillNextFire -= fRps * Time.deltaTime;
    }

    public void FixedUpdateProcess()
    {
        UpdateGunRotation();
        UpdateAnimatorStates();

        if (m_bIsAimingControlled && !m_bIsReloading && !m_bIsMeleeAttack)
        {
            m_fGunPrecision = m_fGunPrecisionAiming;
            m_fRecoilAmountX = m_fRecoilAmountXAim;
            m_fRecoilAmountY = m_fRecoilAmountYAim;
            m_fRecoilAmountZ = m_fRecoilAmountZAim;
            m_vCurrentGunPosition = Vector3.SmoothDamp(m_vCurrentGunPosition, m_vAimPlacePosition, ref m_vGunPosVelocity, m_fGunAimTime);
            if (m_camMain)
                m_camMain.fieldOfView = Mathf.SmoothDamp(m_camMain.fieldOfView, m_fCameraZoomRatioAiming, ref m_fCameraZoomVelocity, m_fGunAimTime);
            if (m_camSecond)
                m_camSecond.fieldOfView = Mathf.SmoothDamp(m_camSecond.fieldOfView, m_fSecondCameraZoomRatioAiming, ref m_fSecondCameraZoomVelocity, m_fGunAimTime);
        }
        else
        {
            m_fGunPrecision = m_fGunPrecisionNotAiming;
            m_fRecoilAmountX = m_fRecoilAmountXNon;
            m_fRecoilAmountY = m_fRecoilAmountYNon;
            m_fRecoilAmountZ = m_fRecoilAmountZNon;
            m_vCurrentGunPosition = Vector3.SmoothDamp(m_vCurrentGunPosition, m_vRestPlacePosition, ref m_vGunPosVelocity, m_fGunAimTime);
            if (m_camMain)
                m_camMain.fieldOfView = Mathf.SmoothDamp(m_camMain.fieldOfView, m_fCameraZoomRatioNotAiming, ref m_fCameraZoomVelocity, m_fGunAimTime);
            if (m_camSecond)
                m_camSecond.fieldOfView = Mathf.SmoothDamp(m_camSecond.fieldOfView, m_fSecondCameraZoomRatioNotAiming, ref m_fSecondCameraZoomVelocity, m_fGunAimTime);
        }
    }

    // ==================== 마리오네트 사격/조준 API ====================

    public void SetAiming(bool _bAiming)
    {
        m_bIsAimingControlled = _bAiming;
    }

    public void TriggerShoot()
    {
        if (!m_bIsMeleeAttack)
        {
            ShootMethod();
        }
    }

    public void TriggerReload()
    {
        if (!m_bIsReloading && !m_bIsMeleeAttack)
        {
            StartCoroutine(ReloadAnimationCoroutine());
        }
    }

    public void TriggerMelee()
    {
        if (!m_bIsMeleeAttack)
        {
            StartCoroutine(MeleeAttackAnimationCoroutine());
        }
    }

    private void ApplyCameraSensitivity()
    {
        if (m_cMouseLook)
        {
            m_cMouseLook.SetSensitivity(m_fMouseSensitivityNotAiming, m_fMouseSensitivityAiming);
        }
    }

    private void UpdateCrosshairExpansion()
    {
        if (m_cPlayer != null && !m_cPlayer.IsPlayer) return;
        if (!m_trPlayer) return;

        Rigidbody rbPlayer = m_trPlayer.GetComponent<Rigidbody>();

        if (rbPlayer && rbPlayer.linearVelocity.magnitude > 1f && !m_bIsAimingControlled)
        {
            m_vExpandValuesCrosshair += new Vector2(20f, 40f) * Time.deltaTime;
            if (m_cPlayerMovement && m_cPlayerMovement.MaxSpeed < m_nRunningSpeed)
            {
                m_vExpandValuesCrosshair = new Vector2(Mathf.Clamp(m_vExpandValuesCrosshair.x, 0f, 10f), Mathf.Clamp(m_vExpandValuesCrosshair.y, 0f, 20f));
                m_fFadeoutValue = Mathf.Lerp(m_fFadeoutValue, 1f, Time.deltaTime * 2f);
            }
            else
            {
                m_fFadeoutValue = Mathf.Lerp(m_fFadeoutValue, 0f, Time.deltaTime * 10f);
                m_vExpandValuesCrosshair = new Vector2(Mathf.Clamp(m_vExpandValuesCrosshair.x, 0f, 20f), Mathf.Clamp(m_vExpandValuesCrosshair.y, 0f, 40f));
            }
        }
        else
        {
            m_vExpandValuesCrosshair = Vector2.Lerp(m_vExpandValuesCrosshair, Vector2.zero, Time.deltaTime * 5f);
            m_vExpandValuesCrosshair = new Vector2(Mathf.Clamp(m_vExpandValuesCrosshair.x, 0f, 10f), Mathf.Clamp(m_vExpandValuesCrosshair.y, 0f, 20f));
            m_fFadeoutValue = Mathf.Lerp(m_fFadeoutValue, 1f, Time.deltaTime * 2f);
        }
    }

    private void UpdateAnimatorStates()
    {
        if (m_animHands)
        {
            AnimatorStateInfo stateInfo = m_animHands.GetCurrentAnimatorStateInfo(0);
            m_bIsMeleeAttack = stateInfo.IsName(m_strMeleeAnimationName);
            m_bIsAiming = stateInfo.IsName(m_strAimingAnimationName);
        }
    }

    private IEnumerator MeleeAttackAnimationCoroutine()
    {
        if (m_animHands)
        {
            m_animHands.SetBool("meeleAttack", true);
            yield return new WaitForSeconds(0.1f);
            m_animHands.SetBool("meeleAttack", false);
        }
    }

    private void UpdateCameraSensitivityWhileMelee()
    {
        if (m_bIsMeleeAttack)
        {
            m_fMouseSensitivityNotAiming = 2f;
            m_fMouseSensitivityAiming = 1.6f;
            m_fMouseSensitivityRunning = 1f;
        }
        else
        {
            m_fMouseSensitivityNotAiming = m_fStartLook;
            m_fMouseSensitivityAiming = m_fStartAim;
            m_fMouseSensitivityRunning = m_fStartRun;
        }
    }

    private void UpdateGunPosition()
    {
        if (transform.parent != null)
        {
            Vector3 vTargetLocalPos = new Vector3(
                -(m_vCurrentGunPosition.x + m_fCurrentRecoilXPos),
                (m_vCurrentGunPosition.y + m_fCurrentRecoilYPos),
                (m_vCurrentGunPosition.z + m_fCurrentRecoilZPos)
            );
            transform.localPosition = Vector3.SmoothDamp(transform.localPosition, vTargetLocalPos, ref m_vVelV, 0f);

            if (m_cPlayerMovement)
                m_cPlayerMovement.m_vCameraPosition = new Vector3(m_fCurrentRecoilXPos, m_fCurrentRecoilYPos, 0f);

            m_fCurrentRecoilZPos = Mathf.SmoothDamp(m_fCurrentRecoilZPos, 0f, ref m_fVelocityZRecoil, m_fRecoilOverTimeZ);
            m_fCurrentRecoilXPos = Mathf.SmoothDamp(m_fCurrentRecoilXPos, 0f, ref m_fVelocityXRecoil, m_fRecoilOverTimeX);
            m_fCurrentRecoilYPos = Mathf.SmoothDamp(m_fCurrentRecoilYPos, 0f, ref m_fVelocityYRecoil, m_fRecoilOverTimeY);
            return;
        }

        Transform trRef = m_trMainCamera != null ? m_trMainCamera : m_trPlayer;
        if (trRef)
        {
            transform.position = Vector3.SmoothDamp(transform.position,
                trRef.position -
                (trRef.right * (m_vCurrentGunPosition.x + m_fCurrentRecoilXPos)) +
                (trRef.up * (m_vCurrentGunPosition.y + m_fCurrentRecoilYPos)) +
                (trRef.forward * (m_vCurrentGunPosition.z + m_fCurrentRecoilZPos)), ref m_vVelV, 0f);

            if (m_cPlayerMovement)
                m_cPlayerMovement.m_vCameraPosition = new Vector3(m_fCurrentRecoilXPos, m_fCurrentRecoilYPos, 0f);

            m_fCurrentRecoilZPos = Mathf.SmoothDamp(m_fCurrentRecoilZPos, 0f, ref m_fVelocityZRecoil, m_fRecoilOverTimeZ);
            m_fCurrentRecoilXPos = Mathf.SmoothDamp(m_fCurrentRecoilXPos, 0f, ref m_fVelocityXRecoil, m_fRecoilOverTimeX);
            m_fCurrentRecoilYPos = Mathf.SmoothDamp(m_fCurrentRecoilYPos, 0f, ref m_fVelocityYRecoil, m_fRecoilOverTimeY);
        }
    }

    private void UpdateGunRotation()
    {
        if (transform.parent != null)
        {
            if (m_cMouseLook)
            {
                m_fRotationDeltaY = m_cMouseLook.CurrentYRotation - m_fRotationLastY;
                m_fRotationDeltaX = m_cMouseLook.CurrentCameraXRotation - m_fRotationLastX;

                m_fRotationLastY = m_cMouseLook.CurrentYRotation;
                m_fRotationLastX = m_cMouseLook.CurrentCameraXRotation;

                m_fAngularVelocityY = Mathf.Lerp(m_fAngularVelocityY, m_fRotationDeltaY, Time.deltaTime * 5f);
                m_fAngularVelocityX = Mathf.Lerp(m_fAngularVelocityX, m_fRotationDeltaX, Time.deltaTime * 5f);

                transform.localRotation = Quaternion.Euler(
                    m_fAngularVelocityX * m_vForwardRotationAmount.x,
                    m_fAngularVelocityY * m_vForwardRotationAmount.y,
                    0f
                );
            }
            else
            {
                transform.localRotation = Quaternion.identity;
            }
            return;
        }

        if (m_cMouseLook)
        {
            m_fRotationDeltaY = m_cMouseLook.CurrentYRotation - m_fRotationLastY;
            m_fRotationDeltaX = m_cMouseLook.CurrentCameraXRotation - m_fRotationLastX;

            m_fRotationLastY = m_cMouseLook.CurrentYRotation;
            m_fRotationLastX = m_cMouseLook.CurrentCameraXRotation;

            m_fAngularVelocityY = Mathf.Lerp(m_fAngularVelocityY, m_fRotationDeltaY, Time.deltaTime * 5f);
            m_fAngularVelocityX = Mathf.Lerp(m_fAngularVelocityX, m_fRotationDeltaX, Time.deltaTime * 5f);

            m_fGunWeightX = Mathf.SmoothDamp(m_fGunWeightX, m_cMouseLook.CurrentCameraXRotation, ref m_vVelocityGunRotate.x, m_fRotationLagTime);
            m_fGunWeightY = Mathf.SmoothDamp(m_fGunWeightY, m_cMouseLook.CurrentYRotation, ref m_vVelocityGunRotate.y, m_fRotationLagTime);

            transform.rotation = Quaternion.Euler(m_fGunWeightX + (m_fAngularVelocityX * m_vForwardRotationAmount.x), m_fGunWeightY + (m_fAngularVelocityY * m_vForwardRotationAmount.y), 0f);
        }
        else if (m_trPlayer)
        {
            transform.rotation = m_trPlayer.rotation;
        }
    }

    public void ApplyRecoil()
    {
        m_fCurrentRecoilZPos -= m_fRecoilAmountZ;
        m_fCurrentRecoilXPos -= (Random.value - 0.5f) * m_fRecoilAmountX;
        m_fCurrentRecoilYPos -= (Random.value - 0.5f) * m_fRecoilAmountY;

        if (m_cMouseLook)
        {
            m_cMouseLook.WantedCameraXRotation -= Mathf.Abs(m_fCurrentRecoilYPos * m_fGunPrecision);
            m_cMouseLook.WantedYRotation -= (m_fCurrentRecoilXPos * m_fGunPrecision);
        }
        else if (transform.parent == null && m_trPlayer)
        {
            transform.rotation = m_trPlayer.rotation;
        }

        m_vExpandValuesCrosshair += new Vector2(6f, 12f);
    }

    public void GetBulletSpawnTransform(out Vector3 _vSpawnPosition, out Quaternion _qSpawnRotation)
    {
        bool bIsLocalPlayer = (m_cPlayer == null || m_cPlayer.IsPlayer) && m_trMainCamera != null;

        if (bIsLocalPlayer)
        {
            _qSpawnRotation = m_trMainCamera.rotation;

            if (m_objBulletSpawnPlace != null && m_objBulletSpawnPlace != m_trPlayer.gameObject)
                _vSpawnPosition = m_objBulletSpawnPlace.transform.position;
            else
                _vSpawnPosition = m_trMainCamera.position + (m_trMainCamera.forward * 0.4f);
        }
        else
        {
            Vector3 vForwardDir = m_trPlayer != null ? m_trPlayer.forward : transform.forward;

            if (m_objBulletSpawnPlace != null && m_objBulletSpawnPlace != m_trPlayer.gameObject)
                _vSpawnPosition = m_objBulletSpawnPlace.transform.position;
            else if (m_trPlayer != null)
                _vSpawnPosition = m_trPlayer.position + (Vector3.up * 1.4f) + (vForwardDir * 0.4f);
            else
                _vSpawnPosition = transform.position + (vForwardDir * 0.4f);

            if (m_cPlayer != null && m_cPlayer.AimTargetPosition.HasValue)
            {
                Vector3 vTargetPos = m_cPlayer.AimTargetPosition.Value;
                Vector3 vAimDir = (vTargetPos - _vSpawnPosition).normalized;
                if (vAimDir.sqrMagnitude > 0.001f)
                {
                    _qSpawnRotation = Quaternion.LookRotation(vAimDir, Vector3.up);
                    return;
                }
            }

            _qSpawnRotation = Quaternion.LookRotation(vForwardDir, Vector3.up);
        }
    }

    public void ShootMethod()
    {
        bool bCanShootSpeed = (m_cPlayerMovement == null || m_cPlayerMovement.MaxSpeed <= m_nRunningSpeed);

        if (m_fWaitTillNextFire <= 0f && !m_bIsReloading && bCanShootSpeed)
        {
            if (m_fBulletsInTheGun > 0f)
            {
                GetBulletSpawnTransform(out Vector3 vSpawnPos, out Quaternion qSpawnRot);

                if (m_prefabBullet)
                {
                    Instantiate(m_prefabBullet, vSpawnPos, qSpawnRot);
                }

                if (m_objMuzzleSpawn && m_arrPrefabMuzzleFlash != null && m_arrPrefabMuzzleFlash.Length > 0)
                {
                    int nRandomIdx = Random.Range(0, m_arrPrefabMuzzleFlash.Length);
                    if (m_arrPrefabMuzzleFlash[nRandomIdx] != null)
                    {
                        m_objHoldFlash = Instantiate(m_arrPrefabMuzzleFlash[nRandomIdx], m_objMuzzleSpawn.transform.position, m_objMuzzleSpawn.transform.rotation * Quaternion.Euler(0f, 0f, 90f));
                        m_objHoldFlash.transform.parent = m_objMuzzleSpawn.transform;
                    }
                }

                if (m_asShootSound)
                    m_asShootSound.Play();

                ApplyRecoil();

                m_fWaitTillNextFire = 1f;
                m_fBulletsInTheGun -= 1f;
            }
            else
            {
                StartCoroutine(ReloadAnimationCoroutine());
            }
        }
    }

    private IEnumerator ReloadAnimationCoroutine()
    {
        if (m_fBulletsIHave > 0f && m_fBulletsInTheGun < m_fAmountOfBulletsPerLoad && !m_bIsReloading)
        {
            if (m_asReloadSound && !m_asReloadSound.isPlaying)
            {
                m_asReloadSound.Play();
            }

            if (m_animHands)
            {
                m_animHands.SetBool("reloading", true);
                yield return new WaitForSeconds(0.5f);
                m_animHands.SetBool("reloading", false);
            }

            yield return new WaitForSeconds(m_fReloadChangeBulletsTime - 0.5f);

            if (!m_bIsMeleeAttack && m_cPlayerMovement && m_cPlayerMovement.MaxSpeed != m_nRunningSpeed)
            {
                if (m_cPlayerMovement.m_asReloadVoiceSound)
                    m_cPlayerMovement.m_asReloadVoiceSound.Play();

                float fNeeded = m_fAmountOfBulletsPerLoad - m_fBulletsInTheGun;
                if (m_fBulletsIHave >= fNeeded)
                {
                    m_fBulletsIHave -= fNeeded;
                    m_fBulletsInTheGun = m_fAmountOfBulletsPerLoad;
                }
                else
                {
                    m_fBulletsInTheGun += m_fBulletsIHave;
                    m_fBulletsIHave = 0f;
                }
            }
            else
            {
                if (m_asReloadSound)
                    m_asReloadSound.Stop();
            }
        }
    }

    private void OnGUI()
    {
        if (m_cPlayer != null && !m_cPlayer.IsPlayer) return;
        if (m_camMain == null) return;

        if (!m_tmHudBullets && GameManager.s_cInstance != null)
        {
            m_tmHudBullets = GameManager.s_cInstance.HudBullets;
        }

        if (m_cMouseLook && m_tmHudBullets)
            m_tmHudBullets.text = $"{m_fBulletsIHave} - {m_fBulletsInTheGun}";

        DrawCrosshair();
    }

    private void DrawCrosshair()
    {
        GUI.color = new Color(GUI.color.r, GUI.color.g, GUI.color.b, m_fFadeoutValue);
        if (!m_bIsAimingControlled)
        {
            GUI.DrawTexture(new Rect(ToScreenVec2(m_vLeftPosCrosshair).x + ToScreenX(-m_vExpandValuesCrosshair.x) + Screen.width / 2f, Screen.height / 2f + ToScreenVec2(m_vLeftPosCrosshair).y, ToScreenVec2(m_vSizeCrosshairHorizontal).x, ToScreenVec2(m_vSizeCrosshairHorizontal).y), m_texVerticalCrosshair);
            GUI.DrawTexture(new Rect(ToScreenVec2(m_vRightPosCrosshair).x + ToScreenX(m_vExpandValuesCrosshair.x) + Screen.width / 2f, Screen.height / 2f + ToScreenVec2(m_vRightPosCrosshair).y, ToScreenVec2(m_vSizeCrosshairHorizontal).x, ToScreenVec2(m_vSizeCrosshairHorizontal).y), m_texVerticalCrosshair);

            GUI.DrawTexture(new Rect(ToScreenVec2(m_vTopPosCrosshair).x + Screen.width / 2f, Screen.height / 2f + ToScreenVec2(m_vTopPosCrosshair).y + ToScreenY(-m_vExpandValuesCrosshair.y), ToScreenVec2(m_vSizeCrosshairVertical).x, ToScreenVec2(m_vSizeCrosshairVertical).y), m_texHorizontalCrosshair);
            GUI.DrawTexture(new Rect(ToScreenVec2(m_vBottomPosCrosshair).x + Screen.width / 2f, Screen.height / 2f + ToScreenVec2(m_vBottomPosCrosshair).y + ToScreenY(m_vExpandValuesCrosshair.y), ToScreenVec2(m_vSizeCrosshairVertical).x, ToScreenVec2(m_vSizeCrosshairVertical).y), m_texHorizontalCrosshair);
        }
    }

    private float ToScreenX(float _fVal) => Screen.width * _fVal / 100f;
    private float ToScreenY(float _fVal) => Screen.height * _fVal / 100f;
    private Vector2 ToScreenVec2(Vector2 _vVal) => new Vector2(Screen.width * _vVal.x / 100f, Screen.height * _vVal.y / 100f);

    private void UpdateAnimatorParameters()
    {
        if (m_animHands && m_cPlayerMovement)
        {
            m_bIsReloading = m_animHands.GetCurrentAnimatorStateInfo(0).IsName(m_strReloadAnimationName);

            m_animHands.SetFloat("walkSpeed", m_cPlayerMovement.CurrentSpeed);
            m_animHands.SetBool("aiming", m_bIsAimingControlled);
            m_animHands.SetInteger("maxSpeed", m_cPlayerMovement.MaxSpeed);
        }
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawCube(transform.position, Vector2.one);
    }
}
