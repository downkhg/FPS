using UnityEngine;
using UnityEngine.Serialization;
using System.Collections;

[RequireComponent(typeof(Rigidbody))]
public class PlayerMovementScript : MonoBehaviour
{
    private Rigidbody m_rbBody;

    [Tooltip("Current players speed")]
    [FormerlySerializedAs("currentSpeed")]
    public float m_fCurrentSpeed;

    [Tooltip("Force that moves player into jump")]
    [FormerlySerializedAs("jumpForce")]
    public float m_fJumpForce = 500f;

    [Tooltip("Position of the camera inside the player")]
    [HideInInspector]
    [FormerlySerializedAs("cameraPosition")]
    public Vector3 m_vCameraPosition;

    [Header("Marionette / NPC Sync Settings")]
    [Tooltip("카메라가 없을 때(NPC) 플레이어의 Forward를 기준으로 키보드 입력에 맞춰 함께 이동할지 여부")]
    [FormerlySerializedAs("syncMovementWithPlayer")]
    public bool m_bSyncMovementWithPlayer = true;

    [Tooltip("기준으로 삼을 플레이어 Transform (비워두면 GameManager.s_cInstance.CurrentPlayer 참조)")]
    [FormerlySerializedAs("referencePlayerTransform")]
    public Transform m_trReferencePlayer;

    private Player m_cPlayer;

    // 마리오네트 제어를 위한 입력 캐싱
    private Vector2 m_vCurrentInputMove;
    private Vector3 m_vCurrentWorldMove;
    private bool m_bHasWorldMove = false;
    private bool m_bIsCrouching = false;

    private Vector3 m_vSlowdownVelocity;
    private Vector2 m_vHorizontalMovement;

    [Tooltip("The maximum speed you want to achieve")]
    [FormerlySerializedAs("maxSpeed")]
    public int m_nMaxSpeed = 5;

    [Tooltip("The higher the number the faster it will stop")]
    [FormerlySerializedAs("deaccelerationSpeed")]
    public float m_fDeaccelerationSpeed = 15.0f;

    [Tooltip("Force that is applied when moving forward or backward")]
    [FormerlySerializedAs("accelerationSpeed")]
    public float m_fAccelerationSpeed = 50000.0f;

    [Tooltip("Tells us weather the player is grounded or not.")]
    [FormerlySerializedAs("grounded")]
    public bool m_bIsGrounded;

    [Header("Shooting Properties")]
    private LayerMask m_maskIgnoreLayer;
    private float m_fRayDetectorMeleeSpace = 0.15f;
    private float m_fOffsetStart = 0.05f;

    [Tooltip("Put BulletSpawn gameobject here, palce from where bullets are created.")]
    [FormerlySerializedAs("bulletSpawn")]
    public Transform m_trBulletSpawn;

    [FormerlySerializedAs("been_to_meele_anim")]
    public bool m_bHasTriggeredMeleeHit = false;

    [Header("BloodForMelleAttaacks")]
    [Tooltip("Put your particle blood effect here.")]
    [FormerlySerializedAs("bloodEffect")]
    public GameObject m_prefabBloodEffect;

    [Header("Player SOUNDS")]
    [Tooltip("Jump sound when player jumps.")]
    [FormerlySerializedAs("_jumpSound")]
    public AudioSource m_asJumpSound;

    [Tooltip("Sound while player makes when successfully reloads weapon.")]
    [FormerlySerializedAs("_freakingZombiesSound")]
    public AudioSource m_asReloadVoiceSound;

    [Tooltip("Sound Bullet makes when hits target.")]
    [FormerlySerializedAs("_hitSound")]
    public AudioSource m_asHitSound;

    [Tooltip("Walk sound player makes.")]
    [FormerlySerializedAs("_walkSound")]
    public AudioSource m_asWalkSound;

    [Tooltip("Run Sound player makes.")]
    [FormerlySerializedAs("_runSound")]
    public AudioSource m_asRunSound;

    // ==================== 프로퍼티 ====================
    public float CurrentSpeed
    {
        get => m_fCurrentSpeed;
        set => m_fCurrentSpeed = value;
    }

    public int MaxSpeed
    {
        get => m_nMaxSpeed;
        set => m_nMaxSpeed = value;
    }

    public bool Grounded => m_bIsGrounded;

    void Awake()
    {
        m_rbBody = GetComponent<Rigidbody>();
        m_cPlayer = GetComponent<Player>();
        m_maskIgnoreLayer = 1 << LayerMask.NameToLayer("Player");

        if (!m_trBulletSpawn)
        {
            Transform trFoundSpawn = transform.Find("BulletSpawn");
            if (trFoundSpawn) m_trBulletSpawn = trFoundSpawn;
            else m_trBulletSpawn = this.transform;
        }
    }

