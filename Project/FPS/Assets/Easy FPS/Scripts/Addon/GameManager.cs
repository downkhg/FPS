using UnityEngine;
using UnityEngine.Serialization;

public class GameManager : MonoBehaviour
{
    [SerializeField, FormerlySerializedAs("prefabPlayer")]
    private GameObject m_prefabPlayer;

    private Camera m_camMainComponent;

    [SerializeField, FormerlySerializedAs("cPlayer")]
    private Player m_cPlayer;

    [SerializeField, FormerlySerializedAs("cNpc")]
    private Player m_cNpc;

    [SerializeField, FormerlySerializedAs("MouseLookScript")]
    private MouseLookScript m_cMouseLook;

    [SerializeField, FormerlySerializedAs("HUD_bullets")]
    private TextMesh m_tmHudBullets;

    [SerializeField, FormerlySerializedAs("mainCamera")]
    private Transform m_trMainCamera;

    [SerializeField, FormerlySerializedAs("secondCamera")]
    private Camera m_camSecond;

    [SerializeField, FormerlySerializedAs("trLevelFloor")]
    private Transform m_trLevelFloor;

    [SerializeField, FormerlySerializedAs("bulletSpawnPlace")]
    private GameObject m_objBulletSpawnPlace;

    [SerializeField]
    private GUIManager m_cGuiManager;

    [SerializeField]
    private PlayModeManager m_cPlayModeManager;

    public static GameManager s_cInstance;
    public static GameManager instance => s_cInstance;

    // ==================== 프로퍼티 ====================
    public TextMesh HudBullets => m_tmHudBullets;
    public Transform MainCamera => m_trMainCamera;
    public Camera SecondCamera => m_camSecond;
    public Transform TransformLevelFloor => m_trLevelFloor;
    public GameObject BulletSpawnPlace => m_objBulletSpawnPlace;
    public Camera CameraComponent => m_camMainComponent;
    public Player CurrentPlayer => m_cPlayer;
    public Player CurrentNpc => m_cNpc;
    public GUIManager GuiManager => m_cGuiManager;
    public PlayModeManager PlayModeMgr => m_cPlayModeManager;

    private void Awake()
    {
        s_cInstance = this;
        Initialize();
    }

    public void Initialize()
    {
        // 카메라 오브젝트에 실수로 부착된 중복 Player 컴포넌트가 있다면 제거
        if (m_trMainCamera)
        {
            Player cStrayPlayerOnCam = m_trMainCamera.GetComponent<Player>();
            if (cStrayPlayerOnCam != null && cStrayPlayerOnCam != m_cPlayer)
            {
                Destroy(cStrayPlayerOnCam);
            }
        }

        if (m_cPlayer)
        {
            // 동일 오브젝트에 중복 부착된 Player 컴포넌트 정리
            Player[] arrPlayerComps = m_cPlayer.GetComponents<Player>();
            for (int i = 0; i < arrPlayerComps.Length; i++)
            {
                if (arrPlayerComps[i] != m_cPlayer)
                {
                    Destroy(arrPlayerComps[i]);
                }
            }

            m_cPlayer.IsPlayer = true;
            m_cPlayer.EnsureComponents();

            if (m_trMainCamera)
            {
                m_trMainCamera.parent = m_cPlayer.ViewPivot;
                m_trMainCamera.localPosition = Vector3.zero;
                m_trMainCamera.localRotation = Quaternion.identity;
                m_camMainComponent = m_trMainCamera.GetComponent<Camera>();
            }

            m_cPlayer.Initialize(m_trMainCamera, m_camSecond);

            if (m_cMouseLook)
                m_cMouseLook.Initialize(m_trMainCamera);

            // 플레이어에 PlayerController가 없으면 자동 추가
            if (!m_cPlayer.GetComponent<PlayerController>())
            {
                m_cPlayer.gameObject.AddComponent<PlayerController>();
            }

            // PC를 RTS 모드에서 지휘관 유닛(아군)으로 선택/명령 가능하도록 RtsUnitAgent 확보
            RtsUnitAgent cPcAgent = m_cPlayer.GetComponent<RtsUnitAgent>();
            if (cPcAgent == null)
            {
                cPcAgent = m_cPlayer.gameObject.AddComponent<RtsUnitAgent>();
            }
            cPcAgent.IsAlly = true;
        }

        if (m_cNpc)
        {
            Player[] arrNpcComps = m_cNpc.GetComponents<Player>();
            for (int i = 0; i < arrNpcComps.Length; i++)
            {
                if (arrNpcComps[i] != m_cNpc)
                {
                    Destroy(arrNpcComps[i]);
                }
            }

            m_cNpc.IsPlayer = false;
            m_cNpc.EnsureComponents();
            m_cNpc.Initialize(null, null);

            // NPC에 AIController가 없으면 자동 추가
            if (!m_cNpc.GetComponent<AIController>())
            {
                m_cNpc.gameObject.AddComponent<AIController>();
            }

            // 기존 씬 NPC에도 RtsUnitAgent 확보 (기본 적대 유닛)
            RtsUnitAgent cNpcAgent = m_cNpc.GetComponent<RtsUnitAgent>();
            if (cNpcAgent == null)
            {
                cNpcAgent = m_cNpc.gameObject.AddComponent<RtsUnitAgent>();
                cNpcAgent.IsAlly = false;
            }
        }

        // 디버거가 씬에 없으면 GameManager에 자동 부착
        if (!FindFirstObjectByType<ControllerDebugger>())
        {
            gameObject.AddComponent<ControllerDebugger>();
        }

        // GUIManager 연결 및 초기화
        if (!m_cGuiManager)
        {
            m_cGuiManager = FindFirstObjectByType<GUIManager>();
            if (!m_cGuiManager)
            {
                m_cGuiManager = gameObject.AddComponent<GUIManager>();
            }
        }
        m_cGuiManager.Initialize();

        // PlayModeManager (FPS <-> RTS 전환 코어) 연결 및 초기화
        if (!m_cPlayModeManager)
        {
            m_cPlayModeManager = GetComponent<PlayModeManager>();
            if (!m_cPlayModeManager)
            {
                m_cPlayModeManager = FindFirstObjectByType<PlayModeManager>();
            }
            if (!m_cPlayModeManager)
            {
                m_cPlayModeManager = gameObject.AddComponent<PlayModeManager>();
            }
        }
        m_cPlayModeManager.Initialize(this);

        if (m_cPlayer)
        {
            m_cGuiManager.UpdateHpUi(m_cPlayer.CurrentHp, m_cPlayer.MaxHp);
        }
    }
}
