using UnityEngine;
using UnityEngine.Serialization;

[RequireComponent(typeof(Player))]
public class AIController : MonoBehaviour
{
    public enum AIState
    {
        Idle,           // 적 탐색 및 대기
        Chase,          // 사거리 밖: 적을 향해 이동
        Snipe,          // 저격거리: 저격총 장착 후 저격 사격
        NormalAttack,   // 일반사거리: 일반총 장착 후 사격
        Melee           // 초근접: 근접 공격
    }

    [Header("FSM State")]
    [SerializeField, FormerlySerializedAs("currentState")]
    private AIState m_eCurrentState = AIState.Idle;

    public AIState CurrentState => m_eCurrentState;

    private Player m_cNpc;

    [Header("AI Detection Settings")]
    [Tooltip("적 탐지 반경")]
    [FormerlySerializedAs("detectionRadius")]
    public float m_fDetectionRadius = 40f;

    [Tooltip("우선 탐색할 타깃 태그 (기본: Player)")]
    [FormerlySerializedAs("primaryTargetTag")]
    public string m_strPrimaryTargetTag = "Player";

    [Tooltip("보조 탐색할 타깃 태그 (예: Dummie)")]
    [FormerlySerializedAs("secondaryTargetTag")]
    public string m_strSecondaryTargetTag = "Dummie";

    [Header("Combat Ranges")]
    [Tooltip("저격 사거리 (이 거리 이상이면 저격총으로 교환 후 사격)")]
    [FormerlySerializedAs("sniperRangeMin")]
    public float m_fSniperRangeMin = 15f;

    [Tooltip("일반 사거리 최대값 (이 거리 이내이면 일반총 사격, 벗어나면 이동)")]
    [FormerlySerializedAs("normalRangeMax")]
    public float m_fNormalRangeMax = 15f;

    [Tooltip("근접 공격 사거리")]
    [FormerlySerializedAs("meleeAttackRange")]
    public float m_fMeleeAttackRange = 2.5f;

    [Header("Attack Fire Rates (Seconds)")]
    [Tooltip("저격총 사격 간격 (초)")]
    [FormerlySerializedAs("sniperFireInterval")]
    public float m_fSniperFireInterval = 1.5f;

    [Tooltip("일반총 사격 간격 (초)")]
    [FormerlySerializedAs("normalFireInterval")]
    public float m_fNormalFireInterval = 0.25f;

    [Tooltip("근접 공격 간격 (초)")]
    [FormerlySerializedAs("meleeInterval")]
    public float m_fMeleeInterval = 0.8f;

    [Header("Target Tracking")]
    [FormerlySerializedAs("currentTarget")]
    public Transform m_trCurrentTarget;

    private float m_fTargetSearchTimer = 0f;
    private float m_fAttackTimer = 0f;

    [Header("Gizmo Debug Settings")]
    [Tooltip("Scene 뷰에 AI 기즈모(사거리 및 조준선) 표시 여부")]
    [FormerlySerializedAs("showGizmos")]
    public bool m_bShowGizmos = true;

    [FormerlySerializedAs("showOnlyWhenSelected")]
    public bool m_bShowOnlyWhenSelected = false;

    void Awake()
    {
        m_cNpc = GetComponent<Player>();
    }

    void Start()
    {
        SetState(AIState.Idle);
    }

    void Update()
    {
        if (!m_cNpc) return;

        // 공격 쿨다운 타이머 감소
        if (m_fAttackTimer > 0f)
            m_fAttackTimer -= Time.deltaTime;

        // 주기적인 적 타깃 탐색 (0.5초마다)
        m_fTargetSearchTimer -= Time.deltaTime;
        if (m_fTargetSearchTimer <= 0f || m_trCurrentTarget == null)
        {
            m_fTargetSearchTimer = 0.5f;
            m_trCurrentTarget = FindTarget();
        }

        // 상태 머신 업데이트
        UpdateState();
    }

    // ==================== 유한상태머신 (FSM) 함수 ====================

