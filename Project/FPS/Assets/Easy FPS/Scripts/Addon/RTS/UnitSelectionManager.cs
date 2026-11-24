using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// RTS 모드에서 클릭 및 드래그 박스로 아군 유닛(PC 및 아군 NPC)을 다중 선택하고
/// 선택 링(Selection Ring)과 RTS HUD 상태 표시를 관리하는 매니저
/// </summary>
public class UnitSelectionManager : MonoBehaviour
{
    [Header("Selection Settings")]
    [SerializeField]
    private float m_fDragThreshold = 10f;

    [SerializeField]
    private Color m_colorBoxFill = new Color(0.2f, 0.8f, 1f, 0.18f);

    [SerializeField]
    private Color m_colorBoxBorder = new Color(0.3f, 0.9f, 1f, 0.85f);

    private readonly List<RtsUnitAgent> m_listSelectedUnits = new List<RtsUnitAgent>();

    public event Action<int> OnSelectionCountChanged;

    private RtsCameraController m_cRtsCamera;
    private bool m_bSelectionEnabled = false;
    private bool m_bIsMouseDown = false;
    private bool m_bIsDragging = false;
    private Vector3 m_vDragStartScreenPos;
    private Texture2D m_texPixel;

    public IReadOnlyList<RtsUnitAgent> SelectedUnits => m_listSelectedUnits;

    public void Initialize(RtsCameraController _cRtsCamera)
    {
        m_cRtsCamera = _cRtsCamera;
        m_bSelectionEnabled = false;
    }

    public void SetSelectionEnabled(bool _bEnabled)
    {
        m_bSelectionEnabled = _bEnabled;
        if (!_bEnabled)
        {
            m_bIsMouseDown = false;
            m_bIsDragging = false;
        }
        UpdateRtsHudStatus();
    }

    public void ClearSelection()
    {
        for (int i = 0; i < m_listSelectedUnits.Count; i++)
        {
            if (m_listSelectedUnits[i] != null)
            {
                m_listSelectedUnits[i].SetSelected(false);
            }
        }
        m_listSelectedUnits.Clear();
        UpdateRtsHudStatus();
    }

