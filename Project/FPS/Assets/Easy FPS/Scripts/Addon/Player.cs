using UnityEngine;

public class Player : MonoBehaviour
{
    [Header("Player Settings")]
    public bool isPlayer = true;
    [Tooltip("카메라가 없을 때(NPC) 플레이어의 Forward를 기준으로 키보드 입력에 맞춰 함께 이동할지 여부")]
    public bool syncMovementWithPlayer = true;

    [SerializeField] Transform viewPivot;
    [SerializeField] GameObject bulletSpawnPlace;

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

    bool isInitGunScript = false;

    Transform trCurrentCamera;
    Camera currentSecondCamera;

    void Awake()
    {
        m_cMouseLockScript = this.GetComponent<MouseLookScript>();
        m_cPlayerMovementScript = this.GetComponent<PlayerMovementScript>();
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

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        if (isInitGunScript && gun) gun.UpdateProcess();
    }

    private void FixedUpdate()
    {
        if (isInitGunScript && gun) gun.FixedUpdateProcess();
    }

    public void Initialize(Transform trCamera, Camera secsecond)
    {
        trCurrentCamera = trCamera;
        currentSecondCamera = secsecond;

        if (m_cPlayerMovementScript)
            m_cPlayerMovementScript.syncMovementWithPlayer = syncMovementWithPlayer;

        // 플레이어의 경우 GameManager에 지정된 BulletSpawnPlace가 있으면 기본으로 연결
        if (isPlayer && !bulletSpawnPlace && GameManager.instance && GameManager.instance.BulletSpawnPlace)
        {
            bulletSpawnPlace = GameManager.instance.BulletSpawnPlace;
        }

        if (gunInventory)
            gunInventory.Initialize();

        GunChangeSetUpdate(trCamera, secsecond);
    }

    public void GunChangeSetUpdate(Transform trMainCamera, Camera secondCamera)
    {
        trCurrentCamera = trMainCamera;
        currentSecondCamera = secondCamera;

        if (gunInventory && gunInventory.currentGun)
        {
            Transform foundMarker = gunInventory.currentGun.transform.Find("hitMarkerSound");
            if (foundMarker)
                hitMarker = foundMarker.GetComponent<AudioSource>();

            gun = gunInventory.currentGun.GetComponent<GunScript>();
            if (gun)
            {
                Transform pivotToUse = (trMainCamera != null) ? trMainCamera : ViewPivot;
                GameObject spawnToUse = bulletSpawnPlace;
                if (isPlayer && !spawnToUse && GameManager.instance && GameManager.instance.BulletSpawnPlace)
                    spawnToUse = GameManager.instance.BulletSpawnPlace;

                gun.Initialize(this.transform, m_cMouseLockScript, pivotToUse, secondCamera, spawnToUse, hitMarker);
                isInitGunScript = true;
            }
        }
    }
}
