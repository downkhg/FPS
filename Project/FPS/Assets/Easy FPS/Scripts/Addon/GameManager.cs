using UnityEngine;

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
    
    public Player CurrentPlayer => cPlayer;
    public Player CurrentNpc => cNpc;

    public static GameManager instance;

    public void Initialize()
    {
        if (cPlayer)
        {
            cPlayer.isPlayer = true;
            cPlayer.Initialize(mainCamera, secondCamera);

            // 플레이어에 PlayerController가 없으면 자동 추가
            if (!cPlayer.GetComponent<PlayerController>())
            {
                cPlayer.gameObject.AddComponent<PlayerController>();
            }

            if (mainCamera)
            {
                mainCamera.parent = cPlayer.ViewPivot;
                mainCamera.localPosition = Vector3.zero;
                mainCamera.localRotation = Quaternion.identity;
                cameraComponent = mainCamera.GetComponent<Camera>();
            }

            if (MouseLookScript)
                MouseLookScript.Initialize(mainCamera);
        }

        if (cNpc)
        {
            cNpc.isPlayer = false;
            cNpc.Initialize(null, null);

            // NPC에 AIController가 없으면 자동 추가
            if (!cNpc.GetComponent<AIController>())
            {
                cNpc.gameObject.AddComponent<AIController>();
            }
        }
    }

    private void Awake()
    {
        instance = this;
        Initialize();
    }

    void Update()
    {
        
    }

    private void FixedUpdate()
    {
        
    }
}
