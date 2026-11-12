using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

public class Player : MonoBehaviour
{
    [Header("Player Settings")]
    [FormerlySerializedAs("isPlayer")]
    public bool m_bIsPlayer = true;

    [Tooltip("카메라가 없을 때(NPC) 플레이어의 Forward를 기준으로 키보드 입력에 맞춰 함께 이동할지 여부")]
    [FormerlySerializedAs("syncMovementWithPlayer")]
    public bool m_bSyncMovementWithPlayer = true;

    [SerializeField, FormerlySerializedAs("viewPivot")]
    private Transform m_trViewPivot;

    [SerializeField, FormerlySerializedAs("bulletSpawnPlace")]
    private GameObject m_objBulletSpawnPlace;

    [Header("Weapon Management (Runtime Weapon Pool)")]
    [Tooltip("런타임에 생성하여 풀(Pool)로 등록할 무기 프리팹 목록 (Resources 폴더 기준)")]
    [FormerlySerializedAs("weaponPrefabNames")]
    public List<string> m_listWeaponPrefabNames = new List<string> { "NewGun_semi", "NewGun_auto" };

    [Tooltip("런타임에 생성/등록되어 SetActive로 제어되는 무기 인스턴스 풀")]
    [SerializeField, FormerlySerializedAs("weaponPool")]
    private List<GunScript> m_listWeaponPool = new List<GunScript>();

    [SerializeField, FormerlySerializedAs("currentWeaponIndex")]
    private int m_nCurrentWeaponIndex = 0;

    private MouseLookScript m_cMouseLook;
    private PlayerMovementScript m_cPlayerMovement;

    [SerializeField, FormerlySerializedAs("gun")]
    private GunScript m_cCurrentGun;

    [SerializeField, FormerlySerializedAs("hitMarker")]
    private AudioSource m_asHitMarker;

    [FormerlySerializedAs("gunInventory")]
    public GunInventory m_cGunInventory;

    private bool m_bIsInitGunScript = false;
    private bool m_bIsInitialized = false;

    private Transform m_trCurrentCamera;
    private Camera m_camSecond;

    // ==================== 프로퍼티 ====================
    public bool IsPlayer
    {
        get => m_bIsPlayer;
        set => m_bIsPlayer = value;
    }

    public MouseLookScript Mls => m_cMouseLook;
    public PlayerMovementScript PmS => m_cPlayerMovement;
    public GunScript Gun => m_cCurrentGun;
    public AudioSource HitMarker => m_asHitMarker;
    public Transform ViewPivot => m_trViewPivot ? m_trViewPivot : this.transform;
    public GameObject BulletSpawnPlace => m_objBulletSpawnPlace;
    public int CurrentWeaponIndex => m_nCurrentWeaponIndex;
    public IReadOnlyList<GunScript> Weapons => m_listWeaponPool;

    // NPC 또는 컨트롤러가 조준하고 있는 월드 목표 지점
    public Vector3? AimTargetPosition { get; set; }

    void Awake()
    {
        EnsureComponents();
    }

    /// <summary>
    /// Awake() 호출 순서와 무관하게 필수 컴포넌트를 안전하게 캐싱합니다.
    /// </summary>
    public void EnsureComponents()
    {
        if (!m_cMouseLook)
            m_cMouseLook = this.GetComponent<MouseLookScript>();
        if (!m_cPlayerMovement)
            m_cPlayerMovement = this.GetComponent<PlayerMovementScript>();
        if (!m_cGunInventory)
            m_cGunInventory = this.GetComponent<GunInventory>();

        if (m_cPlayerMovement)
            m_cPlayerMovement.m_bSyncMovementWithPlayer = m_bSyncMovementWithPlayer;

        if (!m_trViewPivot)
        {
            Transform trFoundPivot = transform.Find("ViewPivot");
            if (trFoundPivot) m_trViewPivot = trFoundPivot;
            else m_trViewPivot = this.transform;
        }

        if (!m_objBulletSpawnPlace)
        {
            Transform trFoundBulletSpawn = transform.Find("BulletSpawn");
            if (trFoundBulletSpawn) m_objBulletSpawnPlace = trFoundBulletSpawn.gameObject;
        }
    }

