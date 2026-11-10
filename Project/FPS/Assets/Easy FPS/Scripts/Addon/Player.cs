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

    [Header("Weapon Management (Marionette Direct Control)")]
    [Tooltip("보유 무기 프리팹 이름 목록 (Resources 폴더 기준)")]
    public List<string> weaponPrefabNames = new List<string> { "NewGun_semi", "NewGun_auto" };
    [Tooltip("자식으로 등록/생성된 무기 인스턴스 목록")]
    [SerializeField] private List<GunScript> weapons = new List<GunScript>();
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
    public IReadOnlyList<GunScript> Weapons => weapons;

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
    /// 무기를 씬 루트에 매번 생성/파괴하지 않고, Player의 자식 계층으로 한 번만 배치하여 활성화/비활성화로 제어합니다.
    /// </summary>
    private void InitializeWeapons()
    {
        Transform weaponParent = (isPlayer && trCurrentCamera != null) ? trCurrentCamera : ViewPivot;

        // GunInventory에 설정된 무기 목록이 있다면 동기화
        if (gunInventory != null && gunInventory.gunsIHave != null && gunInventory.gunsIHave.Count > 0)
        {
            weaponPrefabNames = new List<string>(gunInventory.gunsIHave);
        }

        // 이미 자식 계층에 배치된 GunScript가 있는지 확인
        weapons.RemoveAll(w => w == null);
        if (weapons.Count == 0)
        {
            GunScript[] existingChildGuns = GetComponentsInChildren<GunScript>(true);
            if (existingChildGuns != null && existingChildGuns.Length > 0)
            {
                weapons.AddRange(existingChildGuns);
            }
        }

        // 자식에 무기가 아직 없다면 Resources에서 로드하여 Player(weaponParent)의 자식으로 1회 생성
        if (weapons.Count == 0 && weaponPrefabNames != null)
        {
            for (int i = 0; i < weaponPrefabNames.Count; i++)
            {
                string weaponName = weaponPrefabNames[i];
                if (string.IsNullOrEmpty(weaponName)) continue;

                GameObject resource = Resources.Load<GameObject>(weaponName);
                if (resource == null)
                {
                    Debug.LogWarning($"[{gameObject.name}] Resources에서 무기 '{weaponName}'을(를) 찾을 수 없습니다.");
                    continue;
                }

                GameObject weaponObj = Instantiate(resource, weaponParent);
                weaponObj.name = $"{weaponName}_{gameObject.name}";
                weaponObj.transform.localPosition = Vector3.zero;
                weaponObj.transform.localRotation = Quaternion.identity;

                GunScript gs = weaponObj.GetComponent<GunScript>();
                if (gs != null)
                {
                    weapons.Add(gs);
                }
                weaponObj.SetActive(false);
            }
        }

        // 모든 보유 무기에 Player 의존성 주입 및 부모 계층 정렬
        Transform pivotToUse = (trCurrentCamera != null) ? trCurrentCamera : ViewPivot;
        GameObject spawnToUse = bulletSpawnPlace;
        if (isPlayer && !spawnToUse && GameManager.instance && GameManager.instance.BulletSpawnPlace)
            spawnToUse = GameManager.instance.BulletSpawnPlace;

        for (int i = 0; i < weapons.Count; i++)
        {
            GunScript gs = weapons[i];
            if (gs == null) continue;

            if (gs.transform.parent != weaponParent)
            {
                gs.transform.SetParent(weaponParent, false);
            }

            Transform foundMarker = gs.transform.Find("hitMarkerSound");
            AudioSource marker = foundMarker ? foundMarker.GetComponent<AudioSource>() : hitMarker;

            gs.Initialize(this.transform, m_cMouseLockScript, pivotToUse, currentSecondCamera, spawnToUse, marker);
        }

        // 기본 무기(0번 슬롯) 즉시 장착
        if (weapons.Count > 0)
        {
            int startSlot = Mathf.Clamp(currentWeaponIndex, 0, weapons.Count - 1);
            EquipWeapon(startSlot, true);
        }
    }

    /// <summary>
    /// 지정된 인덱스의 무기를 즉시 활성화하고 나머지 무기를 비활성화합니다.
    /// </summary>
    private void EquipWeapon(int index, bool force = false)
    {
        if (weapons == null || weapons.Count == 0) return;
        if (index < 0 || index >= weapons.Count) return;
        if (!force && currentWeaponIndex == index && gun != null && gun.gameObject.activeSelf) return;

        if (!force && gunInventory != null && gunInventory.weaponChanging != null)
        {
            gunInventory.weaponChanging.Play();
        }

        for (int i = 0; i < weapons.Count; i++)
        {
            if (weapons[i] != null)
            {
                weapons[i].gameObject.SetActive(i == index);
            }
        }

        currentWeaponIndex = index;
        gun = weapons[currentWeaponIndex];

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
        if (weapons == null || weapons.Count == 0) return;
        int next = (currentWeaponIndex + 1) % weapons.Count;
        EquipWeapon(next, false);
    }
}
