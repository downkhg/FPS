using NUnit.Framework;
using UnityEngine;
using UnityEngine.Timeline;

public class GameManager : MonoBehaviour
{
    [SerializeField] GameObject prefabPlayer;

    Camera cameraComponent;

    [SerializeField] Player cPlayer;
    [SerializeField] Player cNpc;

    [SerializeField] MouseLookScript MouseLookScript;
    [SerializeField] TextMesh HUD_bullets;
    [SerializeField] Transform mainCamera;
    [SerializeField] Camera secondCamera;

    [SerializeField] Transform trLevelFloor;

    [SerializeField] GameObject bulletSpawnPlace;
    
    public TextMesh HudBullets => HUD_bullets;
    public Transform MainCamera => mainCamera;
    public Camera SecondCamera => secondCamera;
    public Transform transformLevelFloor => trLevelFloor;

    public GameObject BulletSpawnPlace => bulletSpawnPlace;
   
    public Camera CameraComponent => cameraComponent;
    
    public static GameManager instance;

    public void Initialize()
    {
        //cPlayer = ((GameObject)Instantiate(prefabPlayer, Vector3.up * 1.5f, Quaternion.identity)).GetComponent<Player>();
        cPlayer.Initialize(mainCamera, secondCamera);
        cNpc.Initialize(null, null);    

        mainCamera.parent = cPlayer.transform;
        mainCamera.localPosition = Vector3.zero;

        cameraComponent = mainCamera.GetComponent<Camera>();
 
        MouseLookScript.Initialize(mainCamera);

        cPlayer.GunChangeSetUpdate(mainCamera, secondCamera);
        if(cNpc) cNpc.GunChangeSetUpdate(null, null);
    }

    private void Awake()
    {
        instance = this;
        Initialize();
    }

    // Update is called once per frame
    void Update()
    {
        if(cPlayer) cPlayer.GunChangeSetUpdate(mainCamera, secondCamera);
        if(cNpc) cNpc.GunChangeSetUpdate(null, null);
    }
}