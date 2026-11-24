using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// 스타크래프트 스타일 우측 하단 3x3 (9개 슬롯) 커맨더 명령 액션 종류
/// </summary>
public enum ERtsCommanderSlotAction
{
    None = -1,
    Move = 0,       // [M] 이동 (지점 지정)
    Stop = 1,       // [S] 정지 / 명령 해제
    Hold = 2,       // [H] 제자리 사수
    Attack = 3,     // [A] 공격 / 집중 사격 (타깃 지정)
    Rally = 4,      // [G] 집결
    Follow = 5,     // [F] 지휘관(PC) 추종
    Reload = 6,     // [R] 선택 유닛 일괄 재장전
    FocusPc = 7,    // [Space] 지휘관(PC) 카메라 포커스
    SwitchFps = 8   // [Tab] FPS 1인칭 빙의 전환
}

/// <summary>
/// 커맨드 카드의 개별 슬롯 정의 (단축키, 표시명, 유닛 선택 필요 여부)
/// </summary>
public struct RtsCommandSlotInfo
{
    public ERtsCommanderSlotAction m_eAction;
    public string m_strTitle;
    public string m_strHotkeyLabel;
    public KeyCode m_eHotkey;
    public bool m_bRequiresSelection;

    public RtsCommandSlotInfo(ERtsCommanderSlotAction _eAction, string _strTitle, string _strHotkeyLabel, KeyCode _eHotkey, bool _bRequiresSelection)
    {
        m_eAction = _eAction;
        m_strTitle = _strTitle;
        m_strHotkeyLabel = _strHotkeyLabel;
        m_eHotkey = _eHotkey;
        m_bRequiresSelection = _bRequiresSelection;
    }
}

/// <summary>
/// RTS 모드에서 커맨더 단축키와 9슬롯 커맨드 카드 UI 이벤트를 통합 처리하는 명령 컨트롤러
/// 단축키 입력과 UI 슬롯 버튼 클릭이 모두 동일한 TriggerCommanderAction 이벤트를 통해 실행됩니다.
/// </summary>
public class UnitCommandController : MonoBehaviour
{
    public static readonly RtsCommandSlotInfo[] s_arrCommanderSlots = new RtsCommandSlotInfo[]
    {
        new RtsCommandSlotInfo(ERtsCommanderSlotAction.Move,      "MOVE",     "M",   KeyCode.M,     true),
        new RtsCommandSlotInfo(ERtsCommanderSlotAction.Stop,      "STOP",     "X",   KeyCode.X,     true),
        new RtsCommandSlotInfo(ERtsCommanderSlotAction.Hold,      "HOLD",     "H",   KeyCode.H,     true),
        new RtsCommandSlotInfo(ERtsCommanderSlotAction.Attack,    "ATTACK",   "T",   KeyCode.T,     true),
        new RtsCommandSlotInfo(ERtsCommanderSlotAction.Rally,     "RALLY",    "G",   KeyCode.G,     true),
        new RtsCommandSlotInfo(ERtsCommanderSlotAction.Follow,    "FOLLOW",   "F",   KeyCode.F,     true),
        new RtsCommandSlotInfo(ERtsCommanderSlotAction.Reload,    "RELOAD",   "R",   KeyCode.R,     true),
        new RtsCommandSlotInfo(ERtsCommanderSlotAction.FocusPc,   "FOCUS PC", "SPC", KeyCode.Space, false),
        new RtsCommandSlotInfo(ERtsCommanderSlotAction.SwitchFps, "FPS MODE", "TAB", KeyCode.Tab,   false)
    };

    [Header("Formation Settings")]
    [SerializeField]
    private float m_fFormationSpacing = 2.0f;

    [SerializeField]
    private float m_fRightClickDragThreshold = 8f;

    // 커맨더 명령 실행 및 타깃 대기 상태 변경 이벤트 (UI 슬롯/피드백 구독용)
    public event Action<ERtsCommanderSlotAction> OnCommanderActionTriggered;
    public event Action<ERtsCommanderSlotAction> OnPendingTargetCommandChanged;

    private RtsCameraController m_cRtsCamera;
    private UnitSelectionManager m_cSelectionManager;
    private bool m_bCommandEnabled = false;
    private Vector3 m_vRightDownPos;
    private bool m_bRightDownValid = false;

    // Move(M) / Attack(A)처럼 좌클릭 타깃 지정이 필요한 대기 명령 상태
    private ERtsCommanderSlotAction m_ePendingTargetCommand = ERtsCommanderSlotAction.None;