    void OnValidate()
    {
        if (m_cPlayerMovement)
            m_cPlayerMovement.m_bSyncMovementWithPlayer = m_bSyncMovementWithPlayer;
    }

    void Update()
    {
        if (m_bIsInitGunScript && m_cCurrentGun && m_cCurrentGun.gameObject.activeInHierarchy)
            m_cCurrentGun.UpdateProcess();
    }

    private void FixedUpdate()
    {
        if (m_bIsInitGunScript && m_cCurrentGun && m_cCurrentGun.gameObject.activeInHierarchy)
            m_cCurrentGun.FixedUpdateProcess();
    }

    public void Initialize(Transform _trCamera, Camera _camSecond)
    {
        EnsureComponents();

        m_trCurrentCamera = _trCamera;
        m_camSecond = _camSecond;

        if (m_cPlayerMovement)
            m_cPlayerMovement.m_bSyncMovementWithPlayer = m_bSyncMovementWithPlayer;

        if (m_bIsPlayer && !m_objBulletSpawnPlace && GameManager.s_cInstance && GameManager.s_cInstance.BulletSpawnPlace)
        {
            m_objBulletSpawnPlace = GameManager.s_cInstance.BulletSpawnPlace;
        }

        // 무기들을 Player(카메라 또는 ViewPivot)의 자식으로 초기화 및 풀링
        InitializeWeapons();

        if (m_cGunInventory)
            m_cGunInventory.Initialize();

        m_bIsInitialized = true;
    }

