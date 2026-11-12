using UnityEngine;
using UnityEngine.Serialization;

public class ControllerDebugger : MonoBehaviour
{
    [Header("Toggle Settings")]
    [Tooltip("디버그 GUI 표시 여부 (F1 키로 토글 가능)")]
    [FormerlySerializedAs("showDebugGUI")]
    public bool m_bShowDebugGUI = true;

    [FormerlySerializedAs("toggleKey")]
    public KeyCode m_eToggleKey = KeyCode.F1;

    [Header("Target References (비워두면 GameManager에서 자동 탐색)")]
    [FormerlySerializedAs("playerController")]
    public PlayerController m_cPlayerController;

    [FormerlySerializedAs("aiController")]
    public AIController m_cAiController;

    private GUIStyle m_cBoxStyle;
    private GUIStyle m_cHeaderStyle;
    private GUIStyle m_cLabelStyle;
    private GUIStyle m_cBtnStyle;
    private bool m_bStylesInitialized = false;

    void Start()
    {
        FindControllers();
    }

    void Update()
    {
        // F1 키로 디버그 창 토글
        if (Input.GetKeyDown(m_eToggleKey))
        {
            m_bShowDebugGUI = !m_bShowDebugGUI;
        }

        // 레퍼런스가 누락되었으면 재탐색
        if (m_cPlayerController == null || m_cAiController == null)
        {
            FindControllers();
        }
    }

    private void FindControllers()
    {
        if (m_cPlayerController == null)
            m_cPlayerController = FindFirstObjectByType<PlayerController>();

        if (m_cAiController == null)
            m_cAiController = FindFirstObjectByType<AIController>();
    }

    private void InitStyles()
    {
        if (m_bStylesInitialized) return;

        // 반투명 검정 배경 박스
        m_cBoxStyle = new GUIStyle(GUI.skin.box);
        Texture2D texDarkBg = new Texture2D(1, 1);
        texDarkBg.SetPixel(0, 0, new Color(0.08f, 0.08f, 0.1f, 0.85f));
        texDarkBg.Apply();
        m_cBoxStyle.normal.background = texDarkBg;

        m_cHeaderStyle = new GUIStyle(GUI.skin.label);
        m_cHeaderStyle.fontSize = 14;
        m_cHeaderStyle.fontStyle = FontStyle.Bold;
        m_cHeaderStyle.normal.textColor = Color.yellow;

        m_cLabelStyle = new GUIStyle(GUI.skin.label);
        m_cLabelStyle.fontSize = 12;
        m_cLabelStyle.normal.textColor = Color.white;

        m_cBtnStyle = new GUIStyle(GUI.skin.button);
        m_cBtnStyle.fontSize = 11;
        m_cBtnStyle.normal.textColor = Color.cyan;

        m_bStylesInitialized = true;
    }

    void OnGUI()
    {
        if (!m_bShowDebugGUI) return;

        InitStyles();

        // 상단 안내 바
        GUI.color = Color.white;
        GUI.Label(new Rect(10, 5, 400, 20), $"[F1] 디버그 GUI On/Off (현재: {(m_bShowDebugGUI ? "ON" : "OFF")})", m_cLabelStyle);

        // 1. 플레이어 컨트롤러 디버그 패널 (화면 좌측)
        DrawPlayerDebugPanel(new Rect(10, 30, 280, 320));

        // 2. AI 컨트롤러 디버그 패널 (화면 우측)
        DrawAIDebugPanel(new Rect(Screen.width - 290, 30, 280, 420));
    }