    public ERtsCommanderSlotAction PendingTargetCommand => m_ePendingTargetCommand;
    public bool HasPendingTargetCommand => m_ePendingTargetCommand != ERtsCommanderSlotAction.None;

    public void Initialize(RtsCameraController _cRtsCamera, UnitSelectionManager _cSelectionManager)
    {
        m_cRtsCamera = _cRtsCamera;
        m_cSelectionManager = _cSelectionManager;
        m_bCommandEnabled = false;
        SetPendingTargetCommand(ERtsCommanderSlotAction.None);
    }

    public void SetCommandEnabled(bool _bEnabled)
    {
        m_bCommandEnabled = _bEnabled;
        if (!_bEnabled)
        {
            m_bRightDownValid = false;
            SetPendingTargetCommand(ERtsCommanderSlotAction.None);
        }
    }

    private void SetPendingTargetCommand(ERtsCommanderSlotAction _ePending)
    {
        if (m_ePendingTargetCommand == _ePending) return;
        m_ePendingTargetCommand = _ePending;
        OnPendingTargetCommandChanged?.Invoke(m_ePendingTargetCommand);
    }

    private void Update()
    {
        if (!m_bCommandEnabled || m_cRtsCamera == null || m_cSelectionManager == null) return;
        if (m_cRtsCamera.TacticalCamera == null) return;
        if (!GUIManager.CanRunSimulation()) return;

        IReadOnlyList<RtsUnitAgent> listSelected = m_cSelectionManager.SelectedUnits;

        // 1. 타깃 지정 대기 중(Move / Attack)일 때 좌클릭으로 명령 확정, 우클릭으로 취소
        if (HasPendingTargetCommand)
        {
            if (Input.GetMouseButtonDown(0))
            {
                if (EventSystem.current == null || !EventSystem.current.IsPointerOverGameObject())
                {
                    ExecutePendingTargetCommand(Input.mousePosition, listSelected);
                    return;
                }
            }
            else if (Input.GetMouseButtonDown(1))
            {
                SetPendingTargetCommand(ERtsCommanderSlotAction.None);
                m_cSelectionManager.UpdateRtsHudStatus("Order Canceled");
                return;
            }
        }

        // 2. 일반 우클릭: 지면 이동 / 적 집중 사격 (우클릭 드래그 카메라 회전과 구분)
        if (Input.GetMouseButtonDown(1))
        {
            if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
            {
                m_bRightDownValid = false;
            }
            else
            {
                m_bRightDownValid = true;
                m_vRightDownPos = Input.mousePosition;
            }
        }

        if (m_bRightDownValid && Input.GetMouseButtonUp(1))
        {
            m_bRightDownValid = false;
            float fDragDist = Vector3.Distance(m_vRightDownPos, Input.mousePosition);
            if (fDragDist < m_fRightClickDragThreshold && listSelected.Count > 0)
            {
                ProcessRightClickCommand(Input.mousePosition, listSelected);
            }
        }

        // 3. 커맨더 단축키 입력을 이벤트(TriggerCommanderAction)로 전달
        //    (Tab은 PlayModeManager에서, Space는 RtsPlayMode에서 처리하되 버튼 클릭 시에도 동일 이벤트로 동작)
        //    WASD 카메라 이동과 충돌하는 A/S 키는 유닛이 선택된 상태에서 단축키로도 동작하도록 Shift 또는 단독 입력 허용:
        //    Move(M), Hold(H), Rally(G), Follow(F), Reload(R), Stop(S - 선택 시), Attack(A - 선택 시)
        if (Input.GetKeyDown(KeyCode.M)) TriggerCommanderAction(ERtsCommanderSlotAction.Move);
        else if (Input.GetKeyDown(KeyCode.H)) TriggerCommanderAction(ERtsCommanderSlotAction.Hold);
        else if (Input.GetKeyDown(KeyCode.G)) TriggerCommanderAction(ERtsCommanderSlotAction.Rally);
        else if (Input.GetKeyDown(KeyCode.F)) TriggerCommanderAction(ERtsCommanderSlotAction.Follow);
        else if (Input.GetKeyDown(KeyCode.R)) TriggerCommanderAction(ERtsCommanderSlotAction.Reload);
        else if (Input.GetKeyDown(KeyCode.X)) TriggerCommanderAction(ERtsCommanderSlotAction.Stop);
        else if (Input.GetKeyDown(KeyCode.T)) TriggerCommanderAction(ERtsCommanderSlotAction.Attack);
    }

