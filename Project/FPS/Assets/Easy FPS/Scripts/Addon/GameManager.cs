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
        // 카메라 오브젝트에 실수로 부착된 중복 Player 컴포넌트가 있다면 제거
        if (mainCamera)
        {
            Player strayPlayerOnCam = mainCamera.GetComponent<Player>();
            if (strayPlayerOnCam != null && strayPlayerOnCam != cPlayer)
            {
                Destroy(strayPlayerOnCam);
            }
        }

        if (cPlayer)
        {
            // 동일 오브젝트에 중복 부착된 Player 컴포넌트 정리
            Player[] playerComps = cPlayer.GetComponents<Player>();
            for (int i = 0; i < playerComps.Length; i++)
            {
                if (playerComps[i] != cPlayer)
                {
                    Destroy(playerComps[i]);
                }
            }

            cPlayer.isPlayer = true;
            cPlayer.EnsureComponents();

            if (mainCamera)
            {
                mainCamera.parent = cPlayer.ViewPivot;
                mainCamera.localPosition = Vector3.zero;
                mainCamera.localRotation = Quaternion.identity;
                cameraComponent = mainCamera.GetComponent<Camera>();
            }

            cPlayer.Initialize(mainCamera, secondCamera);

            if (MouseLookScript)
                MouseLookScript.Initialize(mainCamera);

            // 플레이어에 PlayerController가 없으면 자동 추가
            if (!cPlayer.GetComponent<PlayerController>())
            {
                cPlayer.gameObject.AddComponent<PlayerController>();
            }
        }

        if (cNpc)
        {
            Player[] npcComps = cNpc.GetComponents<Player>();
            for (int i = 0; i < npcComps.Length; i++)
            {
                if (npcComps[i] != cNpc)
                {
                    Destroy(npcComps[i]);
                }
            }

            cNpc.isPlayer = false;
            cNpc.EnsureComponents();
            cNpc.Initialize(null, null);

            // NPC에 AIController가 없으면 자동 추가
            if (!cNpc.GetComponent<AIController>())
            {
                cNpc.gameObject.AddComponent<AIController>();
            }
        }

        // 디버거가 씬에 없으면 GameManager에 자동 부착
        if (!FindFirstObjectByType<ControllerDebugger>())
        {
            gameObject.AddComponent<ControllerDebugger>();
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
