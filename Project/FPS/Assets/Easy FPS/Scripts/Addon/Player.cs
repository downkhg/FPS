using System.Collections.Generic;
using UnityEngine;

public class Player : MonoBehaviour
{
    [Header("Player Settings")]
    public bool isPlayer = true;
    [Tooltip("카메라가 없을 때(NPC) 플레이어의 Forward를 기준으로 키보드 입력에 맞춰 함께 이동할지 여부")]
    public bool syncMovementWithPlayer = true;

    [SerializeField] Transform viewPivot;
    [SerializeField] GameObject bulletSpawnPlace;

    [Header("Weapon Management (Runtime Weapon Pool)")]
    [Tooltip("런타임에 생성하여 풀(Pool)로 등록할 무기 프리팹 목록 (Resources 폴더 기준)")]
    public List<string> weaponPrefabNames = new List<string> { "NewGun_semi", "NewGun_auto" };
    [Tooltip("런타임에 생성/등록되어 SetActive로 제어되는 무기 인스턴스 풀")]
    [SerializeField] private List<GunScript> weaponPool = new List<GunScript>();
    [SerializeField] private int currentWeaponIndex = 0;

    MouseLookScript m_cMouseLockScript;
    PlayerMovementScript m_cPlayerMovementScript;
    [SerializeField] GunScript gun;
    [SerializeField] AudioSource hitMarker;

    public GunInventory gunInventory;

    public MouseLookScript Mls => m_cMouseLockScript;
    public PlayerMovementScript PmS => m_cPlayerMovementScript;
    public GunScript Gun => gun;
    public AudioSource HitMarker => hitMarker;
    public Transform ViewPivot => viewPivot ? viewPivot : this.transform;
    public GameObject BulletSpawnPlace => bulletSpawnPlace;
    public int CurrentWeaponIndex => currentWeaponIndex;
    public IReadOnlyList<GunScript> Weapons => weaponPool;

    // NPC 또는 컨트롤러가 조준하고 있는 월드 목표 지점
    public Vector3? AimTargetPosition { get; set; }

    bool isInitGunScript = false;
    bool isInitialized = false;

    Transform trCurrentCamera;
    Camera currentSecondCamera;

    void Awake()
    {
        EnsureComponents();
    }

    /// <summary>
    /// Awake() 호출 순서와 무관하게 필수 컴포넌트를 안전하게 캐싱합니다.
    /// </summary>
    public void EnsureComponents()
    {
        if (!m_cMouseLockScript)
            m_cMouseLockScript = this.GetComponent<MouseLookScript>();
        if (!m_cPlayerMovementScript)
            m_cPlayerMovementScript = this.GetComponent<PlayerMovementScript>();
        if (!gunInventory)
            gunInventory = this.GetComponent<GunInventory>();

        if (m_cPlayerMovementScript)
            m_cPlayerMovementScript.syncMovementWithPlayer = syncMovementWithPlayer;

        if (!viewPivot)
        {
            Transform foundPivot = transform.Find("ViewPivot");
            if (foundPivot) viewPivot = foundPivot;
            else viewPivot = this.transform;
        }

        if (!bulletSpawnPlace)
        {
            Transform foundBulletSpawn = transform.Find("BulletSpawn");
            if (foundBulletSpawn) bulletSpawnPlace = foundBulletSpawn.gameObject;
        }
    }

    void OnValidate()
    {
        if (m_cPlayerMovementScript)
            m_cPlayerMovementScript.syncMovementWithPlayer = syncMovementWithPlayer;
    }

    void Update()
    {
        if (isInitGunScript && gun && gun.gameObject.activeInHierarchy)
            gun.UpdateProcess();
    }

    private void FixedUpdate()
    {
        if (isInitGunScript && gun && gun.gameObject.activeInHierarchy)
            gun.FixedUpdateProcess();
    }

    public void Initialize(Transform trCamera, Camera secsecond)
    {
        EnsureComponents();

        trCurrentCamera = trCamera;
        currentSecondCamera = secsecond;

        if (m_cPlayerMovementScript)
            m_cPlayerMovementScript.syncMovementWithPlayer = syncMovementWithPlayer;

        if (isPlayer && !bulletSpawnPlace && GameManager.instance && GameManager.instance.BulletSpawnPlace)
        {
            bulletSpawnPlace = GameManager.instance.BulletSpawnPlace;
        }

        // 무기들을 Player(카메라 또는 ViewPivot)의 자식으로 초기화 및 풀링
        InitializeWeapons();

        if (gunInventory)
            gunInventory.Initialize();

        isInitialized = true;
    }