    /// <summary>
    /// 단축키 및 우측 하단 9개 커맨드 카드 UI 슬롯 버튼이 공통 호출하는 커맨더 이벤트 진입점
    /// </summary>
    public void TriggerCommanderAction(ERtsCommanderSlotAction _eAction)
    {
        if (!m_bCommandEnabled && _eAction != ERtsCommanderSlotAction.SwitchFps) return;
        if (m_cSelectionManager == null) return;

        IReadOnlyList<RtsUnitAgent> listSelected = m_cSelectionManager.SelectedUnits;

        if (GUIManager.s_cInstance != null)
        {
            GUIManager.s_cInstance.PlayUiClickSound();
        }

        switch (_eAction)
        {
            case ERtsCommanderSlotAction.Move:
                if (listSelected.Count == 0)
                {
                    m_cSelectionManager.UpdateRtsHudStatus("Select unit(s) first");
                    return;
                }
                SetPendingTargetCommand(ERtsCommanderSlotAction.Move);
                m_cSelectionManager.UpdateRtsHudStatus("MOVE: Left-Click destination");
                break;

            case ERtsCommanderSlotAction.Attack:
                if (listSelected.Count == 0)
                {
                    m_cSelectionManager.UpdateRtsHudStatus("Select unit(s) first");
                    return;
                }
                SetPendingTargetCommand(ERtsCommanderSlotAction.Attack);
                m_cSelectionManager.UpdateRtsHudStatus("ATTACK: Left-Click target/ground");
                break;

            case ERtsCommanderSlotAction.Stop:
                SetPendingTargetCommand(ERtsCommanderSlotAction.None);
                for (int i = 0; i < listSelected.Count; i++)
                {
                    listSelected[i]?.ClearCommand();
                }
                m_cSelectionManager.UpdateRtsHudStatus("Order: STOP");
                break;

            case ERtsCommanderSlotAction.Hold:
                SetPendingTargetCommand(ERtsCommanderSlotAction.None);
                for (int i = 0; i < listSelected.Count; i++)
                {
                    listSelected[i]?.IssueHoldCommand();
                }
                m_cSelectionManager.UpdateRtsHudStatus("Order: HOLD");
                break;

            case ERtsCommanderSlotAction.Rally:
                SetPendingTargetCommand(ERtsCommanderSlotAction.None);
                if (listSelected.Count == 0) return;
                if (TryGetGroundPoint(Input.mousePosition, out Vector3 vRallyPos))
                {
                    IssueFormationMove(listSelected, vRallyPos);
                    m_cSelectionManager.UpdateRtsHudStatus("Order: RALLY");
                }
                else if (GameManager.s_cInstance != null && GameManager.s_cInstance.CurrentPlayer != null)
                {
                    IssueFormationMove(listSelected, GameManager.s_cInstance.CurrentPlayer.transform.position);
                    m_cSelectionManager.UpdateRtsHudStatus("Order: RALLY TO PC");
                }
                break;

            case ERtsCommanderSlotAction.Follow:
                SetPendingTargetCommand(ERtsCommanderSlotAction.None);
                Player cPc = GameManager.s_cInstance != null ? GameManager.s_cInstance.CurrentPlayer : null;
                if (cPc != null)
                {
                    for (int i = 0; i < listSelected.Count; i++)
                    {
                        RtsUnitAgent cUnit = listSelected[i];
                        if (cUnit != null && cUnit.transform != cPc.transform)
                        {
                            cUnit.IssueFollowCommand(cPc.transform);
                        }
                    }
                    m_cSelectionManager.UpdateRtsHudStatus("Order: FOLLOW PC");
                }
                break;

            case ERtsCommanderSlotAction.Reload:
                SetPendingTargetCommand(ERtsCommanderSlotAction.None);
                for (int i = 0; i < listSelected.Count; i++)
                {
                    listSelected[i]?.UnitPlayer?.Reload();
                }
                m_cSelectionManager.UpdateRtsHudStatus("Order: RELOAD");
                break;

            case ERtsCommanderSlotAction.FocusPc:
                SetPendingTargetCommand(ERtsCommanderSlotAction.None);
                Player cCommander = GameManager.s_cInstance != null ? GameManager.s_cInstance.CurrentPlayer : null;
                if (cCommander != null && m_cRtsCamera != null)
                {
                    m_cRtsCamera.FocusOnPosition(cCommander.transform.position);
                    m_cSelectionManager.UpdateRtsHudStatus("Camera: FOCUS PC");
                }
                break;

            case ERtsCommanderSlotAction.SwitchFps:
                SetPendingTargetCommand(ERtsCommanderSlotAction.None);
                if (PlayModeManager.s_cInstance != null)
                {
                    PlayModeManager.s_cInstance.SwitchToFps();
                }
                break;
        }

        OnCommanderActionTriggered?.Invoke(_eAction);
    }

