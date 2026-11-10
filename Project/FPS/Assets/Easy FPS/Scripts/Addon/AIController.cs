using UnityEngine;

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
    [SerializeField] private AIState currentState = AIState.Idle;
    public AIState CurrentState => currentState;

    private Player npc;

    [Header("AI Detection Settings")]
    [Tooltip("적 탐지 반경")]
    public float detectionRadius = 40f;
    [Tooltip("우선 탐색할 타깃 태그 (기본: Player)")]
    public string primaryTargetTag = "Player";
    [Tooltip("보조 탐색할 타깃 태그 (예: Dummie)")]
    public string secondaryTargetTag = "Dummie";

    [Header("Combat Ranges")]
    [Tooltip("저격 사거리 (이 거리 이상이면 저격총으로 교환 후 사격)")]
    public float sniperRangeMin = 15f;
    [Tooltip("일반 사거리 최대값 (이 거리 이내이면 일반총 사격, 벗어나면 이동)")]
    public float normalRangeMax = 15f;
    [Tooltip("근접 공격 사거리")]
    public float meleeAttackRange = 2.5f;

    [Header("Attack Fire Rates (Seconds)")]
    [Tooltip("저격총 사격 간격 (초)")]
    public float sniperFireInterval = 1.5f;
    [Tooltip("일반총 사격 간격 (초)")]
    public float normalFireInterval = 0.25f;
    [Tooltip("근접 공격 간격 (초)")]
    public float meleeInterval = 0.8f;

    [Header("Target Tracking")]
    public Transform currentTarget;

    private float targetSearchTimer = 0f;
    private float attackTimer = 0f;

    void Awake()
    {
        npc = GetComponent<Player>();
    }

    void Start()
    {
        Debug.Log($"{gameObject.name}.Start()");
        SetState(AIState.Idle);
    }

    void Update()
    {
        if (!npc) return;

        // 공격 쿨다운 타이머 감소
        if (attackTimer > 0f)
            attackTimer -= Time.deltaTime;

        // 주기적인 적 타깃 탐색 (0.5초마다)
        targetSearchTimer -= Time.deltaTime;
        if (targetSearchTimer <= 0f || currentTarget == null)
        {
            targetSearchTimer = 0.5f;
            currentTarget = FindTarget();
        }

        // 상태 머신 업데이트
        UpdateState();
    }

    // ==================== 유한상태머신 (FSM) 함수 ====================

    public void SetState(AIState newState)
    {
        if (currentState == newState) return;

        // 이전 상태 Exit
        switch (currentState)
        {
            case AIState.Chase:
                npc.MoveWorld(Vector3.zero);
                break;

            case AIState.Snipe:
                npc.Aim(false);
                break;
        }

        currentState = newState;
        attackTimer = 0f; // 상태 전이 시 즉시 1발 발사 가능하도록 쿨다운 리셋

        // 새 상태 Enter
        switch (currentState)
        {
            case AIState.Idle:
                npc.MoveWorld(Vector3.zero);
                npc.Aim(false);
                break;

            case AIState.Chase:
                npc.Aim(false);
                break;

            case AIState.Snipe:
                npc.MoveWorld(Vector3.zero);
                npc.SwitchWeapon(1); // 저격총(NewGun_semi) 교체
                npc.Aim(true);        // 조준 활성화
                break;

            case AIState.NormalAttack:
                npc.MoveWorld(Vector3.zero);
                npc.SwitchWeapon(0); // 일반총(NewGun_auto) 교체
                npc.Aim(false);
                break;

            case AIState.Melee:
                npc.MoveWorld(Vector3.zero);
                npc.Aim(false);
                npc.Melee();
                attackTimer = meleeInterval;
                break;
        }
    }

    public void UpdateState()
    {
        // 타깃이 없으면 Idle
        if (currentTarget == null)
        {
            if (currentState != AIState.Idle)
                SetState(AIState.Idle);
            UpdateIdle();
            return;
        }

        // 타깃의 가슴/상체 위치(y + 1.0f)를 지속적으로 조준
        Vector3 aimPoint = currentTarget.position + Vector3.up * 1.0f;
        npc.LookAtTarget(aimPoint);

        switch (currentState)
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
        if (currentTarget != null)
        {
            DecideCombatState();
        }
    }

    private void UpdateChase()
    {
        float distance = Vector3.Distance(transform.position, currentTarget.position);

        if (distance <= meleeAttackRange)
        {
            SetState(AIState.Melee);
            return;
        }
        else if (distance >= sniperRangeMin)
        {
            SetState(AIState.Snipe);
            return;
        }
        else if (distance <= normalRangeMax)
        {
            SetState(AIState.NormalAttack);
            return;
        }

        // [범위에 들어오지 않았으면 > 이동]
        Vector3 toTargetDir = (currentTarget.position - transform.position).normalized;
        npc.MoveWorld(toTargetDir);
    }

    private void UpdateSnipe()
    {
        float distance = Vector3.Distance(transform.position, currentTarget.position);

        if (distance < sniperRangeMin)
        {
            DecideCombatState();
            return;
        }

        // [적이 저격거리에 있으면 > 저격총으로 쏜다]
        npc.Aim(true);

        if (attackTimer <= 0f)
        {
            attackTimer = sniperFireInterval;
            npc.Shoot();
        }
    }

    private void UpdateNormalAttack()
    {
        float distance = Vector3.Distance(transform.position, currentTarget.position);

        if (distance <= meleeAttackRange)
        {
            SetState(AIState.Melee);
            return;
        }
        else if (distance > normalRangeMax)
        {
            SetState(AIState.Chase);
            return;
        }

        // [범위에 들어왔다면 > 총을 쏜다]
        if (attackTimer <= 0f)
        {
            attackTimer = normalFireInterval;
            npc.Shoot();
        }
    }

    private void UpdateMelee()
    {
        float distance = Vector3.Distance(transform.position, currentTarget.position);

        if (distance > meleeAttackRange)
        {
            DecideCombatState();
            return;
        }

        if (attackTimer <= 0f)
        {
            attackTimer = meleeInterval;
            npc.Melee();
        }
    }

    private void DecideCombatState()
    {
        if (currentTarget == null)
        {
            SetState(AIState.Idle);
            return;
        }

        float distance = Vector3.Distance(transform.position, currentTarget.position);

        if (distance <= meleeAttackRange)
        {
            SetState(AIState.Melee);
        }
        else if (distance >= sniperRangeMin)
        {
            SetState(AIState.Snipe);
        }
        else if (distance <= normalRangeMax)
        {
            SetState(AIState.NormalAttack);
        }
        else
        {
            SetState(AIState.Chase);
        }
    }

    Transform FindTarget()
    {
        GameObject playerObj = GameObject.FindGameObjectWithTag(primaryTargetTag);
        if (playerObj && playerObj != this.gameObject)
        {
            float dist = Vector3.Distance(transform.position, playerObj.transform.position);
            if (dist <= detectionRadius)
                return playerObj.transform;
        }

        GameObject dummieObj = GameObject.FindGameObjectWithTag(secondaryTargetTag);
        if (dummieObj && dummieObj != this.gameObject)
        {
            float dist = Vector3.Distance(transform.position, dummieObj.transform.position);
            if (dist <= detectionRadius)
                return dummieObj.transform;
        }

        Collider[] hits = Physics.OverlapSphere(transform.position, detectionRadius);
        foreach (var hit in hits)
        {
            if (hit.gameObject == this.gameObject) continue;
            if (hit.CompareTag(primaryTargetTag) || hit.CompareTag(secondaryTargetTag))
            {
                return hit.transform;
            }
        }

        return null;
    }

    [Header("Gizmo Debug Settings")]
    [Tooltip("Scene 뷰에 AI 기즈모(사거리 및 조준선) 표시 여부")]
    public bool showGizmos = true;
    public bool showOnlyWhenSelected = false;

    void OnDrawGizmos()
    {
        if (showOnlyWhenSelected) return;
        DrawAIGizmos();
    }

    void OnDrawGizmosSelected()
    {
        if (!showOnlyWhenSelected) return;
        DrawAIGizmos();
    }

    void DrawAIGizmos()
    {
        if (!showGizmos) return;

        Vector3 center = transform.position;

        // 1. 탐지 반경 (노랑)
        Gizmos.color = new Color(1f, 0.92f, 0.016f, 0.4f);
        Gizmos.DrawWireSphere(center, detectionRadius);

        // 2. 저격 사거리 (파랑)
        Gizmos.color = new Color(0f, 0.5f, 1f, 0.6f);
        Gizmos.DrawWireSphere(center, sniperRangeMin);

        // 3. 일반 사거리 최대 (시안)
        Gizmos.color = new Color(0f, 1f, 1f, 0.5f);
        Gizmos.DrawWireSphere(center, normalRangeMax);

        // 4. 근접 공격 사거리 (빨강)
        Gizmos.color = new Color(1f, 0f, 0f, 0.8f);
        Gizmos.DrawWireSphere(center, meleeAttackRange);

        // 5. 타깃 조준선 및 시선 방향
        if (currentTarget != null)
        {
            Vector3 aimPoint = currentTarget.position + Vector3.up * 1.0f;
            Vector3 myEyePos = transform.position + Vector3.up * 1.4f;

            // 상태에 따른 선 색상
            switch (currentState)
            {
                case AIState.Snipe: Gizmos.color = Color.cyan; break;
                case AIState.NormalAttack: Gizmos.color = Color.red; break;
                case AIState.Chase: Gizmos.color = Color.yellow; break;
                case AIState.Melee: Gizmos.color = new Color(1f, 0.5f, 0f); break;
                default: Gizmos.color = Color.green; break;
            }

            Gizmos.DrawLine(myEyePos, aimPoint);
            Gizmos.DrawWireSphere(aimPoint, 0.25f);
        }
        else
        {
            // 타깃이 없을 땐 정면 시선 방향 표시
            Gizmos.color = Color.gray;
            Gizmos.DrawRay(transform.position + Vector3.up * 1.4f, transform.forward * 3f);
        }
    }
}