    /// <summary>
    /// PC와 NPC 프리팹을 가볍고 독립적으로 유지할 수 있도록,
    /// 사용할 무기 목록을 런타임에 1회 생성(Instantiate)하여 자식 풀(Pool)로 구축하고
    /// 이후 무기 교체는 SetActive(true/false)로만 처리합니다.
    /// </summary>
    private void InitializeWeapons()
    {
        Transform weaponParent = (isPlayer && trCurrentCamera != null) ? trCurrentCamera : ViewPivot;

        // GunInventory에 무기 목록이 지정되어 있다면 우선 동기화
        if (gunInventory != null && gunInventory.gunsIHave != null && gunInventory.gunsIHave.Count > 0)
        {
            weaponPrefabNames = new List<string>(gunInventory.gunsIHave);
        }

        // 1. 기존 풀에서 유효하지 않은 참조 제거
        weaponPool.RemoveAll(w => w == null);

        // 2. 프리팹에 미리 무기가 박혀있지 않은 경우(기본 권장 사항: 프리팹 경량화),
        //    weaponPrefabNames 목록에 있는 모든 무기들을 런타임에 1:1로 생성하여 풀에 등록
        if (weaponPool.Count == 0 && weaponPrefabNames != null && weaponPrefabNames.Count > 0)
        {
            for (int i = 0; i < weaponPrefabNames.Count; i++)
            {
                string prefabName = weaponPrefabNames[i];
                if (string.IsNullOrEmpty(prefabName)) continue;

                GameObject res = Resources.Load<GameObject>(prefabName);
                if (res != null)
                {
                    GameObject weaponObj = Instantiate(res, weaponParent);
                    weaponObj.name = $"{prefabName}_{gameObject.name}_slot{i}";
                    weaponObj.transform.localPosition = Vector3.zero;
                    weaponObj.transform.localRotation = Quaternion.identity;

                    GunScript gs = weaponObj.GetComponent<GunScript>();
                    if (gs != null)
                    {
                        weaponPool.Add(gs);
                    }
                    weaponObj.SetActive(false);
                    Debug.LogWarning($"[{gameObject.name}] Resources에서 무기 '{weaponObj.name}' 로드 성공.");
                }
                else
                {
                    Debug.LogWarning($"[{gameObject.name}] Resources에서 무기 '{prefabName}'을(를) 로드할 수 없습니다.");
                }
            }
        }
        // 인스펙터/프리팹에 이미 일부 자식 무기가 등록되어 있는 경우 부족한 슬롯만큼 추가 생성
        else if (weaponPrefabNames != null && weaponPool.Count < weaponPrefabNames.Count)
        {
            int startIndex = weaponPool.Count;
            for (int i = startIndex; i < weaponPrefabNames.Count; i++)
            {
                string prefabName = weaponPrefabNames[i];
                if (string.IsNullOrEmpty(prefabName)) continue;

                GameObject res = Resources.Load<GameObject>(prefabName);
                if (res != null)
                {
                    GameObject weaponObj = Instantiate(res, weaponParent);
                    weaponObj.name = $"{prefabName}_{gameObject.name}_slot{i}";
                    weaponObj.transform.localPosition = Vector3.zero;
                    weaponObj.transform.localRotation = Quaternion.identity;

                    GunScript gs = weaponObj.GetComponent<GunScript>();
                    if (gs != null)
                    {
                        weaponPool.Add(gs);
                    }
                    weaponObj.SetActive(false);
                }
            }
        }

        // 3. 풀의 모든 무기에 Player 의존성 주입 및 부모 계층 연결
        Transform pivotToUse = (trCurrentCamera != null) ? trCurrentCamera : ViewPivot;
        GameObject spawnToUse = bulletSpawnPlace;
        if (isPlayer && !spawnToUse && GameManager.instance && GameManager.instance.BulletSpawnPlace)
            spawnToUse = GameManager.instance.BulletSpawnPlace;

        for (int i = 0; i < weaponPool.Count; i++)
        {
            GunScript gs = weaponPool[i];
            if (gs == null) continue;

            if (gs.transform.parent != weaponParent)
            {
                gs.transform.SetParent(weaponParent, false);
            }

            Transform foundMarker = gs.transform.Find("hitMarkerSound");
            AudioSource marker = foundMarker ? foundMarker.GetComponent<AudioSource>() : hitMarker;

            gs.Initialize(this.transform, m_cMouseLockScript, pivotToUse, currentSecondCamera, spawnToUse, marker);
        }

        Debug.Log($"[{gameObject.name}] 런타임 무기 풀 초기화 완료: 총 {weaponPool.Count}개 무기 준비됨");

        // 4. 0번 슬롯(기본 무기)만 활성화하고 나머지는 비활성화
        if (weaponPool.Count > 0)
        {
            int startSlot = Mathf.Clamp(currentWeaponIndex, 0, weaponPool.Count - 1);
            EquipWeapon(startSlot, true);
        }
    }

