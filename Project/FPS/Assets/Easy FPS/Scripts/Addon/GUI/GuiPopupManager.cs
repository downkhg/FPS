using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 팝업 UI를 프리팹 기반으로 생성·관리하는 팝업 관리자
/// - 기획 의도에 따른 레이어(Active / Menu / Modal / System) 컨테이너를 분리 관리합니다.
/// - SingleReuse(단일 재사용) ↔ PooledMulti(무한 중첩 풀링) 및
///   Overlay / StashAndRestore(상태 저장·숨김 후 복구) / Replace 정책 전환을 지원합니다.
/// - 비활성화 팝업(정지 필요) 유무에 따라 공용 반투명 Dim 배경을 자동 배치·해제합니다.
/// </summary>
public class GuiPopupManager : MonoBehaviour
{
    private class PopupStackEntry
    {
        public IGuiPopup m_cPopup;
        public string m_strPopupName;
        public EPopupLayer m_eLayer;
        public IGuiPopup m_cStashedPopup;
        public bool m_bIsPooledInstance;
    }

    [Header("Popup Root & Prefabs")]
    [Tooltip("팝업 레이어들이 생성될 최상단 루트 Transform (비어있으면 Canvas 하위에 GuiPopupRoot 자동 생성)")]
    [SerializeField]
    private Transform m_trPopupRoot;

    [Header("Popup Policy Settings")]
    [Tooltip("SingleReuse: 프리팹당 1개 재사용 / PooledMulti: 무한 중첩 스택을 위한 오브젝트 풀링")]
    [SerializeField]
    private EPopupInstanceMode m_eInstanceMode = EPopupInstanceMode.SingleReuse;

    [Tooltip("Overlay: 그대로 덮기 / StashAndRestore: 직전 팝업 숨김 후 복구 / Replace: 동일 레이어 교체")]
    [SerializeField]
    private EPopupStackMode m_eDefaultStackMode = EPopupStackMode.Overlay;

    [Header("Dim Overlay Settings")]
    [Tooltip("비활성화 팝업 표시 시 뒤쪽 화면을 반투명으로 덮고 클릭을 차단하는 공용 배경 이미지")]
    [SerializeField]
    private Image m_imgDimOverlay;

    [SerializeField]
    private Color m_colorDim = new Color(0f, 0f, 0f, 0.65f);

    [Tooltip("런타임에 레이어 하위로 생성(Instantiate)할 팝업 프리팹 목록")]
    [SerializeField]
    private List<GuiPopupBase> m_listPopupPrefabs = new List<GuiPopupBase>();

    [Tooltip("생성되어 프라이빗 딕셔너리에 등록/사용될 팝업 객체들을 담는 별도의 뷰 리스트")]
    [SerializeField]
    private List<GuiPopupBase> m_listPopupViews = new List<GuiPopupBase>();

    // 레이어별 컨테이너 Transform 딕셔너리
    private readonly Dictionary<EPopupLayer, Transform> m_dictLayerRoots = new Dictionary<EPopupLayer, Transform>();

    // 오브젝트명(PopupName) -> 원본 프리팹 매핑 (PooledMulti 추가 생성용)
    private readonly Dictionary<string, GuiPopupBase> m_dictPrefabLookup = new Dictionary<string, GuiPopupBase>();

    // 오브젝트명(PopupName)을 키로 하여 기본 팝업 인스턴스에 접근하는 프라이빗 딕셔너리
    private readonly Dictionary<string, IGuiPopup> m_dictGuiPopups = new Dictionary<string, IGuiPopup>();

    // 무한 스택(PooledMulti) 시 추가 생성된 유휴 팝업 인스턴스 풀
    private readonly Dictionary<string, Queue<GuiPopupBase>> m_dictPopupPools = new Dictionary<string, Queue<GuiPopupBase>>();

    // 현재 열려 있는 팝업 스택 (열린 순서대로 Push, 역순으로 Pop/복구)
    private readonly List<PopupStackEntry> m_listPopupStack = new List<PopupStackEntry>();

    private GUIManager m_cGuiManager;
    private bool m_bIsInitialized = false;

    // ==================== 프로퍼티 ====================
    public EPopupInstanceMode InstanceMode
    {
        get => m_eInstanceMode;
        set => m_eInstanceMode = value;
    }