    /// <summary>
    /// PC와 NPC 프리팹을 가볍고 독립적으로 유지할 수 있도록,
    /// 사용할 무기 목록을 런타임에 1회 생성(Instantiate)하여 자식 풀(Pool)로 구축하고
    /// 이후 무기 교체는 SetActive(true/false)로만 처리합니다.
    /// </summary>
    private void InitializeWeapons()
    {
        Transform trWeaponParent = (m_bIsPlayer && m_trCurrentCamera != null) ? m_trCurrentCamera : ViewPivot;

        // GunInventory에 무기 목록이 지정되어 있다면 우선 동기화
        if (m_cGunInventory != null && m_cGunInventory.GunsIHave != null && m_cGunInventory.GunsIHave.Count > 0)
        {
            m_listWeaponPrefabNames = new List<string>(m_cGunInventory.GunsIHave);
        }

        // 1. 기존 풀에서 유효하지 않은 참조 제거
        m_listWeaponPool.RemoveAll(w => w == null);

        // 2. 프리팹에 미리 무기가 박혀있지 않은 경우,
        //    m_listWeaponPrefabNames 목록에 있는 모든 무기들을 런타임에 1:1로 생성하여 풀에 등록
        if (m_listWeaponPool.Count == 0 && m_listWeaponPrefabNames != null && m_listWeaponPrefabNames.Count > 0)
        {
            for (int i = 0; i < m_listWeaponPrefabNames.Count; i++)
            {
                string strPrefabName = m_listWeaponPrefabNames[i];
                if (string.IsNullOrEmpty(strPrefabName)) continue;

                GameObject prefabWeapon = Resources.Load<GameObject>(strPrefabName);
                if (prefabWeapon != null)
                {
                    GameObject objWeapon = Instantiate(prefabWeapon, trWeaponParent);
                    objWeapon.name = $"{strPrefabName}_{gameObject.name}_slot{i}";
                    objWeapon.transform.localPosition = Vector3.zero;
                    objWeapon.transform.localRotation = Quaternion.identity;

                    GunScript cGun = objWeapon.GetComponent<GunScript>();
                    if (cGun != null)
                    {
                        m_listWeaponPool.Add(cGun);
                    }
                    objWeapon.SetActive(false);
                }
                else
                {
                    Debug.LogWarning($"[{gameObject.name}] Resources에서 무기 '{strPrefabName}'을(를) 로드할 수 없습니다.");
                }
            }
        }
        // 인스펙터/프리팹에 이미 일부 자식 무기가 등록되어 있는 경우 부족한 슬롯만큼 추가 생성
        else if (m_listWeaponPrefabNames != null && m_listWeaponPool.Count < m_listWeaponPrefabNames.Count)
        {
            int nStartIndex = m_listWeaponPool.Count;
            for (int i = nStartIndex; i < m_listWeaponPrefabNames.Count; i++)
            {
                string strPrefabName = m_listWeaponPrefabNames[i];
                if (string.IsNullOrEmpty(strPrefabName)) continue;

                GameObject prefabWeapon = Resources.Load<GameObject>(strPrefabName);
                if (prefabWeapon != null)
                {
                    GameObject objWeapon = Instantiate(prefabWeapon, trWeaponParent);
                    objWeapon.name = $"{strPrefabName}_{gameObject.name}_slot{i}";
                    objWeapon.transform.localPosition = Vector3.zero;
                    objWeapon.transform.localRotation = Quaternion.identity;

                    GunScript cGun = objWeapon.GetComponent<GunScript>();
                    if (cGun != null)
                    {
                        m_listWeaponPool.Add(cGun);
                    }
                    objWeapon.SetActive(false);
                }
            }
        }

        // 3. 풀의 모든 무기에 Player 의존성 주입 및 부모 계층 연결
        Transform trPivotToUse = (m_trCurrentCamera != null) ? m_trCurrentCamera : ViewPivot;
        GameObject objSpawnToUse = m_objBulletSpawnPlace;
        if (m_bIsPlayer && !objSpawnToUse && GameManager.s_cInstance && GameManager.s_cInstance.BulletSpawnPlace)
            objSpawnToUse = GameManager.s_cInstance.BulletSpawnPlace;

        TextMesh tmHudBullets = (m_bIsPlayer && GameManager.s_cInstance != null) ? GameManager.s_cInstance.HudBullets : null;

        for (int i = 0; i < m_listWeaponPool.Count; i++)
        {
            GunScript cGun = m_listWeaponPool[i];
            if (cGun == null) continue;

            if (cGun.transform.parent != trWeaponParent)
            {
                cGun.transform.SetParent(trWeaponParent, false);
            }

            Transform trFoundMarker = cGun.transform.Find("hitMarkerSound");
            AudioSource asMarker = trFoundMarker ? trFoundMarker.GetComponent<AudioSource>() : m_asHitMarker;

            cGun.Initialize(this.transform, m_cMouseLook, trPivotToUse, m_camSecond, objSpawnToUse, asMarker, tmHudBullets);
        }

        // 4. 0번 슬롯(기본 무기)만 활성화하고 나머지는 비활성화
        if (m_listWeaponPool.Count > 0)
        {
            int nStartSlot = Mathf.Clamp(m_nCurrentWeaponIndex, 0, m_listWeaponPool.Count - 1);
            EquipWeapon(nStartSlot, true);
        }
    }

