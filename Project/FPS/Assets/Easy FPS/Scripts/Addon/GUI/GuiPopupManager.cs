using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 팝업 UI를 프리팹 기반으로 생성·관리하는 팝업 관리자
/// GUIManager가 소유하여 초기화/제어하며, 현재 GUI 씬 화면을 끄지 않고 위 레이어에 덮어씌워 표시합니다.
/// 비활성화 팝업(정지 필요)이 열려 있을 때만 공용 반투명 Dim 배경을 활성화하고,
/// 정지가 불필요한 활성화 팝업만 열려 있을 때는 반투명 Dim 배경을 사용하지 않습니다.
/// </summary>
public class GuiPopupManager : MonoBehaviour
{
    [Header("Popup Root & Prefabs")]
    [Tooltip("팝업들이 생성될 최상단 루트 Transform (비어있으면 Canvas 하위에 GuiPopupRoot 자동 생성)")]
    [SerializeField]
    private Transform m_trPopupRoot;

    [Header("Dim Overlay Settings")]
    [Tooltip("비활성화 팝업 표시 시 뒤쪽 화면을 반투명으로 덮고 클릭을 차단하는 공용 배경 이미지")]
    [SerializeField]
    private Image m_imgDimOverlay;

    [SerializeField]
    private Color m_colorDim = new Color(0f, 0f, 0f, 0.65f);

    [Tooltip("런타임에 GuiPopupRoot 하위로 생성(Instantiate)할 팝업 프리팹 목록")]
    [SerializeField]
    private List<GuiPopupBase> m_listPopupPrefabs = new List<GuiPopupBase>();

    [Tooltip("생성되어 프라이빗 딕셔너리에 등록/사용될 팝업 객체들을 담는 별도의 뷰 리스트")]
    [SerializeField]
    private List<GuiPopupBase> m_listPopupViews = new List<GuiPopupBase>();

    // 오브젝트명(PopupName)을 키로 하여 모든 런타임 접근을 수행하는 프라이빗 딕셔너리
    private readonly Dictionary<string, IGuiPopup> m_dictGuiPopups = new Dictionary<string, IGuiPopup>();

    // 현재 열려 있는 팝업 목록 (열린 순서 유지)
    private readonly List<IGuiPopup> m_listActivePopups = new List<IGuiPopup>();

    private GUIManager m_cGuiManager;
    private bool m_bIsInitialized = false;

    /// <summary>
    /// 화면 및 게임 진행을 정지시키는 비활성화 팝업(BlocksGameplay == true)이 하나라도 열려 있는지 여부
    /// </summary>
    public bool HasBlockingPopupOpen
    {
        get
        {
            for (int i = 0; i < m_listActivePopups.Count; i++)
            {
                IGuiPopup cPopup = m_listActivePopups[i];
                if (cPopup != null && cPopup.IsActive && cPopup.BlocksGameplay)
                {
                    return true;
                }
            }
            return false;
        }
    }

    public bool HasAnyPopupOpen => m_listActivePopups.Count > 0;

