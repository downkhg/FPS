using UnityEngine;
using UnityEngine.Timeline;

public class GameManager : MonoBehaviour
{
    Camera cameraComponent;
 
    //MouseLookScript mls;
    //PlayerMovementScript pmS;
    
    //[SerializeField] GunScript gun;
    //[SerializeField] AudioSource hitMarker;

    //GunInventory gunInventory;

    [SerializeField] Player cPlayer;

    [SerializeField] MouseLookScript MouseLookScript;
    [SerializeField] TextMesh HUD_bullets;
    [SerializeField] Transform mainCamera;
    [SerializeField] Camera secondCamera;

    [SerializeField] Transform trLevelFloor;

    [SerializeField] Transform trPlayer;
    [SerializeField] GameObject bulletSpawnPlace;
    
    public TextMesh HudBullets => HUD_bullets;
    public Transform MainCamera => mainCamera;
    public Camera SecondCamera => secondCamera;
    public Transform transformLevelFloor => trLevelFloor;

    public Transform Player => trPlayer;
    public GameObject BulletSpawnPlace => bulletSpawnPlace;
    //public GunScript Gun => gun;
    //public AudioSource HitMarker => hitMarker;


    // Public Getters
    //public MouseLookScript Mls => mls;
   
    public Camera CameraComponent => cameraComponent;
    
    //public PlayerMovementScript PmS => pmS;
   
   
    //public GameObject Weapon => weapon;
   

    public static GameManager instance;

    public static GameManager returm;

    public void Initialize()
    {
        cPlayer = trPlayer.GetComponent<Player>();
        cPlayer.Initialize(mainCamera, secondCamera);
        //mls = Player.GetComponent<MouseLookScript>();
        cameraComponent = mainCamera.GetComponent<Camera>();
        //pmS = player.GetComponent<PlayerMovementScript>();
        //gunInventory = player.GetComponent<GunInventory>();

        //gunInventory.Initialize();
        MouseLookScript.Initialize(mainCamera);

        cPlayer.GunChangeSetUpdate(mainCamera, secondCamera);
    }

    public void GunChangeSetUpdate()
    {
        //if (gunInventory && gunInventory.currentGun)
        //{
        //    if (!hitMarker)
        //        hitMarker = gunInventory.currentGun.transform.Find("hitMarkerSound").GetComponent<AudioSource>();
        //    if (!gun)
        //    {
        //        gun = gunInventory.currentGun.GetComponent<GunScript>();
        //        gun.Initialize(mls, mainCamera, secondCamera, bulletSpawnPlace, hitMarker);
        //    }
        //}
        //
    }

    private void Awake()
    {
        instance = this;
        Initialize();
    }

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
       
    }

    // Update is called once per frame
    void Update()
    {
        //GunChangeSetUpdate();
        if(cPlayer) cPlayer.GunChangeSetUpdate(mainCamera, secondCamera);
    }
}