    public void SetState(AIState _eNewState)
    {
        if (m_eCurrentState == _eNewState) return;

        // 이전 상태 Exit
        switch (m_eCurrentState)
        {
            case AIState.Chase:
                m_cNpc.MoveWorld(Vector3.zero);
                break;

            case AIState.Snipe:
                m_cNpc.Aim(false);
                break;
        }

        m_eCurrentState = _eNewState;
        m_fAttackTimer = 0f; // 상태 전이 시 즉시 1발 발사 가능하도록 쿨다운 리셋

        // 새 상태 Enter
        switch (m_eCurrentState)
        {
            case AIState.Idle:
                m_cNpc.MoveWorld(Vector3.zero);
                m_cNpc.Aim(false);
                break;

            case AIState.Chase:
                m_cNpc.Aim(false);
                break;

            case AIState.Snipe:
                m_cNpc.MoveWorld(Vector3.zero);
                m_cNpc.SwitchWeapon(0); // 단발/저격총(NewGun_semi) 장착
                m_cNpc.Aim(true);       // 조준 활성화
                break;

            case AIState.NormalAttack:
                m_cNpc.MoveWorld(Vector3.zero);
                m_cNpc.SwitchWeapon(1); // 연사/일반총(NewGun_auto) 장착
                m_cNpc.Aim(false);
                break;

            case AIState.Melee:
                m_cNpc.MoveWorld(Vector3.zero);
                m_cNpc.Aim(false);
                m_cNpc.Melee();
                m_fAttackTimer = m_fMeleeInterval;
                break;
        }
    }

    public void UpdateState()
    {
        // 타깃이 없으면 Idle
        if (m_trCurrentTarget == null)
        {
            if (m_eCurrentState != AIState.Idle)
                SetState(AIState.Idle);
            UpdateIdle();
            return;
        }

        // 타깃의 가슴/상체 위치(y + 1.0f)를 지속적으로 조준
        Vector3 vAimPoint = m_trCurrentTarget.position + Vector3.up * 1.0f;
        m_cNpc.LookAtTarget(vAimPoint);

        switch (m_eCurrentState)
        {
            case AIState.Idle:
                UpdateIdle();
                break;

            case AIState.Chase:
                UpdateChase();
                break;

            case AIState.Snipe:
                UpdateSnipe();
                break;

            case AIState.NormalAttack:
                UpdateNormalAttack();
                break;

            case AIState.Melee:
                UpdateMelee();
                break;
        }
    }

    // ==================== 개별 상태 로직 ====================

    private void UpdateIdle()
    {
        if (m_trCurrentTarget != null)
        {
            DecideCombatState();
        }
    }

    private void UpdateChase()
    {
        float fDistance = Vector3.Distance(transform.position, m_trCurrentTarget.position);

        if (fDistance <= m_fMeleeAttackRange)
        {
            SetState(AIState.Melee);
            return;
        }
        else if (fDistance >= m_fSniperRangeMin)
        {
            SetState(AIState.Snipe);
            return;
        }
        else if (fDistance <= m_fNormalRangeMax)
        {
            SetState(AIState.NormalAttack);
            return;
        }

        // [범위에 들어오지 않았으면 > 이동]
        Vector3 vToTargetDir = (m_trCurrentTarget.position - transform.position).normalized;
        m_cNpc.MoveWorld(vToTargetDir);
    }

    private void UpdateSnipe()
    {
        float fDistance = Vector3.Distance(transform.position, m_trCurrentTarget.position);

        if (fDistance < m_fSniperRangeMin)
        {
            DecideCombatState();
            return;
        }

        // [적이 저격거리에 있으면 > 저격총으로 쏜다]
        m_cNpc.Aim(true);

        if (m_fAttackTimer <= 0f)
        {
            m_fAttackTimer = m_fSniperFireInterval;
            m_cNpc.Shoot();
        }
    }

    private void UpdateNormalAttack()
    {
        float fDistance = Vector3.Distance(transform.position, m_trCurrentTarget.position);

        if (fDistance <= m_fMeleeAttackRange)
        {
            SetState(AIState.Melee);
            return;
        }
        else if (fDistance > m_fNormalRangeMax)
        {
            SetState(AIState.Chase);
            return;
        }

        // [범위에 들어왔다면 > 총을 쏜다]
        if (m_fAttackTimer <= 0f)
        {
            m_fAttackTimer = m_fNormalFireInterval;
            m_cNpc.Shoot();
        }
    }

    private void UpdateMelee()
    {
        float fDistance = Vector3.Distance(transform.position, m_trCurrentTarget.position);

        if (fDistance > m_fMeleeAttackRange)
        {
            DecideCombatState();
            return;
        }

        if (m_fAttackTimer <= 0f)
        {
            m_fAttackTimer = m_fMeleeInterval;
            m_cNpc.Melee();
        }
    }

