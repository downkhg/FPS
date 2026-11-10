using UnityEngine;

public class ControllerDebugger : MonoBehaviour
{
    [Header("Toggle Settings")]
    [Tooltip("디버그 GUI 표시 여부 (F1 키로 토글 가능)")]
    public bool showDebugGUI = true;
    public KeyCode toggleKey = KeyCode.F1;

    [Header("Target References (비워두면 GameManager에서 자동 탐색)")]
    public PlayerController playerController;
    public AIController aiController;

    private GUIStyle boxStyle;
    private GUIStyle headerStyle;
    private GUIStyle labelStyle;
    private GUIStyle btnStyle;
    private bool stylesInitialized = false;

    void Start()
    {
        FindControllers();
    }

    void Update()
    {
        // F1 키로 디버그 창 토글
        if (Input.GetKeyDown(toggleKey))
        {
            showDebugGUI = !showDebugGUI;
        }

        // 레퍼런스가 누락되었으면 주기적으로 재탐색
        if (playerController == null || aiController == null)
        {
            FindControllers();
        }
    }

    void FindControllers()
    {
        if (playerController == null)
            playerController = FindFirstObjectByType<PlayerController>();

        if (aiController == null)
            aiController = FindFirstObjectByType<AIController>();
    }

    void InitStyles()
    {
        if (stylesInitialized) return;

        // 반투명 검정 배경 박스
        boxStyle = new GUIStyle(GUI.skin.box);
        Texture2D darkBg = new Texture2D(1, 1);
        darkBg.SetPixel(0, 0, new Color(0.08f, 0.08f, 0.1f, 0.85f));
        darkBg.Apply();
        boxStyle.normal.background = darkBg;

        headerStyle = new GUIStyle(GUI.skin.label);
        headerStyle.fontSize = 14;
        headerStyle.fontStyle = FontStyle.Bold;
        headerStyle.normal.textColor = Color.yellow;

        labelStyle = new GUIStyle(GUI.skin.label);
        labelStyle.fontSize = 12;
        labelStyle.normal.textColor = Color.white;

        btnStyle = new GUIStyle(GUI.skin.button);
        btnStyle.fontSize = 11;
        btnStyle.normal.textColor = Color.cyan;

        stylesInitialized = true;
    }

    void OnGUI()
    {
        if (!showDebugGUI) return;

        InitStyles();

        // 상단 안내 바
        GUI.color = Color.white;
        GUI.Label(new Rect(10, 5, 400, 20), $"[F1] 디버그 GUI On/Off (현재: {(showDebugGUI ? "ON" : "OFF")})", labelStyle);

        // 1. 플레이어 컨트롤러 디버그 패널 (화면 좌측)
        DrawPlayerDebugPanel(new Rect(10, 30, 280, 320));

        // 2. AI 컨트롤러 디버그 패널 (화면 우측)
        DrawAIDebugPanel(new Rect(Screen.width - 290, 30, 280, 420));
    }

    void DrawPlayerDebugPanel(Rect rect)
    {
        GUI.Box(rect, GUIContent.none, boxStyle);

        GUILayout.BeginArea(new Rect(rect.x + 10, rect.y + 10, rect.width - 20, rect.height - 20));
        GUILayout.Label("🎮 PLAYER CONTROLLER", headerStyle);
        GUILayout.Space(5);

        if (playerController != null)
        {
            Player p = playerController.GetComponent<Player>();
            PlayerMovementScript pms = p != null ? p.PmS : null;
            GunScript gun = p != null ? p.Gun : null;

            // 상태
            Color stateColor = GetPlayerStateColor(playerController.CurrentState);
            GUI.color = stateColor;
            GUILayout.Label($"State: [{playerController.CurrentState}]", headerStyle);
            GUI.color = Color.white;

            // 물리 및 이동 정보
            if (pms != null)
            {
                GUILayout.Label($"Speed: {pms.currentSpeed:F2} / Max: {pms.maxSpeed}", labelStyle);
                GUILayout.Label($"Grounded: {pms.grounded}", labelStyle);
            }

            // 무기 및 탄약 정보 (Player 마리오네트에서 직접 조회)
            if (p != null)
            {
                string gunName = (gun != null) ? gun.gameObject.name : "None";
                GUILayout.Label($"Weapon [{p.CurrentWeaponIndex}/{p.Weapons.Count}]: {gunName}", labelStyle);
            }

            if (gun != null)
            {
                GUILayout.Label($"Ammo: {gun.bulletsInTheGun} / {gun.bulletsIHave}", labelStyle);
                GUILayout.Label($"Reloading: {gun.reloading} | Style: {gun.currentStyle}", labelStyle);
            }

            GUILayout.Space(10);
            GUILayout.Label("── 강제 상태 전환 테스트 ──", labelStyle);
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Idle", btnStyle)) playerController.SetState(PlayerController.PlayerState.Idle);
            if (GUILayout.Button("Move", btnStyle)) playerController.SetState(PlayerController.PlayerState.Move);
            if (GUILayout.Button("Attack", btnStyle)) playerController.SetState(PlayerController.PlayerState.Attack);
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Reload", btnStyle)) playerController.SetState(PlayerController.PlayerState.Reload);
            if (GUILayout.Button("Melee", btnStyle)) playerController.SetState(PlayerController.PlayerState.Melee);
            if (GUILayout.Button("NextGun", btnStyle)) p?.NextWeapon();
            GUILayout.EndHorizontal();
        }
        else
        {
            GUILayout.Label("PlayerController를 찾을 수 없습니다.", labelStyle);
        }

        GUILayout.EndArea();
    }