    /// <summary>
    /// 무기 풀(Pool)에서 지정된 인덱스의 무기만 활성화(SetActive(true))하고 나머지는 비활성화(SetActive(false))합니다.
    /// </summary>
    private void EquipWeapon(int index, bool force = false)
    {
        if (weaponPool == null || weaponPool.Count == 0) return;
        if (index < 0 || index >= weaponPool.Count) return;
        if (!force && currentWeaponIndex == index && gun != null && gun.gameObject.activeSelf) return;

        if (!force && gunInventory != null && gunInventory.weaponChanging != null)
        {
            gunInventory.weaponChanging.Play();
        }

        // 풀(Pool)에 있는 무기들을 순회하며 선택된 인덱스만 SetActive(true)
        for (int i = 0; i < weaponPool.Count; i++)
        {
            if (weaponPool[i] != null)
            {
                weaponPool[i].gameObject.SetActive(i == index);
            }
        }

        currentWeaponIndex = index;
        gun = weaponPool[currentWeaponIndex];

        if (gun != null)
        {
            Transform foundMarker = gun.transform.Find("hitMarkerSound");
            if (foundMarker)
                hitMarker = foundMarker.GetComponent<AudioSource>();

            Transform pivotToUse = (trCurrentCamera != null) ? trCurrentCamera : ViewPivot;
            GameObject spawnToUse = bulletSpawnPlace;
            if (isPlayer && !spawnToUse && GameManager.instance && GameManager.instance.BulletSpawnPlace)
                spawnToUse = GameManager.instance.BulletSpawnPlace;

            gun.Initialize(this.transform, m_cMouseLockScript, pivotToUse, currentSecondCamera, spawnToUse, hitMarker);
            isInitGunScript = true;
        }
        else
        {
            isInitGunScript = false;
        }

        // HUD 아이콘 표시용 GunInventory 상태 동기화
        if (gunInventory != null)
        {
            gunInventory.SyncWeaponState(currentWeaponIndex, gun != null ? gun.gameObject : null);
        }
    }

    public void GunChangeSetUpdate(Transform trMainCamera, Camera secondCamera)
    {
        trCurrentCamera = trMainCamera;
        currentSecondCamera = secondCamera;
        EquipWeapon(currentWeaponIndex, true);
    }

    // ==================== 마리오네트 제어 API ====================

    public void Move(Vector2 inputDirection)
    {
        if (m_cPlayerMovementScript)
            m_cPlayerMovementScript.SetInputMove(inputDirection);
    }

    public void MoveWorld(Vector3 worldDirection)
    {
        if (m_cPlayerMovementScript)
            m_cPlayerMovementScript.SetWorldMove(worldDirection);
    }

    public void LookAtTarget(Vector3 targetPosition)
    {
        AimTargetPosition = targetPosition;

        Vector3 targetDir = targetPosition - transform.position;
        targetDir.y = 0; // 수평 회전
        if (targetDir.sqrMagnitude > 0.001f)
        {
            Quaternion targetRot = Quaternion.LookRotation(targetDir, Vector3.up);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, Time.deltaTime * 15f);
        }
    }

    public void Jump()
    {
        if (m_cPlayerMovementScript)
            m_cPlayerMovementScript.DoJump();
    }

    public void Crouch(bool enable)
    {
        if (m_cPlayerMovementScript)
            m_cPlayerMovementScript.DoCrouch(enable);
    }

    public void Shoot()
    {
        if (gun)
            gun.TriggerShoot();
    }

    public void Aim(bool enable)
    {
        if (gun)
            gun.SetAiming(enable);
    }

    public void Reload()
    {
        if (gun)
            gun.TriggerReload();
    }

    public void Melee()
    {
        if (gun)
            gun.TriggerMelee();
    }

    public void SwitchWeapon(int weaponIndex)
    {
        EquipWeapon(weaponIndex, false);
    }

    public void NextWeapon()
    {
        if (weaponPool == null || weaponPool.Count == 0) return;
        int next = (currentWeaponIndex + 1) % weaponPool.Count;
        Debug.Log($"NextWepon:{next}");
        EquipWeapon(next, false);
    }
}
