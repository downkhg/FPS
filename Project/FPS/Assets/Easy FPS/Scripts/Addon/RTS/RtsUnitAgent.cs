using UnityEngine;

/// <summary>
/// RTS 모드에서 유닛(PC 지휘관 및 NPC)이 수신하는 명령 종류
/// </summary>
public enum ERtsCommandType
{
    None,
    Move,
    AttackTarget,
    Hold,
    Follow
}

/// <summary>
/// PC 및 NPC에 부착되어 RTS 모드에서의 선택 링(Selection Ring) 표시와
/// 이동/집중 사격/홀드/PC 추종 명령 실행을 담당하는 컴포넌트
/// </summary>
[RequireComponent(typeof(Player))]
public class RtsUnitAgent : MonoBehaviour
{
    [Header("Unit Identity")]
    [Tooltip("아군 여부 (true면 플레이어가 RTS 모드에서 선택 및 명령 가능)")]
    [SerializeField]
    private bool m_bIsAlly = true;

    [Header("Command Execution Settings")]
    [SerializeField]
    private float m_fStopDistance = 0.8f;

    [SerializeField]
    private float m_fFollowDistance = 3.0f;

    [SerializeField]
    private float m_fAttackRange = 16f;

    [SerializeField]
    private float m_fFireInterval = 0.35f;

    [Header("Selection Ring Settings")]
    [SerializeField]
    private float m_fRingRadius = 0.85f;

    [SerializeField]
    private float m_fRingHeightOffset = 0.08f;

    private Player m_cPlayer;
    private LineRenderer m_lrSelectionRing;
    private Material m_matSelectionRing;

    private bool m_bIsSelected = false;
    private ERtsCommandType m_eCurrentCommand = ERtsCommandType.None;
    private Vector3 m_vMoveDestination;
    private Transform m_trAttackTarget;
    private Transform m_trFollowLeader;
    private float m_fFireTimer = 0f;

    public Player UnitPlayer => m_cPlayer;
    public bool IsAlly
    {
        get => m_bIsAlly;
        set => m_bIsAlly = value;
    }

    public bool IsSelected => m_bIsSelected;
    public ERtsCommandType CurrentCommand => m_eCurrentCommand;
    public bool HasActiveCommand => m_eCurrentCommand != ERtsCommandType.None;

    private void Awake()
    {
        m_cPlayer = GetComponent<Player>();
        EnsureSelectionRing();
        SetSelected(false);

        AIController cAi = GetComponent<AIController>();
        if (cAi != null)
        {
            cAi.SetRtsUnitAgent(this);
        }
    }

    private void OnDestroy()
    {
        if (m_matSelectionRing != null)
        {
            Destroy(m_matSelectionRing);
            m_matSelectionRing = null;
        }
    }

    private void EnsureSelectionRing()
    {
        if (m_lrSelectionRing != null) return;

        Transform trExisting = transform.Find("RTS_SelectionRing");
        if (trExisting != null)
        {
            m_lrSelectionRing = trExisting.GetComponent<LineRenderer>();
        }

        if (m_lrSelectionRing == null)
        {
            GameObject objRing = new GameObject("RTS_SelectionRing");
            objRing.transform.SetParent(transform, false);
            objRing.transform.localPosition = Vector3.zero;

            m_lrSelectionRing = objRing.AddComponent<LineRenderer>();
            m_lrSelectionRing.useWorldSpace = false;
            m_lrSelectionRing.loop = true;
            m_lrSelectionRing.startWidth = 0.08f;
            m_lrSelectionRing.endWidth = 0.08f;
            m_lrSelectionRing.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            m_lrSelectionRing.receiveShadows = false;

            Shader shaderSprite = Shader.Find("Sprites/Default");
            if (shaderSprite != null)
            {
                m_matSelectionRing = new Material(shaderSprite);
                m_lrSelectionRing.material = m_matSelectionRing;
            }

            Color sRingColor = (m_cPlayer != null && m_cPlayer.IsPlayer)
                ? new Color(0.25f, 0.85f, 1f, 0.95f)
                : new Color(0.25f, 1f, 0.45f, 0.95f);

            m_lrSelectionRing.startColor = sRingColor;
            m_lrSelectionRing.endColor = sRingColor;

            const int nSegments = 32;
            m_lrSelectionRing.positionCount = nSegments;
            for (int i = 0; i < nSegments; i++)
            {
                float fAngle = (float)i / nSegments * Mathf.PI * 2f;
                float fX = Mathf.Cos(fAngle) * m_fRingRadius;
                float fZ = Mathf.Sin(fAngle) * m_fRingRadius;
                m_lrSelectionRing.SetPosition(i, new Vector3(fX, m_fRingHeightOffset, fZ));
            }
        }
    }

    public void SetSelected(bool _bSelected)
    {
        m_bIsSelected = _bSelected && m_bIsAlly;
        if (m_lrSelectionRing != null)
        {
            m_lrSelectionRing.enabled = m_bIsSelected;
        }
    }

