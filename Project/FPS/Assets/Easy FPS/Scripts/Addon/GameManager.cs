using UnityEngine;

public class GameManager : MonoBehaviour
{
    MouseLookScript mls;
    Camera cameraComponent;
    PlayerMovementScript pmS;
    GunInventory gunInventory;

    [SerializeField] Transform mainCamera;
    [SerializeField] Camera secondCamera;
    [SerializeField] Transform player;
    [SerializeField] Transform gunPlaceHolder;
    [SerializeField] GameObject bulletSpawnPlace;
    [SerializeField] AudioSource hitMarker;
    [SerializeField] TextMesh HUD_bullets;
    [SerializeField] GameObject weapon;
    [SerializeField] GunScript gun;
    [SerializeField] MouseLookScript MouseLookScript;

    // Public Getters
    public MouseLookScript Mls => mls;
    public Transform MainCamera => mainCamera;
    public Camera SecondCamera => secondCamera;
    public Transform Player => player;
    public Camera CameraComponent => cameraComponent;
    public Transform GunPlaceHolder => gunPlaceHolder;
    public PlayerMovementScript PmS => pmS;
    public GameObject BulletSpawnPlace => bulletSpawnPlace;
    public AudioSource HitMarker => hitMarker;
    public TextMesh HudBullets => HUD_bullets;
    public GameObject Weapon => weapon;
    public GunScript Gun => gun;

    public static GameManager instance;

    public static GameManager returm;

    private void Initialize()
    {
        mls = Player.GetComponent<MouseLookScript>();
        cameraComponent = mainCamera.GetComponent<Camera>();
        pmS = player.GetComponent<PlayerMovementScript>();
        gunInventory = player.GetComponent<GunInventory>();

        gunInventory.Initialize();
        MouseLookScript.Initialize(mainCamera);
    }

    private void Awake()
    {
        Initialize();
    }

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        instance = this;
    }

    // Update is called once per frame
    void Update()
    {
        if (gunInventory && gunInventory.currentGun)
        {
            if (!hitMarker)
                hitMarker = gunInventory.currentGun.transform.Find("hitMarkerSound").GetComponent<AudioSource>();
            if (!gun)
                gun = gunInventory.currentGun.GetComponent<GunScript>();
        }
    }
}