    private void DecideCombatState()
    {
        if (m_trCurrentTarget == null)
        {
            SetState(AIState.Idle);
            return;
        }

        float fDistance = Vector3.Distance(transform.position, m_trCurrentTarget.position);

        if (fDistance <= m_fMeleeAttackRange)
        {
            SetState(AIState.Melee);
        }
        else if (fDistance >= m_fSniperRangeMin)
        {
            SetState(AIState.Snipe);
        }
        else if (fDistance <= m_fNormalRangeMax)
        {
            SetState(AIState.NormalAttack);
        }
        else
        {
            SetState(AIState.Chase);
        }
    }

    private Transform FindTarget()
    {
        // GameManager의 CurrentPlayer를 우선 참조하여 FindGameObjectWithTag 비용 절감
        if (GameManager.s_cInstance != null && GameManager.s_cInstance.CurrentPlayer != null)
        {
            Player cTargetPlayer = GameManager.s_cInstance.CurrentPlayer;
            if (cTargetPlayer.gameObject != this.gameObject)
            {
                float fDist = Vector3.Distance(transform.position, cTargetPlayer.transform.position);
                if (fDist <= m_fDetectionRadius)
                    return cTargetPlayer.transform;
            }
        }

        Collider[] arrHits = Physics.OverlapSphere(transform.position, m_fDetectionRadius);
        Transform trSecondaryMatch = null;

        for (int i = 0; i < arrHits.Length; i++)
        {
            Collider cHit = arrHits[i];
            if (cHit.gameObject == this.gameObject) continue;

            if (cHit.CompareTag(m_strPrimaryTargetTag))
            {
                return cHit.transform;
            }
            if (trSecondaryMatch == null && cHit.CompareTag(m_strSecondaryTargetTag))
            {
                trSecondaryMatch = cHit.transform;
            }
        }

        return trSecondaryMatch;
    }

    void OnDrawGizmos()
    {
        if (m_bShowOnlyWhenSelected) return;
        DrawAIGizmos();
    }

    void OnDrawGizmosSelected()
    {
        if (!m_bShowOnlyWhenSelected) return;
        DrawAIGizmos();
    }

    private void DrawAIGizmos()
    {
        if (!m_bShowGizmos) return;

        Vector3 vCenter = transform.position;

        // 1. 탐지 반경 (노랑)
        Gizmos.color = new Color(1f, 0.92f, 0.016f, 0.4f);
        Gizmos.DrawWireSphere(vCenter, m_fDetectionRadius);

        // 2. 저격 사거리 (파랑)
        Gizmos.color = new Color(0f, 0.5f, 1f, 0.6f);
        Gizmos.DrawWireSphere(vCenter, m_fSniperRangeMin);

        // 3. 일반 사거리 최대 (시안)
        Gizmos.color = new Color(0f, 1f, 1f, 0.5f);
        Gizmos.DrawWireSphere(vCenter, m_fNormalRangeMax);

        // 4. 근접 공격 사거리 (빨강)
        Gizmos.color = new Color(1f, 0f, 0f, 0.8f);
        Gizmos.DrawWireSphere(vCenter, m_fMeleeAttackRange);

        // 5. 타깃 조준선 및 시선 방향
        if (m_trCurrentTarget != null)
        {
            Vector3 vAimPoint = m_trCurrentTarget.position + Vector3.up * 1.0f;
            Vector3 vMyEyePos = transform.position + Vector3.up * 1.4f;

            switch (m_eCurrentState)
            {
                case AIState.Snipe: Gizmos.color = Color.cyan; break;
                case AIState.NormalAttack: Gizmos.color = Color.red; break;
                case AIState.Chase: Gizmos.color = Color.yellow; break;
                case AIState.Melee: Gizmos.color = new Color(1f, 0.5f, 0f); break;
                default: Gizmos.color = Color.green; break;
            }

            Gizmos.DrawLine(vMyEyePos, vAimPoint);
            Gizmos.DrawWireSphere(vAimPoint, 0.25f);
        }
        else
        {
            // 타깃이 없을 땐 정면 시선 방향 표시
            Gizmos.color = Color.gray;
            Gizmos.DrawRay(transform.position + Vector3.up * 1.4f, transform.forward * 3f);
        }
    }
}