    public EPopupStackMode DefaultStackMode
    {
        get => m_eDefaultStackMode;
        set => m_eDefaultStackMode = value;
    }

    /// <summary>
    /// 화면 및 게임 진행을 정지시키는 비활성화 팝업(BlocksGameplay == true)이 하나라도 열려 있는지 여부
    /// </summary>
    public bool HasBlockingPopupOpen
    {
        get
        {
            for (int i = 0; i < m_listPopupStack.Count; i++)
            {
                IGuiPopup cPopup = m_listPopupStack[i].m_cPopup;
                if (cPopup != null && cPopup.IsActive && cPopup.BlocksGameplay)
                {
                    return true;
                }
            }
            return false;
        }
    }

    public bool HasAnyPopupOpen => m_listPopupStack.Count > 0;
    public int ActivePopupCount => m_listPopupStack.Count;

    // ==================== 초기화 및 레이어 구성 ====================

    /// <summary>
    /// GUIManager에 의해 호출되어 레이어 컨테이너와 팝업 프리팹들을 초기화합니다.
    /// </summary>
    public void Initialize(GUIManager _cGuiManager, Canvas _canvasMain)
    {
        m_cGuiManager = _cGuiManager;
        if (m_bIsInitialized)
        {
            BringPopupRootToFront();
            return;
        }
        m_bIsInitialized = true;

        EnsurePopupRoot(_canvasMain);
        EnsureLayerRoots();
        EnsureDimOverlay();
        InitializeGuiPopups();
        UpdateDimOverlay();
    }

    /// <summary>
    /// 팝업이 GUI 씬 화면 위에 항상 덮어씌워지도록 Canvas 최상단 루트 컨테이너를 확보합니다.
    /// </summary>
    private void EnsurePopupRoot(Canvas _canvasMain)
    {
        if (m_trPopupRoot == null)
        {
            Transform trParent = _canvasMain != null ? _canvasMain.transform : transform;
            Transform trExisting = trParent.Find("GuiPopupRoot");
            if (trExisting != null)
            {
                m_trPopupRoot = trExisting;
            }
            else
            {
                GameObject objRoot = new GameObject("GuiPopupRoot");
                objRoot.transform.SetParent(trParent, false);
                RectTransform rtRoot = objRoot.AddComponent<RectTransform>();
                rtRoot.anchorMin = Vector2.zero;
                rtRoot.anchorMax = Vector2.one;
                rtRoot.offsetMin = Vector2.zero;
                rtRoot.offsetMax = Vector2.zero;
                m_trPopupRoot = rtRoot;
            }
        }

        BringPopupRootToFront();
    }

    /// <summary>
    /// EPopupLayer 순서대로(Active -> Menu -> Modal -> System) 레이어별 컨테이너를 생성 및 정렬합니다.
    /// </summary>
    private void EnsureLayerRoots()
    {
        m_dictLayerRoots.Clear();
        if (m_trPopupRoot == null) return;

        Array arrLayers = Enum.GetValues(typeof(EPopupLayer));
        for (int i = 0; i < arrLayers.Length; i++)
        {
            EPopupLayer eLayer = (EPopupLayer)arrLayers.GetValue(i);
            string strLayerName = eLayer.ToString() + "Layer";

            Transform trLayer = m_trPopupRoot.Find(strLayerName);
            if (trLayer == null)
            {
                GameObject objLayer = new GameObject(strLayerName);
                objLayer.transform.SetParent(m_trPopupRoot, false);
                RectTransform rtLayer = objLayer.AddComponent<RectTransform>();
                rtLayer.anchorMin = Vector2.zero;
                rtLayer.anchorMax = Vector2.one;
                rtLayer.offsetMin = Vector2.zero;
                rtLayer.offsetMax = Vector2.zero;
                trLayer = rtLayer;
            }

            trLayer.SetSiblingIndex(i);
            m_dictLayerRoots[eLayer] = trLayer;
        }
    }

    private Transform GetLayerRoot(EPopupLayer _eLayer)
    {
        if (m_dictLayerRoots.TryGetValue(_eLayer, out Transform trLayer) && trLayer != null)
        {
            return trLayer;
        }
        return m_trPopupRoot;
    }

