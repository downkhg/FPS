using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// RTS 전술 화면 GUI 프리팹 컴포넌트
/// - RTS 진입 시 Time.timeScale = 1f 유지 + 마우스 커서 해제 정책 제공
/// - 스타크래프트 스타일 우측 하단 3x3 (9개 슬롯) 커맨드 카드 UI를 통해
///   단축키와 동일한 커맨더 이벤트(TriggerCommanderAction)를 표시 및 실행합니다.
/// </summary>
public class RtsGuiScene : GuiSceneBase
{
    private const int COMMAND_SLOT_COUNT = 9;

    [Header("RTS HUD Controls")]
    [SerializeField]
    private TextMeshProUGUI m_txtModeHeader;

    [SerializeField]
    private TextMeshProUGUI m_txtTacticalStatus;

    [SerializeField]
    private Button m_btnReturnToFps;

    [Header("Commander 3x3 Command Card (Bottom-Right)")]
    [SerializeField]
    private RectTransform m_rtCommandCardPanel;

    private readonly Button[] m_arrSlotButtons = new Button[COMMAND_SLOT_COUNT];
    private readonly Image[] m_arrSlotBackgrounds = new Image[COMMAND_SLOT_COUNT];
    private readonly TextMeshProUGUI[] m_arrSlotLabels = new TextMeshProUGUI[COMMAND_SLOT_COUNT];

    private bool m_bCommandCardBuilt = false;
    private bool m_bEventsBound = false;
    private int m_nSelectedUnitCount = 0;
    private ERtsCommanderSlotAction m_ePendingAction = ERtsCommanderSlotAction.None;

    private readonly Color m_colorSlotNormal = new Color(0.14f, 0.20f, 0.28f, 0.94f);
    private readonly Color m_colorSlotDisabled = new Color(0.10f, 0.12f, 0.15f, 0.65f);
    private readonly Color m_colorSlotPending = new Color(0.82f, 0.52f, 0.12f, 0.98f);
    private readonly Color m_colorSlotUtility = new Color(0.16f, 0.34f, 0.26f, 0.94f);

    private void Reset()
    {
        ApplyRtsScenePolicy();
    }

    public override void Initialize(GUIManager _cManager)
    {
        ApplyRtsScenePolicy();
        base.Initialize(_cManager);

        if (m_cManager != null && m_btnReturnToFps != null)
        {
            m_btnReturnToFps.onClick.RemoveAllListeners();
            m_btnReturnToFps.onClick.AddListener(m_cManager.SwitchToFpsScene);
        }

        EnsureCommandCardUi();
        RefreshCommandCardVisuals();
    }

    public override void OnEnter()
    {
        base.OnEnter();
        EnsureCommandCardUi();
        BindCommanderEvents();

        if (PlayModeManager.s_cInstance != null)
        {
            if (PlayModeManager.s_cInstance.SelectionManager != null)
            {
                m_nSelectedUnitCount = PlayModeManager.s_cInstance.SelectionManager.SelectedUnits.Count;
            }
            if (PlayModeManager.s_cInstance.CommandController != null)
            {
                m_ePendingAction = PlayModeManager.s_cInstance.CommandController.PendingTargetCommand;
            }
        }

        RefreshCommandCardVisuals();
    }

    public override void OnExit()
    {
        UnbindCommanderEvents();
        base.OnExit();
    }

    private void OnDestroy()
    {
        UnbindCommanderEvents();
    }

    private void ApplyRtsScenePolicy()
    {
        m_bIsPlayableScene = true;
        m_bAllowsDirectFpsInput = false;
        m_bCanPause = true;
        m_bShowWorldHudBullets = false;
        m_eCursorLockMode = CursorLockMode.None;
        m_bCursorVisible = true;
    }

    public void SetTacticalStatus(string _strStatus)
    {
        if (m_txtTacticalStatus != null)
        {
            m_txtTacticalStatus.text = _strStatus;
        }
    }

    // ==================== 우측 하단 3x3 (9개 슬롯) 스타크래프트 스타일 커맨드 카드 ====================