    private void Initialize(Transform _trMainCamera, Transform _trBulletSpawn)
    {
        m_rbBody = GetComponent<Rigidbody>();
        m_cPlayer = GetComponent<Player>();
        if (_trBulletSpawn) m_trBulletSpawn = _trBulletSpawn;
        m_maskIgnoreLayer = 1 << LayerMask.NameToLayer("Player");
    }

    void FixedUpdate()
    {
        RaycastForMeleeAttacks();
        PlayerMovementLogic();
    }

    void PlayerMovementLogic()
    {
        m_fCurrentSpeed = m_rbBody.linearVelocity.magnitude;
        m_vHorizontalMovement = new Vector2(m_rbBody.linearVelocity.x, m_rbBody.linearVelocity.z);
        if (m_vHorizontalMovement.magnitude > m_nMaxSpeed)
        {
            m_vHorizontalMovement = m_vHorizontalMovement.normalized;
            m_vHorizontalMovement *= m_nMaxSpeed;
        }

        m_rbBody.linearVelocity = new Vector3(
            m_vHorizontalMovement.x,
            m_rbBody.linearVelocity.y,
            m_vHorizontalMovement.y
        );

        if (m_bIsGrounded)
        {
            m_rbBody.linearVelocity = Vector3.SmoothDamp(
                m_rbBody.linearVelocity,
                new Vector3(0, m_rbBody.linearVelocity.y, 0),
                ref m_vSlowdownVelocity,
                m_fDeaccelerationSpeed
            );
        }

        float fHInput = m_vCurrentInputMove.x;
        float fVInput = m_vCurrentInputMove.y;
        float fForceMultiplier = m_bIsGrounded ? m_fAccelerationSpeed : (m_fAccelerationSpeed / 2f);

        if (m_bHasWorldMove)
        {
            // [AI 컨트롤러 / 월드 방향 이동]
            Vector3 vMoveDir = Vector3.ProjectOnPlane(m_vCurrentWorldMove, Vector3.up).normalized;
            m_rbBody.AddForce(vMoveDir * fForceMultiplier * Time.deltaTime, ForceMode.Force);

            m_fDeaccelerationSpeed = (m_vCurrentWorldMove.sqrMagnitude > 0.01f) ? 0.5f : 0.1f;
        }
        else
        {
            // [플레이어 컨트롤러 / 로컬 축 기준 이동]
            m_rbBody.AddRelativeForce(fHInput * fForceMultiplier * Time.deltaTime, 0, fVInput * fForceMultiplier * Time.deltaTime);

            if (fHInput != 0 || fVInput != 0)
            {
                m_fDeaccelerationSpeed = 0.5f;
            }
            else
            {
                m_fDeaccelerationSpeed = 0.1f;
            }
        }
    }

    // ==================== 마리오네트 제어 API ====================

    public void SetInputMove(Vector2 _vMoveInput)
    {
        m_bHasWorldMove = false;
        m_vCurrentInputMove = _vMoveInput;
    }

    public void SetWorldMove(Vector3 _vWorldDirection)
    {
        m_bHasWorldMove = true;
        m_vCurrentWorldMove = _vWorldDirection;
        m_vCurrentInputMove = new Vector2(_vWorldDirection.x, _vWorldDirection.z);
    }

    public void DoJump()
    {
        if (m_bIsGrounded)
        {
            m_rbBody.AddRelativeForce(Vector3.up * m_fJumpForce);
            if (m_asJumpSound) m_asJumpSound.Play();
            if (m_asWalkSound) m_asWalkSound.Stop();
            if (m_asRunSound) m_asRunSound.Stop();
        }
    }

    public void DoCrouch(bool _bEnable)
    {
        m_bIsCrouching = _bEnable;
    }

    void Update()
    {
        CrouchingProcess();
        WalkingSound();
    }

    void CrouchingProcess()
    {
        if (m_bIsCrouching)
        {
            transform.localScale = Vector3.Lerp(transform.localScale, new Vector3(1, 0.6f, 1), Time.deltaTime * 15f);
        }
        else
        {
            transform.localScale = Vector3.Lerp(transform.localScale, new Vector3(1, 1, 1), Time.deltaTime * 15f);
        }
    }

    void WalkingSound()
    {
        if (m_asWalkSound && m_asRunSound)
        {
            if (RayCastGrounded())
            {
                if (m_fCurrentSpeed > 1f)
                {
                    if (m_nMaxSpeed == 3)
                    {
                        if (!m_asWalkSound.isPlaying)
                        {
                            m_asWalkSound.Play();
                            m_asRunSound.Stop();
                        }
                    }
                    else if (m_nMaxSpeed == 5)
                    {
                        if (!m_asRunSound.isPlaying)
                        {
                            m_asWalkSound.Stop();
                            m_asRunSound.Play();
                        }
                    }
                }
                else
                {
                    m_asWalkSound.Stop();
                    m_asRunSound.Stop();
                }
            }
            else
            {
                m_asWalkSound.Stop();
                m_asRunSound.Stop();
            }
        }
    }