    /// <summary>
    /// 비활성화 팝업 뒤를 반투명으로 덮는 공용 Dim 이미지를 확보합니다. (기본 비활성화)
    /// </summary>
    private void EnsureDimOverlay()
    {
        if (m_trPopupRoot == null) return;

        if (m_imgDimOverlay == null)
        {
            Transform trExisting = m_trPopupRoot.Find("PopupDimOverlay");
            if (trExisting != null)
            {
                m_imgDimOverlay = trExisting.GetComponent<Image>();
            }
            else
            {
                GameObject objDim = new GameObject("PopupDimOverlay");
                objDim.transform.SetParent(m_trPopupRoot, false);
                RectTransform rtDim = objDim.AddComponent<RectTransform>();
                rtDim.anchorMin = Vector2.zero;
                rtDim.anchorMax = Vector2.one;
                rtDim.offsetMin = Vector2.zero;
                rtDim.offsetMax = Vector2.zero;

                m_imgDimOverlay = objDim.AddComponent<Image>();
            }
        }

        if (m_imgDimOverlay != null)
        {
            m_imgDimOverlay.color = m_colorDim;
            m_imgDimOverlay.raycastTarget = true;
            m_imgDimOverlay.gameObject.SetActive(false);
        }
    }

    /// <summary>
    /// 활성 팝업 종류(비활성화 팝업 vs 활성화 팝업) 및 레이어 우선순위에 따라 반투명 Dim UI를 배치합니다.
    /// 정지가 필요한 비활성화 팝업이 있으면 최상단 레이어/스택의 비활성화 팝업 바로 아래에 반투명 UI를 배치하고,
    /// 정지가 불필요한 활성화 팝업만 있거나 열린 팝업이 없으면 반투명 UI를 아예 사용하지 않습니다.
    /// </summary>
    private void UpdateDimOverlay()
    {
        if (m_imgDimOverlay == null) return;

        PopupStackEntry cTopBlockingEntry = null;
        for (int i = 0; i < m_listPopupStack.Count; i++)
        {
            PopupStackEntry cEntry = m_listPopupStack[i];
            if (cEntry.m_cPopup == null || !cEntry.m_cPopup.IsActive || cEntry.m_cPopup.IsVisuallyHidden)
            {
                continue;
            }

            if (!cEntry.m_cPopup.BlocksGameplay)
            {
                continue;
            }

            // 레이어 우선순위가 높거나, 동일 레이어에서 더 나중에 열린(스택 상단) 팝업을 최상단으로 선택
            if (cTopBlockingEntry == null || cEntry.m_eLayer >= cTopBlockingEntry.m_eLayer)
            {
                cTopBlockingEntry = cEntry;
            }
        }

        MonoBehaviour cTopBlockingMono = cTopBlockingEntry != null ? cTopBlockingEntry.m_cPopup as MonoBehaviour : null;
        if (cTopBlockingMono != null)
        {
            Transform trTargetLayer = GetLayerRoot(cTopBlockingEntry.m_eLayer);
            if (trTargetLayer != null && m_imgDimOverlay.transform.parent != trTargetLayer)
            {
                m_imgDimOverlay.transform.SetParent(trTargetLayer, false);
            }

            m_imgDimOverlay.color = m_colorDim;
            m_imgDimOverlay.gameObject.SetActive(true);
            m_imgDimOverlay.transform.SetAsLastSibling();
            int nTargetIndex = cTopBlockingMono.transform.GetSiblingIndex();
            m_imgDimOverlay.transform.SetSiblingIndex(nTargetIndex);
        }
        else
        {
            m_imgDimOverlay.gameObject.SetActive(false);
        }
    }

    /// <summary>
    /// 팝업 루트를 부모(Canvas)의 마지막 자식으로 유지하여 밑에 있는 GUI 씬 화면을 항상 덮도록 보장합니다.
    /// </summary>
    public void BringPopupRootToFront()
    {
        if (m_trPopupRoot != null)
        {
            m_trPopupRoot.SetAsLastSibling();
        }
    }

