using UnityEngine;

/// <summary>
/// FPS ↔ RTS 하이브리드 전용 테스트 씬(RtsHybridTestScene) 부트스트랩 컴포넌트
/// 씬 시작 시 아군 분대 NPC 및 적군 NPC를 자동 배치하고 머리 위 식별 마커를 부착해
/// 다중 선택, 포메이션 이동, 집중 사격, 홀드/집결/추종, 모드 전환을 즉시 테스트할 수 있게 합니다.
/// </summary>
public class RtsHybridTestBootstrap : MonoBehaviour
{
    [Header("Test Scene Startup Settings")]
    [Tooltip("체크 시 타이틀 화면을 건너뛰고 바로 플레이 화면으로 진입")]
    [SerializeField]
    private bool m_bSkipTitleOnStart = true;

    [Tooltip("체크 시 시작하자마자 RTS 전술 모드로 진입 (해제 시 FPS 모드로 시작 후 Tab으로 전환)")]
    [SerializeField]
    private bool m_bStartInRtsMode = false;

    [Header("Test Unit Spawn Settings")]
    [SerializeField]
    private int m_nAllySquadCount = 3;

    [SerializeField]
    private int m_nExtraEnemyCount = 1;

    [SerializeField]
    private float m_fAllySpawnRadius = 3.5f;

    [SerializeField]
    private float m_fEnemySpawnSpacing = 4.0f;

    private bool m_bBootstrapped = false;

    private void Start()
    {
        if (m_bBootstrapped) return;
        m_bBootstrapped = true;

        GameManager cGameMgr = GameManager.s_cInstance;
        if (cGameMgr == null) return;

        SetupTestUnits(cGameMgr);

        // 테스트 씬 시작 화면 설정
        GUIManager cGui = GUIManager.s_cInstance;
        if (m_bSkipTitleOnStart && cGui != null)
        {
            if (m_bStartInRtsMode && PlayModeManager.s_cInstance != null)
            {
                PlayModeManager.s_cInstance.SwitchToRts();
            }
            else
            {
                cGui.SetGuiScene(cGui.InGameSceneName);
            }

            cGui.ShowNoticeToast("RTS HYBRID TEST READY - Press [Tab] to toggle FPS / RTS", 4.0f);
        }
    }

    private void SetupTestUnits(GameManager _cGameMgr)
    {
        Player cPc = _cGameMgr.CurrentPlayer;
        Player cTemplateNpc = _cGameMgr.CurrentNpc;

        // PC 머리 위 지휘관 마커 (하늘색)
        if (cPc != null)
        {
            AttachRoleMarker(cPc.transform, new Color(0.2f, 0.85f, 1f, 1f), "CommanderMarker");
        }

        if (cTemplateNpc == null) return;

        // 기존 NPC를 적군 1호로 마킹 (빨간색)
        ConfigureNpcUnit(cTemplateNpc, false, "Enemy_Unit_1", "Dummie", "Player", "Dummie");
        AttachRoleMarker(cTemplateNpc.transform, new Color(1f, 0.25f, 0.25f, 1f), "EnemyMarker");

        // 1. 아군 분대 NPC 스폰 (PC 주변에 배치)
        Vector3 vPcOrigin = cPc != null ? cPc.transform.position : Vector3.zero;
        for (int i = 0; i < m_nAllySquadCount; i++)
        {
            float fAngle = ((float)i / Mathf.Max(1, m_nAllySquadCount) * 180f - 90f) * Mathf.Deg2Rad;
            Vector3 vOffset = new Vector3(Mathf.Sin(fAngle), 0f, Mathf.Cos(fAngle)) * m_fAllySpawnRadius;
            Vector3 vSpawnPos = vPcOrigin + vOffset;

            Player cAlly = Instantiate(cTemplateNpc, vSpawnPos, Quaternion.identity);
            ConfigureNpcUnit(cAlly, true, $"Ally_Squad_{i + 1}", "Untagged", "Dummie", "Enemy");
            AttachRoleMarker(cAlly.transform, new Color(0.25f, 1f, 0.45f, 1f), "AllyMarker");
        }

        // 2. 추가 적군 NPC 스폰 (기존 적군 옆에 배치)
        Vector3 vEnemyOrigin = cTemplateNpc.transform.position;
        for (int i = 0; i < m_nExtraEnemyCount; i++)
        {
            Vector3 vSpawnPos = vEnemyOrigin + new Vector3((i + 1) * m_fEnemySpawnSpacing, 0f, 0f);
            Player cEnemy = Instantiate(cTemplateNpc, vSpawnPos, Quaternion.identity);
            ConfigureNpcUnit(cEnemy, false, $"Enemy_Unit_{i + 2}", "Dummie", "Player", "Untagged");
            AttachRoleMarker(cEnemy.transform, new Color(1f, 0.25f, 0.25f, 1f), "EnemyMarker");
        }
    }

    private void ConfigureNpcUnit(Player _cNpc, bool _bIsAlly, string _strName, string _strTag, string _strPrimaryTargetTag, string _strSecondaryTargetTag)
    {
        if (_cNpc == null) return;

        _cNpc.gameObject.name = _strName;
        if (!string.IsNullOrEmpty(_strTag))
        {
            try
            {
                _cNpc.gameObject.tag = _strTag;
            }
            catch
            {
                // 프로젝트에 미등록 태그인 경우 안전 무시
            }
        }

        _cNpc.IsPlayer = false;
        _cNpc.EnsureComponents();
        _cNpc.Initialize(null, null);

        AIController cAi = _cNpc.GetComponent<AIController>();
        if (cAi == null)
        {
            cAi = _cNpc.gameObject.AddComponent<AIController>();
        }
        cAi.m_strPrimaryTargetTag = _strPrimaryTargetTag;
        cAi.m_strSecondaryTargetTag = _strSecondaryTargetTag;

        RtsUnitAgent cAgent = _cNpc.GetComponent<RtsUnitAgent>();
        if (cAgent == null)
        {
            cAgent = _cNpc.gameObject.AddComponent<RtsUnitAgent>();
        }
        cAgent.IsAlly = _bIsAlly;
        cAgent.SetSelected(false);
    }

    private void AttachRoleMarker(Transform _trParent, Color _sColor, string _strMarkerName)
    {
        if (_trParent == null) return;
        if (_trParent.Find(_strMarkerName) != null) return;

        GameObject objMarker = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        objMarker.name = _strMarkerName;
        objMarker.transform.SetParent(_trParent, false);
        objMarker.transform.localPosition = new Vector3(0f, 2.35f, 0f);
        objMarker.transform.localScale = new Vector3(0.35f, 0.35f, 0.35f);

        Collider col = objMarker.GetComponent<Collider>();
        if (col != null)
        {
            Destroy(col);
        }

        MeshRenderer mr = objMarker.GetComponent<MeshRenderer>();
        if (mr != null)
        {
            Shader shaderUnlit = Shader.Find("Sprites/Default");
            if (shaderUnlit != null)
            {
                Material mat = new Material(shaderUnlit);
                mat.color = _sColor;
                mr.material = mat;
            }
            mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            mr.receiveShadows = false;
        }
    }
}