    private bool RayCastGrounded()
    {
        RaycastHit sGroundedInfo;
        if (Physics.Raycast(transform.position, transform.up * -1f, out sGroundedInfo, 1f, ~m_maskIgnoreLayer))
        {
            Debug.DrawRay(transform.position, transform.up * -1f, Color.red, 0.0f);
            if (sGroundedInfo.transform != null)
            {
                return true;
            }
        }
        return false;
    }

    void OnCollisionStay(Collision _cOther)
    {
        foreach (ContactPoint sContact in _cOther.contacts)
        {
            if (Vector2.Angle(sContact.normal, Vector3.up) < 60f)
            {
                m_bIsGrounded = true;
            }
        }
    }

    void OnCollisionExit()
    {
        m_bIsGrounded = false;
    }

    /// <summary>
    /// 근접 공격 상태일 때만 9개의 판정 Ray를 생성하여 충돌을 검사합니다.
    /// </summary>
    private void RaycastForMeleeAttacks()
    {
        if (m_trBulletSpawn == null || m_cPlayer == null) return;

        GunScript cGun = m_cPlayer.Gun;
        if (cGun == null) return;

        if (!cGun.IsMeleeAttack)
        {
            m_bHasTriggeredMeleeHit = false;
            return;
        }

        if (m_bHasTriggeredMeleeHit) return;

        m_bHasTriggeredMeleeHit = true;
        StartCoroutine(MeleeAttackWeaponHit(cGun));
    }

    private IEnumerator MeleeAttackWeaponHit(GunScript _cGun)
    {
        Vector3 vOrigin = m_trBulletSpawn.position;
        Vector3 vRight = m_trBulletSpawn.right;
        Vector3 vUp = m_trBulletSpawn.up;
        Vector3 vForward = m_trBulletSpawn.forward;

        Ray[] arrRays = new Ray[9]
        {
            // middle row
            new Ray(vOrigin + (vRight * m_fOffsetStart), vForward + (vRight * m_fRayDetectorMeleeSpace)),
            new Ray(vOrigin - (vRight * m_fOffsetStart), vForward - (vRight * m_fRayDetectorMeleeSpace)),
            new Ray(vOrigin, vForward),
            // upper row
            new Ray(vOrigin + (vRight * m_fOffsetStart) + (vUp * m_fOffsetStart), vForward + (vRight * m_fRayDetectorMeleeSpace) + (vUp * m_fRayDetectorMeleeSpace)),
            new Ray(vOrigin - (vRight * m_fOffsetStart) + (vUp * m_fOffsetStart), vForward - (vRight * m_fRayDetectorMeleeSpace) + (vUp * m_fRayDetectorMeleeSpace)),
            new Ray(vOrigin + (vUp * m_fOffsetStart), vForward + (vUp * m_fRayDetectorMeleeSpace)),
            // bottom row
            new Ray(vOrigin + (vRight * m_fOffsetStart) - (vUp * m_fOffsetStart), vForward + (vRight * m_fRayDetectorMeleeSpace) - (vUp * m_fRayDetectorMeleeSpace)),
            new Ray(vOrigin - (vRight * m_fOffsetStart) - (vUp * m_fOffsetStart), vForward - (vRight * m_fRayDetectorMeleeSpace) - (vUp * m_fRayDetectorMeleeSpace)),
            new Ray(vOrigin - (vUp * m_fOffsetStart), vForward - (vUp * m_fRayDetectorMeleeSpace))
        };

        RaycastHit sHitInfo;
        for (int i = 0; i < arrRays.Length; i++)
        {
            if (Physics.Raycast(arrRays[i], out sHitInfo, 2f, ~m_maskIgnoreLayer))
            {
                if (sHitInfo.transform.CompareTag("Dummie"))
                {
                    InstantiateBlood(sHitInfo, _cGun);
                    break;
                }
            }
        }

        yield return new WaitForEndOfFrame();
    }

    private void InstantiateBlood(RaycastHit _sHitPos, GunScript _cGun)
    {
        if (_cGun != null)
        {
            _cGun.PlayHitMarkerSound();
        }

        if (m_asHitSound)
            m_asHitSound.Play();

        if (m_prefabBloodEffect)
            Instantiate(m_prefabBloodEffect, _sHitPos.point, Quaternion.identity);
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.blue;
        Gizmos.DrawCube(this.transform.position, Vector2.one);
    }
}