    /// <summary>
    /// 등록된 팝업 프리팹들을 소속 레이어 하위에 생성하여 뷰 리스트(m_listPopupViews)와 프라이빗 딕셔너리(m_dictGuiPopups)에 등록합니다.
    /// </summary>
    private void InitializeGuiPopups()
    {
        m_dictGuiPopups.Clear();
        m_dictPrefabLookup.Clear();
        m_dictPopupPools.Clear();
        m_listPopupStack.Clear();

        if (m_listPopupPrefabs == null)
        {
            m_listPopupPrefabs = new List<GuiPopupBase>();
        }

        if (m_listPopupViews == null)
        {
            m_listPopupViews = new List<GuiPopupBase>();
        }
        else
        {
            m_listPopupViews.RemoveAll(cView => cView == null);
        }

        // 1. 이미 뷰 리스트에 할당된 씬 내 인스턴스가 있다면 우선 등록 및 소속 레이어로 정렬
        for (int i = 0; i < m_listPopupViews.Count; i++)
        {
            GuiPopupBase cExistingView = m_listPopupViews[i];
            if (cExistingView != null && cExistingView.gameObject.scene.IsValid())
            {
                RegisterViewToDictionary(cExistingView);
                AttachToLayerRoot(cExistingView);
                cExistingView.OnClose();
            }
        }

        // 2. 프리팹 리스트를 순회하며 원본 프리팹 매핑 및 기본 인스턴스 생성
        for (int i = 0; i < m_listPopupPrefabs.Count; i++)
        {
            GuiPopupBase prefabPopup = m_listPopupPrefabs[i];
            if (prefabPopup == null) continue;

            string strPrefabName = prefabPopup.gameObject.name;
            m_dictPrefabLookup[strPrefabName] = prefabPopup;

            if (m_dictGuiPopups.ContainsKey(strPrefabName)) continue;

            Transform trLayerRoot = GetLayerRoot(prefabPopup.Layer);
            Transform trExistingChild = trLayerRoot != null ? trLayerRoot.Find(strPrefabName) : null;
            GuiPopupBase cViewInstance = null;

            if (trExistingChild != null)
            {
                cViewInstance = trExistingChild.GetComponent<GuiPopupBase>();
            }

            if (cViewInstance == null)
            {
                cViewInstance = Instantiate(prefabPopup, trLayerRoot);
                cViewInstance.gameObject.name = strPrefabName;
            }

            AddGuiPopupView(cViewInstance);
            AttachToLayerRoot(cViewInstance);
            cViewInstance.OnClose();
        }
    }

    private void AttachToLayerRoot(GuiPopupBase _cPopupView)
    {
        if (_cPopupView == null) return;

        Transform trLayerRoot = GetLayerRoot(_cPopupView.Layer);
        if (trLayerRoot != null && _cPopupView.transform.parent != trLayerRoot)
        {
            _cPopupView.transform.SetParent(trLayerRoot, false);
        }
    }

    public void AddGuiPopupView(GuiPopupBase _cPopupView)
    {
        if (_cPopupView == null) return;

        if (!m_listPopupViews.Contains(_cPopupView))
        {
            m_listPopupViews.Add(_cPopupView);
        }

        RegisterViewToDictionary(_cPopupView);
    }

    private void RegisterViewToDictionary(GuiPopupBase _cPopupView)
    {
        if (_cPopupView == null) return;

        string strKey = _cPopupView.PopupName;
        m_dictGuiPopups[strKey] = _cPopupView;
        _cPopupView.Initialize(m_cGuiManager, this);
    }

    // ==================== 조회 유틸리티 ====================

    /// <summary>
    /// 등록된 팝업 프리팹 목록에서 특정 타입의 프리팹 오브젝트명을 동적으로 취득합니다.
    /// </summary>
    public string GetPrefabPopupName<T>() where T : GuiPopupBase
    {
        if (m_listPopupPrefabs != null)
        {
            for (int i = 0; i < m_listPopupPrefabs.Count; i++)
            {
                if (m_listPopupPrefabs[i] is T cPrefab && cPrefab != null)
                {
                    return cPrefab.gameObject.name;
                }
            }
        }
        return string.Empty;
    }

    /// <summary>
    /// 활성 스택(최상단 우선) 또는 프라이빗 딕셔너리에서 타입(T) 기반으로 팝업을 조회합니다.
    /// </summary>
    public T GetPopup<T>() where T : class, IGuiPopup
    {
        for (int i = m_listPopupStack.Count - 1; i >= 0; i--)
        {
            if (m_listPopupStack[i].m_cPopup is T cActivePopup)
            {
                return cActivePopup;
            }
        }

        foreach (KeyValuePair<string, IGuiPopup> kvp in m_dictGuiPopups)
        {
            if (kvp.Value is T cTargetPopup)
            {
                return cTargetPopup;
            }
        }
        return null;
    }