    private void Update()
    {
        if (!m_bSelectionEnabled || m_cRtsCamera == null || m_cRtsCamera.TacticalCamera == null) return;
        if (!GUIManager.CanRunSimulation()) return;

        // 죽거나 파괴된 유닛 정리
        m_listSelectedUnits.RemoveAll(cUnit => cUnit == null || (cUnit.UnitPlayer != null && cUnit.UnitPlayer.IsDead));

        if (Input.GetMouseButtonDown(0))
        {
            if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
            {
                return;
            }

            // 커맨드 카드의 타깃팅 대기 명령(Move/Attack)이 활성 중이면 좌클릭을 명령 컨트롤러에 양보
            if (PlayModeManager.s_cInstance != null &&
                PlayModeManager.s_cInstance.CommandController != null &&
                PlayModeManager.s_cInstance.CommandController.HasPendingTargetCommand)
            {
                return;
            }

            m_bIsMouseDown = true;
            m_bIsDragging = false;
            m_vDragStartScreenPos = Input.mousePosition;
        }

        if (m_bIsMouseDown && Input.GetMouseButton(0))
        {
            if (!m_bIsDragging && Vector3.Distance(m_vDragStartScreenPos, Input.mousePosition) >= m_fDragThreshold)
            {
                m_bIsDragging = true;
            }
        }

        if (m_bIsMouseDown && Input.GetMouseButtonUp(0))
        {
            bool bAdditive = Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);
            if (m_bIsDragging)
            {
                SelectUnitsInDragBox(m_vDragStartScreenPos, Input.mousePosition, bAdditive);
            }
            else
            {
                SelectSingleUnitAtPointer(Input.mousePosition, bAdditive);
            }

            m_bIsMouseDown = false;
            m_bIsDragging = false;
            UpdateRtsHudStatus();
        }
    }

    private void SelectSingleUnitAtPointer(Vector3 _vScreenPos, bool _bAdditive)
    {
        Camera cam = m_cRtsCamera.TacticalCamera;
        Ray sRay = cam.ScreenPointToRay(_vScreenPos);
        RtsUnitAgent cHitAgent = null;

        if (Physics.Raycast(sRay, out RaycastHit sHit, 500f))
        {
            cHitAgent = sHit.collider.GetComponentInParent<RtsUnitAgent>();
        }

        if (!_bAdditive)
        {
            ClearSelection();
        }

        if (cHitAgent != null && cHitAgent.IsAlly && (cHitAgent.UnitPlayer == null || !cHitAgent.UnitPlayer.IsDead))
        {
            if (_bAdditive && m_listSelectedUnits.Contains(cHitAgent))
            {
                cHitAgent.SetSelected(false);
                m_listSelectedUnits.Remove(cHitAgent);
            }
            else if (!m_listSelectedUnits.Contains(cHitAgent))
            {
                cHitAgent.SetSelected(true);
                m_listSelectedUnits.Add(cHitAgent);
            }
        }
    }

    private void SelectUnitsInDragBox(Vector3 _vStart, Vector3 _vEnd, bool _bAdditive)
    {
        if (!_bAdditive)
        {
            ClearSelection();
        }

        Camera cam = m_cRtsCamera.TacticalCamera;
        float fMinX = Mathf.Min(_vStart.x, _vEnd.x);
        float fMaxX = Mathf.Max(_vStart.x, _vEnd.x);
        float fMinY = Mathf.Min(_vStart.y, _vEnd.y);
        float fMaxY = Mathf.Max(_vStart.y, _vEnd.y);

        RtsUnitAgent[] arrAllAgents = FindObjectsByType<RtsUnitAgent>(FindObjectsSortMode.None);
        for (int i = 0; i < arrAllAgents.Length; i++)
        {
            RtsUnitAgent cAgent = arrAllAgents[i];
            if (cAgent == null || !cAgent.IsAlly) continue;
            if (cAgent.UnitPlayer != null && cAgent.UnitPlayer.IsDead) continue;

            Vector3 vScreenPoint = cam.WorldToScreenPoint(cAgent.transform.position + Vector3.up * 0.5f);
            if (vScreenPoint.z < 0f) continue;

            if (vScreenPoint.x >= fMinX && vScreenPoint.x <= fMaxX &&
                vScreenPoint.y >= fMinY && vScreenPoint.y <= fMaxY)
            {
                if (!m_listSelectedUnits.Contains(cAgent))
                {
                    cAgent.SetSelected(true);
                    m_listSelectedUnits.Add(cAgent);
                }
            }
        }
    }

    public void UpdateRtsHudStatus(string _strLastAction = null)
    {
        OnSelectionCountChanged?.Invoke(m_listSelectedUnits.Count);

        if (GUIManager.s_cInstance == null) return;

        RtsGuiScene cRtsHud = GUIManager.s_cInstance.GetGuiScene<RtsGuiScene>();
        if (cRtsHud == null) return;

        string strActionSuffix = string.IsNullOrEmpty(_strLastAction) ? string.Empty : $"  |  {_strLastAction}";
        cRtsHud.SetTacticalStatus($"Selected: {m_listSelectedUnits.Count}  |  [RMB] Move/Attack  [M/X/H/T/G/F/R] Command Card  [Tab] FPS{strActionSuffix}");
    }

    private void OnGUI()
    {
        if (!m_bSelectionEnabled || !m_bIsDragging) return;
        if (!GUIManager.CanRunSimulation()) return;

        EnsurePixelTexture();

        Vector3 vStart = m_vDragStartScreenPos;
        Vector3 vEnd = Input.mousePosition;

        float fX = Mathf.Min(vStart.x, vEnd.x);
        float fY = Screen.height - Mathf.Max(vStart.y, vEnd.y);
        float fW = Mathf.Abs(vEnd.x - vStart.x);
        float fH = Mathf.Abs(vEnd.y - vStart.y);

        Rect sBoxRect = new Rect(fX, fY, fW, fH);

        Color sPrevColor = GUI.color;
        GUI.color = m_colorBoxFill;
        GUI.DrawTexture(sBoxRect, m_texPixel);

        GUI.color = m_colorBoxBorder;
        const float fBorder = 2f;
        GUI.DrawTexture(new Rect(sBoxRect.x, sBoxRect.y, sBoxRect.width, fBorder), m_texPixel);
        GUI.DrawTexture(new Rect(sBoxRect.x, sBoxRect.yMax - fBorder, sBoxRect.width, fBorder), m_texPixel);
        GUI.DrawTexture(new Rect(sBoxRect.x, sBoxRect.y, fBorder, sBoxRect.height), m_texPixel);
        GUI.DrawTexture(new Rect(sBoxRect.xMax - fBorder, sBoxRect.y, fBorder, sBoxRect.height), m_texPixel);
        GUI.color = sPrevColor;
    }

    private void EnsurePixelTexture()
    {
        if (m_texPixel != null) return;
        m_texPixel = new Texture2D(1, 1);
        m_texPixel.SetPixel(0, 0, Color.white);
        m_texPixel.Apply();
    }
}
