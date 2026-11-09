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
        }
    }

    private void Awake()
    {
        instance = this;
        Initialize();
    }

    void Update()
    {
        // 각 Player 인스턴스의 Update()에서 자신의 총기 및 컴포넌트를 직접 업데이트합니다.
    }

    private void FixedUpdate()
    {
        // 각 Player 인스턴스의 FixedUpdate()에서 자신의 총기 및 컴포넌트를 직접 업데이트합니다.
    }
}