    /// <summary>
    /// 프라이빗 딕셔너리에서 오브젝트명을 통해 특정 타입의 팝업을 조회합니다.
    /// </summary>
    public T GetPopup<T>(string _strPopupName) where T : class, IGuiPopup
    {
        if (string.IsNullOrEmpty(_strPopupName)) return null;

        if (m_dictGuiPopups.TryGetValue(_strPopupName, out IGuiPopup cPopup))
        {
            return cPopup as T;
        }
        return null;
    }

    // ==================== 팝업 열기 / 스택 저장 / 풀링 ====================

    /// <summary>
    /// 타입에 해당하는 팝업을 지정한 스택 정책(생략 시 기본 정책)으로 엽니다.
    /// </summary>
    public T OpenPopup<T>(EPopupStackMode? _eStackModeOverride = null) where T : GuiPopupBase
    {
        string strPopupName = GetPrefabPopupName<T>();
        if (!string.IsNullOrEmpty(strPopupName))
        {
            return OpenPopup(strPopupName, _eStackModeOverride) as T;
        }

        T cPopup = GetPopup<T>();
        if (cPopup != null)
        {
            OpenPopupInstance(cPopup, cPopup.PopupName, false, _eStackModeOverride ?? m_eDefaultStackMode);
        }
        return cPopup;
    }

    /// <summary>
    /// 오브젝트명 키를 통해 팝업을 엽니다.
    /// PooledMulti 모드에서 동일 팝업이 이미 열려 있으면 풀에서 가져오거나 새로 Instantiate하여 무한 스택을 지원합니다.
    /// </summary>
    public IGuiPopup OpenPopup(string _strPopupName, EPopupStackMode? _eStackModeOverride = null)
    {
        if (string.IsNullOrEmpty(_strPopupName)) return null;

        EPopupStackMode eStackMode = _eStackModeOverride ?? m_eDefaultStackMode;
        bool bIsPooledInstance = false;
        IGuiPopup cPopup = AcquirePopupInstance(_strPopupName, out bIsPooledInstance);

        if (cPopup != null)
        {
            OpenPopupInstance(cPopup, _strPopupName, bIsPooledInstance, eStackMode);
            return cPopup;
        }
        return null;
    }

    private IGuiPopup AcquirePopupInstance(string _strPopupName, out bool _bIsPooledInstance)
    {
        _bIsPooledInstance = false;

        if (m_dictGuiPopups.TryGetValue(_strPopupName, out IGuiPopup cBasePopup) && cBasePopup != null)
        {
            // SingleReuse 모드이거나 기본 인스턴스가 현재 미사용 상태라면 기본 인스턴스 사용
            if (m_eInstanceMode == EPopupInstanceMode.SingleReuse || !IsPopupInStack(cBasePopup))
            {
                return cBasePopup;
            }
        }

        // PooledMulti 모드: 동일 팝업이 이미 스택에 올라가 있을 때 풀에서 꺼내거나 새로 생성
        if (m_eInstanceMode == EPopupInstanceMode.PooledMulti)
        {
            if (m_dictPopupPools.TryGetValue(_strPopupName, out Queue<GuiPopupBase> qPool))
            {
                while (qPool.Count > 0)
                {
                    GuiPopupBase cPooled = qPool.Dequeue();
                    if (cPooled != null)
                    {
                        cPooled.OnResetForPool();
                        _bIsPooledInstance = true;
                        return cPooled;
                    }
                }
            }

            if (m_dictPrefabLookup.TryGetValue(_strPopupName, out GuiPopupBase prefabPopup) && prefabPopup != null)
            {
                Transform trLayerRoot = GetLayerRoot(prefabPopup.Layer);
                GuiPopupBase cExtraInstance = Instantiate(prefabPopup, trLayerRoot);
                cExtraInstance.gameObject.name = _strPopupName;
                cExtraInstance.Initialize(m_cGuiManager, this);
                AttachToLayerRoot(cExtraInstance);
                _bIsPooledInstance = true;
                return cExtraInstance;
            }
        }

        return cBasePopup;
    }