    private void DrawPlayerDebugPanel(Rect _sRect)
    {
        GUI.Box(_sRect, GUIContent.none, m_cBoxStyle);

        GUILayout.BeginArea(new Rect(_sRect.x + 10, _sRect.y + 10, _sRect.width - 20, _sRect.height - 20));
        GUILayout.Label("PLAYER CONTROLLER", m_cHeaderStyle);
        GUILayout.Space(5);

        if (m_cPlayerController != null)
        {
            Player cPlayer = m_cPlayerController.GetComponent<Player>();
            PlayerMovementScript cPms = cPlayer != null ? cPlayer.PmS : null;
            GunScript cGun = cPlayer != null ? cPlayer.Gun : null;

            // 상태
            Color sStateColor = GetPlayerStateColor(m_cPlayerController.CurrentState);
            GUI.color = sStateColor;
            GUILayout.Label($"State: [{m_cPlayerController.CurrentState}]", m_cHeaderStyle);
            GUI.color = Color.white;

            // 물리 및 이동 정보
            if (cPms != null)
            {
                GUILayout.Label($"Speed: {cPms.CurrentSpeed:F2} / Max: {cPms.MaxSpeed}", m_cLabelStyle);
                GUILayout.Label($"Grounded: {cPms.Grounded}", m_cLabelStyle);
            }

            // 무기 및 탄약 정보 (Player 마리오네트에서 직접 조회)
            if (cPlayer != null)
            {
                string strGunName = (cGun != null) ? cGun.gameObject.name : "None";
                GUILayout.Label($"Weapon [{cPlayer.CurrentWeaponIndex}/{cPlayer.Weapons.Count}]: {strGunName}", m_cLabelStyle);
            }

            if (cGun != null)
            {
                GUILayout.Label($"Ammo: {cGun.BulletsInTheGun} / {cGun.BulletsIHave}", m_cLabelStyle);
                GUILayout.Label($"Reloading: {cGun.IsReloading} | Style: {cGun.CurrentStyle}", m_cLabelStyle);
            }

            GUILayout.Space(10);
            GUILayout.Label("── 강제 상태 전환 테스트 ──", m_cLabelStyle);
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Idle", m_cBtnStyle)) m_cPlayerController.SetState(PlayerController.PlayerState.Idle);
            if (GUILayout.Button("Move", m_cBtnStyle)) m_cPlayerController.SetState(PlayerController.PlayerState.Move);
            if (GUILayout.Button("Attack", m_cBtnStyle)) m_cPlayerController.SetState(PlayerController.PlayerState.Attack);
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Reload", m_cBtnStyle)) m_cPlayerController.SetState(PlayerController.PlayerState.Reload);
            if (GUILayout.Button("Melee", m_cBtnStyle)) m_cPlayerController.SetState(PlayerController.PlayerState.Melee);
            if (GUILayout.Button("NextGun", m_cBtnStyle)) cPlayer?.NextWeapon();
            GUILayout.EndHorizontal();
        }
        else
        {
            GUILayout.Label("PlayerController를 찾을 수 없습니다.", m_cLabelStyle);
        }

        GUILayout.EndArea();
    }