    /// <summary>
    /// GUIManager에 의해 호출되어 팝업 프리팹들을 인스턴스화하고 딕셔너리에 등록합니다.
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
        EnsureDimOverlay();
        InitializeGuiPopups();
        UpdateDimOverlay();
    }

    /// <summary>
    /// 팝업이 GUI 씬 화면 위에 항상 덮어씌워지도록 Canvas 최하단(가장 나중에 렌더되는) 루트 컨테이너를 확보합니다.
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
    /// 활성 팝업 종류(비활성화 팝업 vs 활성화 팝업)에 따라 반투명 Dim UI 사용 여부를 결정합니다.
    /// 정지가 필요한 비활성화 팝업이 있으면 최상단 비활성화 팝업 바로 아래에 반투명 UI를 배치해 사용하고,
    /// 정지가 불필요한 활성화 팝업만 있거나 열린 팝업이 없으면 반투명 UI를 아예 사용하지 않습니다.
    /// </summary>
    private void UpdateDimOverlay()
    {
        if (m_imgDimOverlay == null) return;

        MonoBehaviour cTopBlockingMono = null;
        for (int i = m_listActivePopups.Count - 1; i >= 0; i--)
        {
            IGuiPopup cPopup = m_listActivePopups[i];
            if (cPopup != null && cPopup.IsActive && cPopup.BlocksGameplay)
            {
                cTopBlockingMono = cPopup as MonoBehaviour;
                break;
            }
        }

        if (cTopBlockingMono != null)
        {
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
    /// 등록된 팝업 프리팹들을 생성하여 뷰 리스트(m_listPopupViews)와 프라이빗 딕셔너리(m_dictGuiPopups)에 등록합니다.
    /// </summary>
    private void InitializeGuiPopups()
    {
        m_dictGuiPopups.Clear();
        m_listActivePopups.Clear();

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

        // 1. 이미 뷰 리스트에 할당된 씬 내 인스턴스가 있다면 우선 등록
        for (int i = 0; i < m_listPopupViews.Count; i++)
        {
            GuiPopupBase cExistingView = m_listPopupViews[i];
            if (cExistingView != null && cExistingView.gameObject.scene.IsValid())
            {
                RegisterViewToDictionary(cExistingView);
                cExistingView.OnClose();
            }
        }

        // 2. 프리팹 리스트를 순회하며 인스턴스 생성 후 뷰 리스트 추가 및 프라이빗 딕셔너리 등록
        for (int i = 0; i < m_listPopupPrefabs.Count; i++)
        {
            GuiPopupBase prefabPopup = m_listPopupPrefabs[i];
            if (prefabPopup == null) continue;

            string strPrefabName = prefabPopup.gameObject.name;

            if (m_dictGuiPopups.ContainsKey(strPrefabName)) continue;

            Transform trExistingChild = m_trPopupRoot != null ? m_trPopupRoot.Find(strPrefabName) : null;
            GuiPopupBase cViewInstance = null;

            if (trExistingChild != null)
            {
                cViewInstance = trExistingChild.GetComponent<GuiPopupBase>();
            }

            if (cViewInstance == null)
            {
                cViewInstance = Instantiate(prefabPopup, m_trPopupRoot);
                cViewInstance.gameObject.name = strPrefabName;
            }

            AddGuiPopupView(cViewInstance);
            cViewInstance.OnClose();
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
    /// 프라이빗 딕셔너리에서 타입(T) 기반으로 팝업 인터페이스를 조회합니다.
    /// </summary>
    public T GetPopup<T>() where T : class, IGuiPopup
    {
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

    /// <summary>
    /// 타입에 해당하는 팝업을 엽니다.
    /// 하위 GUI 씬 화면을 끄지 않고 그 위에 덮어씌우며, 비활성화 팝업일 경우 화면 정지 및 커서를 활성화합니다.
    /// </summary>
    public T OpenPopup<T>() where T : GuiPopupBase
    {
        string strPopupName = GetPrefabPopupName<T>();
        if (!string.IsNullOrEmpty(strPopupName))
        {
            return OpenPopup(strPopupName) as T;
        }

        T cPopup = GetPopup<T>();
        if (cPopup != null)
        {
            OpenPopupInstance(cPopup);
        }
        return cPopup;
    }

    /// <summary>
    /// 오브젝트명 키를 통해 팝업을 엽니다.
    /// </summary>
    public IGuiPopup OpenPopup(string _strPopupName)
    {
        if (string.IsNullOrEmpty(_strPopupName)) return null;

        if (m_dictGuiPopups.TryGetValue(_strPopupName, out IGuiPopup cPopup) && cPopup != null)
        {
            OpenPopupInstance(cPopup);
            return cPopup;
        }
        return null;
    }

    private void OpenPopupInstance(IGuiPopup _cPopup)
    {
        if (_cPopup == null) return;

        BringPopupRootToFront();

        if (m_listActivePopups.Contains(_cPopup))
        {
            m_listActivePopups.Remove(_cPopup);
        }
        m_listActivePopups.Add(_cPopup);

        _cPopup.OnOpen();
        UpdateDimOverlay();

        if (m_cGuiManager != null)
        {
            m_cGuiManager.OnPopupStateChanged();
        }
    }

    /// <summary>
    /// 특정 타입의 팝업을 닫습니다.
    /// </summary>
    public void ClosePopup<T>() where T : class, IGuiPopup
    {
        T cPopup = GetPopup<T>();
        if (cPopup != null)
        {
            ClosePopup(cPopup);
        }
    }

    /// <summary>
    /// 지정한 팝업 인스턴스를 닫고, 남은 팝업 상태에 맞춰 게임플레이 상태를 복구합니다.
    /// </summary>
    public void ClosePopup(IGuiPopup _cPopup)
    {
        if (_cPopup == null) return;

        m_listActivePopups.Remove(_cPopup);
        _cPopup.OnClose();
        UpdateDimOverlay();

        if (m_cGuiManager != null)
        {
            m_cGuiManager.OnPopupStateChanged();
        }
    }

    /// <summary>
    /// 가장 위에 열려 있는 비활성화 팝업(BlocksGameplay == true) 하나를 닫습니다.
    /// </summary>
    public bool CloseTopBlockingPopup()
    {
        for (int i = m_listActivePopups.Count - 1; i >= 0; i--)
        {
            IGuiPopup cPopup = m_listActivePopups[i];
            if (cPopup != null && cPopup.IsActive && cPopup.BlocksGameplay)
            {
                ClosePopup(cPopup);
                return true;
            }
        }
        return false;
    }

    /// <summary>
    /// 열려 있는 모든 팝업을 닫습니다.
    /// </summary>
    public void CloseAllPopups()
    {
        for (int i = m_listActivePopups.Count - 1; i >= 0; i--)
        {
            IGuiPopup cPopup = m_listActivePopups[i];
            if (cPopup != null)
            {
                cPopup.OnClose();
            }
        }
        m_listActivePopups.Clear();
        UpdateDimOverlay();

        if (m_cGuiManager != null)
        {
            m_cGuiManager.OnPopupStateChanged();
        }
    }

    /// <summary>
    /// 프라이빗 딕셔너리에 등록된 팝업 중 IAudioSyncableGui 구현체에 오디오 상태를 일괄 동기화합니다.
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
    }
}