    private bool IsPopupInStack(IGuiPopup _cPopup)
    {
        for (int i = 0; i < m_listPopupStack.Count; i++)
        {
            if (m_listPopupStack[i].m_cPopup == _cPopup)
            {
                return true;
            }
        }
        return false;
    }

    private void OpenPopupInstance(IGuiPopup _cPopup, string _strPopupName, bool _bIsPooledInstance, EPopupStackMode _eStackMode)
    {
        if (_cPopup == null) return;

        BringPopupRootToFront();

        if (_cPopup is GuiPopupBase cBasePopup)
        {
            AttachToLayerRoot(cBasePopup);
        }

        // Replace 모드: 동일 레이어의 기존 활성 팝업을 먼저 닫고 교체
        if (_eStackMode == EPopupStackMode.Replace)
        {
            ClosePopupsInLayerInternal(_cPopup.Layer, _cPopup);
        }

        // SingleReuse에서 이미 스택에 있는 동일 인스턴스를 재오픈하는 경우 기존 엔트리 제거
        for (int i = m_listPopupStack.Count - 1; i >= 0; i--)
        {
            if (m_listPopupStack[i].m_cPopup == _cPopup)
            {
                m_listPopupStack.RemoveAt(i);
            }
        }

        // StashAndRestore 모드: 직전에 열려 있던 비활성화 팝업을 데이터 보존 상태로 숨김
        IGuiPopup cStashedPopup = null;
        if (_eStackMode == EPopupStackMode.StashAndRestore && _cPopup.BlocksGameplay)
        {
            for (int i = m_listPopupStack.Count - 1; i >= 0; i--)
            {
                IGuiPopup cPrevPopup = m_listPopupStack[i].m_cPopup;
                if (cPrevPopup != null && cPrevPopup.IsActive && !cPrevPopup.IsVisuallyHidden && cPrevPopup.BlocksGameplay)
                {
                    cPrevPopup.SetVisualVisible(false);
                    cStashedPopup = cPrevPopup;
                    break;
                }
            }
        }

        PopupStackEntry cEntry = new PopupStackEntry
        {
            m_cPopup = _cPopup,
            m_strPopupName = _strPopupName,
            m_eLayer = _cPopup.Layer,
            m_cStashedPopup = cStashedPopup,
            m_bIsPooledInstance = _bIsPooledInstance
        };
        m_listPopupStack.Add(cEntry);

        _cPopup.OnOpen();
        UpdateDimOverlay();

        if (m_cGuiManager != null)
        {
            m_cGuiManager.OnPopupStateChanged();
        }
    }

    // ==================== 팝업 닫기 / 스택 복구 / 풀 반납 ====================

    /// <summary>
    /// 특정 타입의 가장 상단 팝업을 닫습니다.
    /// </summary>
    public void ClosePopup<T>() where T : class, IGuiPopup
    {
        for (int i = m_listPopupStack.Count - 1; i >= 0; i--)
        {
            if (m_listPopupStack[i].m_cPopup is T)
            {
                CloseStackEntryAt(i);
                NotifyPopupStateChanged();
                return;
            }
        }
    }

    /// <summary>
    /// 지정한 팝업 인스턴스를 닫고, Stash된 직전 팝업이 있었다면 복구합니다.
    /// </summary>
    public void ClosePopup(IGuiPopup _cPopup)
    {
        if (_cPopup == null) return;

        for (int i = m_listPopupStack.Count - 1; i >= 0; i--)
        {
            if (m_listPopupStack[i].m_cPopup == _cPopup)
            {
                CloseStackEntryAt(i);
                NotifyPopupStateChanged();
                return;
            }
        }

        _cPopup.OnClose();
        NotifyPopupStateChanged();
    }