    void DrawAIDebugPanel(Rect rect)
    {
        GUI.Box(rect, GUIContent.none, boxStyle);

        GUILayout.BeginArea(new Rect(rect.x + 10, rect.y + 10, rect.width - 20, rect.height - 20));
        GUILayout.Label("🤖 AI NPC CONTROLLER", headerStyle);
        GUILayout.Space(5);

        if (aiController != null)
        {
            Player npc = aiController.GetComponent<Player>();
            GunScript gun = npc != null ? npc.Gun : null;

            // 상태
            Color stateColor = GetAIStateColor(aiController.CurrentState);
            GUI.color = stateColor;
            GUILayout.Label($"State: [{aiController.CurrentState}]", headerStyle);
            GUI.color = Color.white;

            // 타깃 정보
            if (aiController.currentTarget != null)
            {
                float dist = Vector3.Distance(aiController.transform.position, aiController.currentTarget.position);
                GUILayout.Label($"Target: {aiController.currentTarget.name}", labelStyle);
                GUILayout.Label($"Distance: {dist:F2}m", labelStyle);

                // 현재 타깃에 대한 사거리 판정 표시
                if (dist <= aiController.meleeAttackRange)
                    GUILayout.Label("▶ 범위: [초근접 (Melee)]", labelStyle);
                else if (dist >= aiController.sniperRangeMin)
                    GUILayout.Label("▶ 범위: [저격 사거리 (Snipe)]", labelStyle);
                else if (dist <= aiController.normalRangeMax)
                    GUILayout.Label("▶ 범위: [일반 사거리 (Normal)]", labelStyle);
                else
                    GUILayout.Label("▶ 범위: [사거리 밖 (Chase)]", labelStyle);
            }
            else
            {
                GUILayout.Label("Target: None (적 탐색 중...)", labelStyle);
            }

            // 무기 정보 (Player 마리오네트에서 직접 조회)
            if (npc != null)
            {
                string gunName = (gun != null) ? gun.gameObject.name : "None";
                GUILayout.Label($"Current Gun [{npc.CurrentWeaponIndex}/{npc.Weapons.Count}]: {gunName}", labelStyle);
            }

            if (gun != null)
            {
                GUILayout.Label($"Ammo: {gun.bulletsInTheGun} / {gun.bulletsIHave}", labelStyle);
            }

            GUILayout.Space(10);
            GUILayout.Label("── AI 강제 상태 전환 테스트 ──", labelStyle);
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Idle", btnStyle)) aiController.SetState(AIController.AIState.Idle);
            if (GUILayout.Button("Chase", btnStyle)) aiController.SetState(AIController.AIState.Chase);
            if (GUILayout.Button("Snipe", btnStyle)) aiController.SetState(AIController.AIState.Snipe);
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Normal", btnStyle)) aiController.SetState(AIController.AIState.NormalAttack);
            if (GUILayout.Button("Melee", btnStyle)) aiController.SetState(AIController.AIState.Melee);
            GUILayout.EndHorizontal();

            GUILayout.Space(5);
            GUILayout.Label("── 무기 교체 테스트 ──", labelStyle);
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("0: 일반총", btnStyle)) npc?.SwitchWeapon(0);
            if (GUILayout.Button("1: 저격총", btnStyle)) npc?.SwitchWeapon(1);
            GUILayout.EndHorizontal();
        }
        else
        {
            GUILayout.Label("AIController를 찾을 수 없습니다.", labelStyle);
        }

        GUILayout.EndArea();
    }

    Color GetPlayerStateColor(PlayerController.PlayerState state)
    {
        switch (state)
        {
            case PlayerController.PlayerState.Idle: return Color.gray;
            case PlayerController.PlayerState.Move: return Color.green;
            case PlayerController.PlayerState.Attack: return Color.red;
            case PlayerController.PlayerState.Reload: return Color.yellow;
            case PlayerController.PlayerState.Melee: return new Color(1f, 0.5f, 0f);
            default: return Color.white;
        }
    }

    Color GetAIStateColor(AIController.AIState state)
    {
        switch (state)
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