    // ==================== RTS 명령 수신 API ====================

    public void IssueMoveCommand(Vector3 _vDestination)
    {
        m_eCurrentCommand = ERtsCommandType.Move;
        m_vMoveDestination = _vDestination;
        m_trAttackTarget = null;
        m_trFollowLeader = null;
    }

    public void IssueAttackTargetCommand(Transform _trTarget)
    {
        if (_trTarget == null || _trTarget == transform) return;
        m_eCurrentCommand = ERtsCommandType.AttackTarget;
        m_trAttackTarget = _trTarget;
        m_trFollowLeader = null;
    }

    public void IssueHoldCommand()
    {
        m_eCurrentCommand = ERtsCommandType.Hold;
        m_trAttackTarget = null;
        m_trFollowLeader = null;
        if (m_cPlayer != null)
        {
            m_cPlayer.MoveWorld(Vector3.zero);
        }
    }

    public void IssueFollowCommand(Transform _trLeader)
    {
        if (_trLeader == null || _trLeader == transform) return;
        m_eCurrentCommand = ERtsCommandType.Follow;
        m_trFollowLeader = _trLeader;
        m_trAttackTarget = null;
    }

    public void ClearCommand()
    {
        m_eCurrentCommand = ERtsCommandType.None;
        m_trAttackTarget = null;
        m_trFollowLeader = null;
        if (m_cPlayer != null)
        {
            m_cPlayer.MoveWorld(Vector3.zero);
        }
    }

    // ==================== 명령 실행 루프 ====================

    private void Update()
    {
        if (m_cPlayer == null || m_cPlayer.IsDead)
        {
            if (m_bIsSelected) SetSelected(false);
            return;
        }

        if (!GUIManager.CanRunSimulation()) return;

        // PC는 FPS 직접 조작 중에는 PlayerController가 제어하므로 RTS 명령 실행 스킵
        if (m_cPlayer.IsPlayer && GUIManager.CanProcessGameplayInput())
        {
            return;
        }

        if (m_fFireTimer > 0f)
        {
            m_fFireTimer -= Time.deltaTime;
        }

        switch (m_eCurrentCommand)
        {
            case ERtsCommandType.Move:
                UpdateMoveCommand();
                break;

            case ERtsCommandType.AttackTarget:
                UpdateAttackTargetCommand();
                break;

            case ERtsCommandType.Hold:
                m_cPlayer.MoveWorld(Vector3.zero);
                break;

            case ERtsCommandType.Follow:
                UpdateFollowCommand();
                break;
        }
    }

    private void UpdateMoveCommand()
    {
        Vector3 vDiff = m_vMoveDestination - transform.position;
        vDiff.y = 0f;
        float fDist = vDiff.magnitude;

        if (fDist <= m_fStopDistance)
        {
            m_cPlayer.MoveWorld(Vector3.zero);
            m_eCurrentCommand = ERtsCommandType.None;
            return;
        }

        Vector3 vDir = vDiff / fDist;
        m_cPlayer.LookAtTarget(m_vMoveDestination + Vector3.up * 1.0f);
        m_cPlayer.MoveWorld(vDir);
    }

    private void UpdateAttackTargetCommand()
    {
        if (m_trAttackTarget == null || !m_trAttackTarget.gameObject.activeInHierarchy)
        {
            ClearCommand();
            return;
        }

        Player cTargetPlayer = m_trAttackTarget.GetComponent<Player>();
        if (cTargetPlayer != null && cTargetPlayer.IsDead)
        {
            ClearCommand();
            return;
        }

        Vector3 vDiff = m_trAttackTarget.position - transform.position;
        vDiff.y = 0f;
        float fDist = vDiff.magnitude;

        m_cPlayer.LookAtTarget(m_trAttackTarget.position + Vector3.up * 1.0f);

        if (fDist > m_fAttackRange)
        {
            m_cPlayer.MoveWorld(vDiff.normalized);
        }
        else
        {
            m_cPlayer.MoveWorld(Vector3.zero);
            if (m_fFireTimer <= 0f)
            {
                m_cPlayer.Shoot();
                m_fFireTimer = m_fFireInterval;
            }
        }
    }

    private void UpdateFollowCommand()
    {
        if (m_trFollowLeader == null || !m_trFollowLeader.gameObject.activeInHierarchy)
        {
            ClearCommand();
            return;
        }

        Vector3 vDiff = m_trFollowLeader.position - transform.position;
        vDiff.y = 0f;
        float fDist = vDiff.magnitude;

        if (fDist > m_fFollowDistance)
        {
            m_cPlayer.LookAtTarget(m_trFollowLeader.position + Vector3.up * 1.0f);
            m_cPlayer.MoveWorld(vDiff.normalized);
        }
        else
        {
            m_cPlayer.MoveWorld(Vector3.zero);
        }
    }
}