    private void ExecutePendingTargetCommand(Vector3 _vScreenPos, IReadOnlyList<RtsUnitAgent> _listSelected)
    {
        ERtsCommanderSlotAction eAction = m_ePendingTargetCommand;
        SetPendingTargetCommand(ERtsCommanderSlotAction.None);

        if (_listSelected == null || _listSelected.Count == 0) return;

        Camera cam = m_cRtsCamera.TacticalCamera;
        Ray sRay = cam.ScreenPointToRay(_vScreenPos);
        if (!Physics.Raycast(sRay, out RaycastHit sHit, 500f)) return;

        if (eAction == ERtsCommanderSlotAction.Move)
        {
            IssueFormationMove(_listSelected, sHit.point);
            m_cSelectionManager.UpdateRtsHudStatus("Order: MOVE");
        }
        else if (eAction == ERtsCommanderSlotAction.Attack)
        {
            RtsUnitAgent cHitAgent = sHit.collider.GetComponentInParent<RtsUnitAgent>();
            bool bIsEnemyAgent = (cHitAgent != null && !cHitAgent.IsAlly);
            bool bIsEnemyTag = (sHit.collider.CompareTag("Dummie") || sHit.collider.CompareTag("Enemy"));

            if (bIsEnemyAgent || bIsEnemyTag)
            {
                Transform trTarget = cHitAgent != null ? cHitAgent.transform : sHit.collider.transform;
                for (int i = 0; i < _listSelected.Count; i++)
                {
                    _listSelected[i]?.IssueAttackTargetCommand(trTarget);
                }
                m_cSelectionManager.UpdateRtsHudStatus($"Order: ATTACK ({trTarget.name})");
            }
            else
            {
                IssueFormationMove(_listSelected, sHit.point);
                m_cSelectionManager.UpdateRtsHudStatus("Order: ATTACK-MOVE");
            }
        }
    }

    private void ProcessRightClickCommand(Vector3 _vScreenPos, IReadOnlyList<RtsUnitAgent> _listSelected)
    {
        Camera cam = m_cRtsCamera.TacticalCamera;
        Ray sRay = cam.ScreenPointToRay(_vScreenPos);

        if (!Physics.Raycast(sRay, out RaycastHit sHit, 500f)) return;

        // 적 유닛 클릭 판정 (비-아군 RtsUnitAgent 또는 Dummie/Enemy 태그)
        RtsUnitAgent cHitAgent = sHit.collider.GetComponentInParent<RtsUnitAgent>();
        bool bIsEnemyAgent = (cHitAgent != null && !cHitAgent.IsAlly);
        bool bIsEnemyTag = (sHit.collider.CompareTag("Dummie") || sHit.collider.CompareTag("Enemy"));

        if (bIsEnemyAgent || bIsEnemyTag)
        {
            Transform trTarget = cHitAgent != null ? cHitAgent.transform : sHit.collider.transform;
            for (int i = 0; i < _listSelected.Count; i++)
            {
                _listSelected[i]?.IssueAttackTargetCommand(trTarget);
            }
            m_cSelectionManager.UpdateRtsHudStatus($"Order: ATTACK ({trTarget.name})");
            return;
        }

        // 지면 이동 명령 (다중 선택 시 포메이션 분산)
        IssueFormationMove(_listSelected, sHit.point);
        m_cSelectionManager.UpdateRtsHudStatus("Order: MOVE");
    }

    private void IssueFormationMove(IReadOnlyList<RtsUnitAgent> _listSelected, Vector3 _vCenterPos)
    {
        int nCount = _listSelected.Count;
        if (nCount == 1)
        {
            _listSelected[0]?.IssueMoveCommand(_vCenterPos);
            return;
        }

        for (int i = 0; i < nCount; i++)
        {
            float fAngle = (float)i / nCount * Mathf.PI * 2f;
            Vector3 vOffset = new Vector3(Mathf.Cos(fAngle), 0f, Mathf.Sin(fAngle)) * m_fFormationSpacing;
            _listSelected[i]?.IssueMoveCommand(_vCenterPos + vOffset);
        }
    }

    private bool TryGetGroundPoint(Vector3 _vScreenPos, out Vector3 _vGroundPoint)
    {
        _vGroundPoint = Vector3.zero;
        Camera cam = m_cRtsCamera.TacticalCamera;
        Ray sRay = cam.ScreenPointToRay(_vScreenPos);
        if (Physics.Raycast(sRay, out RaycastHit sHit, 500f))
        {
            _vGroundPoint = sHit.point;
            return true;
        }
        return false;
    }
}