    private void DrawAIDebugPanel(Rect _sRect)
    {
        GUI.Box(_sRect, GUIContent.none, m_cBoxStyle);

        GUILayout.BeginArea(new Rect(_sRect.x + 10, _sRect.y + 10, _sRect.width - 20, _sRect.height - 20));
        GUILayout.Label("AI NPC CONTROLLER", m_cHeaderStyle);
        GUILayout.Space(5);

        if (m_cAiController != null)
        {
            Player cNpc = m_cAiController.GetComponent<Player>();
            GunScript cGun = cNpc != null ? cNpc.Gun : null;

            // 상태
            Color sStateColor = GetAIStateColor(m_cAiController.CurrentState);
            GUI.color = sStateColor;
            GUILayout.Label($"State: [{m_cAiController.CurrentState}]", m_cHeaderStyle);
            GUI.color = Color.white;

            // 타깃 정보
            if (m_cAiController.m_trCurrentTarget != null)
            {
                float fDist = Vector3.Distance(m_cAiController.transform.position, m_cAiController.m_trCurrentTarget.position);
                GUILayout.Label($"Target: {m_cAiController.m_trCurrentTarget.name}", m_cLabelStyle);
                GUILayout.Label($"Distance: {fDist:F2}m", m_cLabelStyle);

                // 현재 타깃에 대한 사거리 판정 표시
                if (fDist <= m_cAiController.m_fMeleeAttackRange)
                    GUILayout.Label("▶ 범위: [초근접 (Melee)]", m_cLabelStyle);
                else if (fDist >= m_cAiController.m_fSniperRangeMin)
                    GUILayout.Label("▶ 범위: [저격 사거리 (Snipe)]", m_cLabelStyle);
                else if (fDist <= m_cAiController.m_fNormalRangeMax)
                    GUILayout.Label("▶ 범위: [일반 사거리 (Normal)]", m_cLabelStyle);
                else
                    GUILayout.Label("▶ 범위: [사거리 밖 (Chase)]", m_cLabelStyle);
            }
            else
            {
                GUILayout.Label("Target: None (적 탐색 중...)", m_cLabelStyle);
            }

            // 무기 정보 (Player 마리오네트에서 직접 조회)
            if (cNpc != null)
            {
                string strGunName = (cGun != null) ? cGun.gameObject.name : "None";
                GUILayout.Label($"Current Gun [{cNpc.CurrentWeaponIndex}/{cNpc.Weapons.Count}]: {strGunName}", m_cLabelStyle);
            }

            if (cGun != null)
            {
                GUILayout.Label($"Ammo: {cGun.BulletsInTheGun} / {cGun.BulletsIHave}", m_cLabelStyle);
            }

            GUILayout.Space(10);
            GUILayout.Label("── AI 강제 상태 전환 테스트 ──", m_cLabelStyle);
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Idle", m_cBtnStyle)) m_cAiController.SetState(AIController.AIState.Idle);
            if (GUILayout.Button("Chase", m_cBtnStyle)) m_cAiController.SetState(AIController.AIState.Chase);
            if (GUILayout.Button("Snipe", m_cBtnStyle)) m_cAiController.SetState(AIController.AIState.Snipe);
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Normal", m_cBtnStyle)) m_cAiController.SetState(AIController.AIState.NormalAttack);
            if (GUILayout.Button("Melee", m_cBtnStyle)) m_cAiController.SetState(AIController.AIState.Melee);
            GUILayout.EndHorizontal();

            GUILayout.Space(5);
            GUILayout.Label("── 무기 교체 테스트 ──", m_cLabelStyle);
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("0: 단발/저격총", m_cBtnStyle)) cNpc?.SwitchWeapon(0);
            if (GUILayout.Button("1: 연사/일반총", m_cBtnStyle)) cNpc?.SwitchWeapon(1);
            GUILayout.EndHorizontal();
        }
        else
        {
            GUILayout.Label("AIController를 찾을 수 없습니다.", m_cLabelStyle);
        }

        GUILayout.EndArea();
    }

    private Color GetPlayerStateColor(PlayerController.PlayerState _eState)
    {
        switch (_eState)
        {
            case PlayerController.PlayerState.Idle: return Color.gray;
            case PlayerController.PlayerState.Move: return Color.green;
            case PlayerController.PlayerState.Attack: return Color.red;
            case PlayerController.PlayerState.Reload: return Color.yellow;
            case PlayerController.PlayerState.Melee: return new Color(1f, 0.5f, 0f);
            default: return Color.white;
        }
    }

    private Color GetAIStateColor(AIController.AIState _eState)
    {
        switch (_eState)
        {
            case AIController.AIState.Idle: return Color.gray;
            case AIController.AIState.Chase: return Color.yellow;
            case AIController.AIState.Snipe: return Color.cyan;
            case AIController.AIState.NormalAttack: return Color.red;
            case AIController.AIState.Melee: return new Color(1f, 0.5f, 0f);
            default: return Color.white;
        }
    }
}
