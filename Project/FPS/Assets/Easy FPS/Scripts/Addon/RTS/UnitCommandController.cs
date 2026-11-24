using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// RTS 모드에서 선택된 유닛들에게 이동, 집중 사격, 홀드, 집결, PC 추종 명령을 하달하는 컨트롤러
/// </summary>
public class UnitCommandController : MonoBehaviour
{
    [Header("Formation Settings")]
    [SerializeField]
    private float m_fFormationSpacing = 2.0f;

    [SerializeField]
    private float m_fRightClickDragThreshold = 8f;

    private RtsCameraController m_cRtsCamera;
    private UnitSelectionManager m_cSelectionManager;
    private bool m_bCommandEnabled = false;
    private Vector3 m_vRightDownPos;
    private bool m_bRightDownValid = false;

    public void Initialize(RtsCameraController _cRtsCamera, UnitSelectionManager _cSelectionManager)
    {
        m_cRtsCamera = _cRtsCamera;
        m_cSelectionManager = _cSelectionManager;
        m_bCommandEnabled = false;
    }

    public void SetCommandEnabled(bool _bEnabled)
    {
        m_bCommandEnabled = _bEnabled;
        if (!_bEnabled)
        {
            m_bRightDownValid = false;
        }
    }

    private void Update()
    {
        if (!m_bCommandEnabled || m_cRtsCamera == null || m_cSelectionManager == null) return;
        if (m_cRtsCamera.TacticalCamera == null) return;
        if (!GUIManager.CanRunSimulation()) return;

        IReadOnlyList<RtsUnitAgent> listSelected = m_cSelectionManager.SelectedUnits;

        // 1. 우클릭 이동 / 집중 사격 (우클릭 드래그 카메라 회전과 구분)
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

        if (listSelected.Count == 0) return;

        // 2. 단축키 명령: [H] 홀드
        if (Input.GetKeyDown(KeyCode.H))
        {
            for (int i = 0; i < listSelected.Count; i++)
            {
                listSelected[i]?.IssueHoldCommand();
            }
            m_cSelectionManager.UpdateRtsHudStatus("Order: HOLD");
        }
        // 3. 단축키 명령: [G] 집결 (마우스 지면 위치 또는 PC 위치)
        else if (Input.GetKeyDown(KeyCode.G))
        {
            Vector3 vRallyPos;
            if (TryGetGroundPoint(Input.mousePosition, out vRallyPos))
            {
                IssueFormationMove(listSelected, vRallyPos);
                m_cSelectionManager.UpdateRtsHudStatus("Order: RALLY");
            }
            else if (GameManager.s_cInstance != null && GameManager.s_cInstance.CurrentPlayer != null)
            {
                IssueFormationMove(listSelected, GameManager.s_cInstance.CurrentPlayer.transform.position);
                m_cSelectionManager.UpdateRtsHudStatus("Order: RALLY TO PC");
            }
        }
        // 4. 단축키 명령: [F] PC 추종
        else if (Input.GetKeyDown(KeyCode.F))
        {
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