    /// <summary>
    /// 가장 높은 레이어/스택 상단에 열려 있는 비활성화 팝업(BlocksGameplay == true) 하나를 닫고 직전 팝업을 복구합니다.
    /// </summary>
    public bool CloseTopBlockingPopup()
    {
        int nTopIndex = -1;
        EPopupLayer eHighestLayer = EPopupLayer.Active;

        for (int i = 0; i < m_listPopupStack.Count; i++)
        {
            PopupStackEntry cEntry = m_listPopupStack[i];
            if (cEntry.m_cPopup != null && cEntry.m_cPopup.IsActive && cEntry.m_cPopup.BlocksGameplay)
            {
                if (nTopIndex < 0 || cEntry.m_eLayer >= eHighestLayer)
                {
                    nTopIndex = i;
                    eHighestLayer = cEntry.m_eLayer;
                }
            }
        }

        if (nTopIndex >= 0)
        {
            CloseStackEntryAt(nTopIndex);
            NotifyPopupStateChanged();
            return true;
        }
        return false;
    }

    /// <summary>
    /// 특정 레이어에 속한 모든 활성 팝업을 닫습니다.
    /// </summary>
    public void ClosePopupsInLayer(EPopupLayer _eLayer)
    {
        ClosePopupsInLayerInternal(_eLayer, null);
        NotifyPopupStateChanged();
    }

    private void ClosePopupsInLayerInternal(EPopupLayer _eLayer, IGuiPopup _cExceptPopup)
    {
        for (int i = m_listPopupStack.Count - 1; i >= 0; i--)
        {
            PopupStackEntry cEntry = m_listPopupStack[i];
            if (cEntry.m_eLayer == _eLayer && cEntry.m_cPopup != _cExceptPopup)
            {
                CloseStackEntryAt(i);
            }
        }
    }

    /// <summary>
    /// 열려 있는 모든 팝업을 닫고 풀링된 인스턴스를 반납합니다.
    /// </summary>
    public void CloseAllPopups()
    {
        for (int i = m_listPopupStack.Count - 1; i >= 0; i--)
        {
            CloseStackEntryAt(i, false);
        }
        m_listPopupStack.Clear();
        NotifyPopupStateChanged();
    }

    private void CloseStackEntryAt(int _nIndex, bool _bRestoreStashed = true)
    {
        if (_nIndex < 0 || _nIndex >= m_listPopupStack.Count) return;

        PopupStackEntry cEntry = m_listPopupStack[_nIndex];
        m_listPopupStack.RemoveAt(_nIndex);

        if (cEntry.m_cPopup != null)
        {
            cEntry.m_cPopup.OnClose();

            // PooledMulti 추가 인스턴스는 풀로 반납하여 재사용
            if (cEntry.m_bIsPooledInstance && cEntry.m_cPopup is GuiPopupBase cPooledPopup)
            {
                cPooledPopup.OnResetForPool();
                if (!m_dictPopupPools.TryGetValue(cEntry.m_strPopupName, out Queue<GuiPopupBase> qPool))
                {
                    qPool = new Queue<GuiPopupBase>();
                    m_dictPopupPools[cEntry.m_strPopupName] = qPool;
                }
                qPool.Enqueue(cPooledPopup);
            }
        }

        // StashAndRestore로 숨겨져 있던 직전 팝업 복구
        if (_bRestoreStashed && cEntry.m_cStashedPopup != null && cEntry.m_cStashedPopup.IsActive)
        {
            cEntry.m_cStashedPopup.SetVisualVisible(true);
        }
    }

    private void NotifyPopupStateChanged()
    {
        UpdateDimOverlay();

        if (m_cGuiManager != null)
        {
            m_cGuiManager.OnPopupStateChanged();
        }
    }

    /// <summary>
    /// 등록된 팝업 및 풀링으로 열린 활성 팝업 중 IAudioSyncableGui 구현체에 오디오 상태를 일괄 동기화합니다.
    /// </summary>
    public void SyncAudioPopups(float _fBgmVolume, float _fSfxVolume, bool _bIsMuted)
    {
        foreach (KeyValuePair<string, IGuiPopup> kvp in m_dictGuiPopups)
        {
            if (kvp.Value is IAudioSyncableGui cAudioSyncable)
            {
                cAudioSyncable.SyncAudioSettings(_fBgmVolume, _fSfxVolume, _bIsMuted);
            }
        }

        for (int i = 0; i < m_listPopupStack.Count; i++)
        {
            if (m_listPopupStack[i].m_bIsPooledInstance && m_listPopupStack[i].m_cPopup is IAudioSyncableGui cPooledAudio)
            {
                cPooledAudio.SyncAudioSettings(_fBgmVolume, _fSfxVolume, _bIsMuted);
            }
        }
    }
}