    private void EnsureCommandCardUi()
    {
        if (m_bCommandCardBuilt) return;
        m_bCommandCardBuilt = true;

        TMP_FontAsset fontShared = m_txtModeHeader != null ? m_txtModeHeader.font : null;

        if (m_rtCommandCardPanel == null)
        {
            Transform trExisting = transform.Find("CommandCardPanel");
            if (trExisting != null)
            {
                m_rtCommandCardPanel = trExisting as RectTransform;
            }
            else
            {
                GameObject objCard = new GameObject("CommandCardPanel");
                objCard.transform.SetParent(transform, false);
                m_rtCommandCardPanel = objCard.AddComponent<RectTransform>();
                m_rtCommandCardPanel.anchorMin = new Vector2(1f, 0f);
                m_rtCommandCardPanel.anchorMax = new Vector2(1f, 0f);
                m_rtCommandCardPanel.pivot = new Vector2(1f, 0f);
                m_rtCommandCardPanel.anchoredPosition = new Vector2(-18f, 18f);
                m_rtCommandCardPanel.sizeDelta = new Vector2(304f, 304f);

                Image imgBg = objCard.AddComponent<Image>();
                imgBg.color = new Color(0.05f, 0.08f, 0.12f, 0.88f);
                imgBg.raycastTarget = true;

                GridLayoutGroup grid = objCard.AddComponent<GridLayoutGroup>();
                grid.cellSize = new Vector2(90f, 90f);
                grid.spacing = new Vector2(7f, 7f);
                grid.padding = new RectOffset(10, 10, 10, 10);
                grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
                grid.constraintCount = 3;
                grid.startCorner = GridLayoutGroup.Corner.UpperLeft;
                grid.startAxis = GridLayoutGroup.Axis.Horizontal;
                grid.childAlignment = TextAnchor.MiddleCenter;
            }
        }

        RtsCommandSlotInfo[] arrSlots = UnitCommandController.s_arrCommanderSlots;
        for (int i = 0; i < COMMAND_SLOT_COUNT && i < arrSlots.Length; i++)
        {
            RtsCommandSlotInfo sSlot = arrSlots[i];
            int nSlotIndex = i;
            ERtsCommanderSlotAction eAction = sSlot.m_eAction;

            GameObject objSlot = new GameObject($"CommandSlot_{i}_{sSlot.m_strTitle}");
            objSlot.transform.SetParent(m_rtCommandCardPanel, false);
            objSlot.AddComponent<RectTransform>();

            Image imgSlotBg = objSlot.AddComponent<Image>();
            imgSlotBg.color = m_colorSlotNormal;
            imgSlotBg.raycastTarget = true;

            Button btnSlot = objSlot.AddComponent<Button>();
            btnSlot.targetGraphic = imgSlotBg;
            btnSlot.onClick.AddListener(() => OnClickCommandSlot(eAction));

            // 슬롯 내부 텍스트 (단축키 + 명령명)
            GameObject objLabel = new GameObject("Label");
            objLabel.transform.SetParent(objSlot.transform, false);
            RectTransform rtLabel = objLabel.AddComponent<RectTransform>();
            rtLabel.anchorMin = Vector2.zero;
            rtLabel.anchorMax = Vector2.one;
            rtLabel.offsetMin = new Vector2(4f, 4f);
            rtLabel.offsetMax = new Vector2(-4f, -4f);

            TextMeshProUGUI txtLabel = objLabel.AddComponent<TextMeshProUGUI>();
            if (fontShared != null)
            {
                txtLabel.font = fontShared;
            }
            txtLabel.alignment = TextAlignmentOptions.Center;
            txtLabel.fontSize = 14f;
            txtLabel.fontStyle = FontStyles.Bold;
            txtLabel.raycastTarget = false;
            txtLabel.text = $"<color=#FFD95A>[{sSlot.m_strHotkeyLabel}]</color>\n{sSlot.m_strTitle}";

            m_arrSlotButtons[nSlotIndex] = btnSlot;
            m_arrSlotBackgrounds[nSlotIndex] = imgSlotBg;
            m_arrSlotLabels[nSlotIndex] = txtLabel;
        }
    }

    private void OnClickCommandSlot(ERtsCommanderSlotAction _eAction)
    {
        if (PlayModeManager.s_cInstance != null && PlayModeManager.s_cInstance.CommandController != null)
        {
            PlayModeManager.s_cInstance.CommandController.TriggerCommanderAction(_eAction);
        }
    }

    private void BindCommanderEvents()
    {
        if (m_bEventsBound || PlayModeManager.s_cInstance == null) return;

        if (PlayModeManager.s_cInstance.SelectionManager != null)
        {
            PlayModeManager.s_cInstance.SelectionManager.OnSelectionCountChanged += HandleSelectionCountChanged;
        }

        if (PlayModeManager.s_cInstance.CommandController != null)
        {
            PlayModeManager.s_cInstance.CommandController.OnPendingTargetCommandChanged += HandlePendingCommandChanged;
        }

        m_bEventsBound = true;
    }

    private void UnbindCommanderEvents()
    {
        if (!m_bEventsBound || PlayModeManager.s_cInstance == null) return;

        if (PlayModeManager.s_cInstance.SelectionManager != null)
        {
            PlayModeManager.s_cInstance.SelectionManager.OnSelectionCountChanged -= HandleSelectionCountChanged;
        }

        if (PlayModeManager.s_cInstance.CommandController != null)
        {
            PlayModeManager.s_cInstance.CommandController.OnPendingTargetCommandChanged -= HandlePendingCommandChanged;
        }

        m_bEventsBound = false;
    }

    private void HandleSelectionCountChanged(int _nSelectedCount)
    {
        m_nSelectedUnitCount = _nSelectedCount;
        RefreshCommandCardVisuals();
    }

    private void HandlePendingCommandChanged(ERtsCommanderSlotAction _ePending)
    {
        m_ePendingAction = _ePending;
        RefreshCommandCardVisuals();
    }

    private void RefreshCommandCardVisuals()
    {
        RtsCommandSlotInfo[] arrSlots = UnitCommandController.s_arrCommanderSlots;
        for (int i = 0; i < COMMAND_SLOT_COUNT && i < arrSlots.Length; i++)
        {
            Button btnSlot = m_arrSlotButtons[i];
            Image imgBg = m_arrSlotBackgrounds[i];
            if (btnSlot == null || imgBg == null) continue;

            RtsCommandSlotInfo sSlot = arrSlots[i];
            bool bCanUse = !sSlot.m_bRequiresSelection || m_nSelectedUnitCount > 0;
            btnSlot.interactable = bCanUse;

            if (m_ePendingAction != ERtsCommanderSlotAction.None && sSlot.m_eAction == m_ePendingAction)
            {
                imgBg.color = m_colorSlotPending;
            }
            else if (!bCanUse)
            {
                imgBg.color = m_colorSlotDisabled;
            }
            else if (!sSlot.m_bRequiresSelection)
            {
                imgBg.color = m_colorSlotUtility;
            }
            else
            {
                imgBg.color = m_colorSlotNormal;
            }
        }
    }
}