    /// <summary>
    /// 무기 풀(Pool)에서 지정된 인덱스의 무기만 활성화(SetActive(true))하고 나머지는 비활성화(SetActive(false))합니다.
    /// </summary>
    private void EquipWeapon(int _nIndex, bool _bForce = false)
    {
        if (m_listWeaponPool == null || m_listWeaponPool.Count == 0) return;
        if (_nIndex < 0 || _nIndex >= m_listWeaponPool.Count) return;
        if (!_bForce && m_nCurrentWeaponIndex == _nIndex && m_cCurrentGun != null && m_cCurrentGun.gameObject.activeSelf) return;

        if (!_bForce && m_cGunInventory != null && m_cGunInventory.WeaponChanging != null)
        {
            m_cGunInventory.WeaponChanging.Play();
        }

        // 풀(Pool)에 있는 무기들을 순회하며 선택된 인덱스만 SetActive(true)
        for (int i = 0; i < m_listWeaponPool.Count; i++)
        {
            if (m_listWeaponPool[i] != null)
            {
                m_listWeaponPool[i].gameObject.SetActive(i == _nIndex);
            }
        }

        m_nCurrentWeaponIndex = _nIndex;
        m_cCurrentGun = m_listWeaponPool[m_nCurrentWeaponIndex];

        if (m_cCurrentGun != null)
        {
            Transform trFoundMarker = m_cCurrentGun.transform.Find("hitMarkerSound");
            if (trFoundMarker)
                m_asHitMarker = trFoundMarker.GetComponent<AudioSource>();

            Transform trPivotToUse = (m_trCurrentCamera != null) ? m_trCurrentCamera : ViewPivot;
            GameObject objSpawnToUse = m_objBulletSpawnPlace;
            if (m_bIsPlayer && !objSpawnToUse && GameManager.s_cInstance && GameManager.s_cInstance.BulletSpawnPlace)
                objSpawnToUse = GameManager.s_cInstance.BulletSpawnPlace;

            TextMesh tmHudBullets = (m_bIsPlayer && GameManager.s_cInstance != null) ? GameManager.s_cInstance.HudBullets : null;

            m_cCurrentGun.Initialize(this.transform, m_cMouseLook, trPivotToUse, m_camSecond, objSpawnToUse, m_asHitMarker, tmHudBullets);
            m_bIsInitGunScript = true;
        }
        else
        {
            m_bIsInitGunScript = false;
        }

        // HUD 아이콘 표시용 GunInventory 상태 동기화
        if (m_cGunInventory != null)
        {
            m_cGunInventory.SyncWeaponState(m_nCurrentWeaponIndex, m_cCurrentGun != null ? m_cCurrentGun.gameObject : null);
        }
    }

    public void GunChangeSetUpdate(Transform _trMainCamera, Camera _camSecond)
    {
        m_trCurrentCamera = _trMainCamera;
        m_camSecond = _camSecond;
        EquipWeapon(m_nCurrentWeaponIndex, true);
    }

    // ==================== 마리오네트 제어 API ====================

    public void Move(Vector2 _vInputDirection)
    {
        if (m_cPlayerMovement)
            m_cPlayerMovement.SetInputMove(_vInputDirection);
    }

    public void MoveWorld(Vector3 _vWorldDirection)
    {
        if (m_cPlayerMovement)
            m_cPlayerMovement.SetWorldMove(_vWorldDirection);
    }

    public void LookAtTarget(Vector3 _vTargetPosition)
    {
        AimTargetPosition = _vTargetPosition;

        Vector3 vTargetDir = _vTargetPosition - transform.position;
        vTargetDir.y = 0f; // 수평 회전
        if (vTargetDir.sqrMagnitude > 0.001f)
        {
            Quaternion qTargetRot = Quaternion.LookRotation(vTargetDir, Vector3.up);
            transform.rotation = Quaternion.Slerp(transform.rotation, qTargetRot, Time.deltaTime * 15f);
        }
    }

    public void Jump()
    {
        if (m_cPlayerMovement)
            m_cPlayerMovement.DoJump();
    }

    public void Crouch(bool _bEnable)
    {
        if (m_cPlayerMovement)
            m_cPlayerMovement.DoCrouch(_bEnable);
    }

    public void Shoot()
    {
        if (m_cCurrentGun)
            m_cCurrentGun.TriggerShoot();
    }

    public void Aim(bool _bEnable)
    {
        if (m_cCurrentGun)
            m_cCurrentGun.SetAiming(_bEnable);
    }

    public void Reload()
    {
        if (m_cCurrentGun)
            m_cCurrentGun.TriggerReload();
    }

    public void Melee()
    {
        if (m_cCurrentGun)
            m_cCurrentGun.TriggerMelee();
    }

    public void SwitchWeapon(int _nWeaponIndex)
    {
        EquipWeapon(_nWeaponIndex, false);
    }

    public void NextWeapon()
    {
        if (m_listWeaponPool == null || m_listWeaponPool.Count == 0) return;
        int nNext = (m_nCurrentWeaponIndex + 1) % m_listWeaponPool.Count;
        EquipWeapon(nNext, false);
    }
}
