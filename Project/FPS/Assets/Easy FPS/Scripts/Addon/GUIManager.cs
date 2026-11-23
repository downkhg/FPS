using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GUIManager : MonoBehaviour
{
    public static GUIManager s_cInstance;

    // 씬 리로드 시 타이틀을 건너뛰고 바로 인게임으로 진입할지 여부 (Restart 기능용)
    private static bool s_bStartInGameOnReload = false;

    [Header("Runtime State")]
    [SerializeField]
    private string m_strCurrentSceneName = string.Empty;

    [Header("GUI Scene Prefabs & View List")]
    [SerializeField]
    private Canvas m_canvasMain;

    [Tooltip("런타임에 Canvas 하위로 생성(Instantiate)할 각 GUI 씬 프리팹 목록")]
    [SerializeField]
    private List<GuiSceneBase> m_listScenePrefabs = new List<GuiSceneBase>();

    [Tooltip("생성되어 프라이빗 딕셔너리에 등록/사용될 GUI 씬 객체들을 담는 별도의 뷰 리스트")]
    [SerializeField]
    private List<GuiSceneBase> m_listSceneViews = new List<GuiSceneBase>();

    // 오브젝트명(SceneName)을 키로 하여 모든 런타임 접근을 수행하는 프라이빗 딕셔너리
    private readonly Dictionary<string, IGuiScene> m_dictGuiScenes = new Dictionary<string, IGuiScene>();

    [Header("GUI Popup Manager")]
    [SerializeField]
    private GuiPopupManager m_cPopupManager;

    [Header("Audio System Settings")]
    [SerializeField]
    private AudioSource m_asBgm;

    [SerializeField]
    private AudioSource m_asUiClick;

    [SerializeField, Range(0f, 1f)]
    private float m_fBgmVolume = 0.5f;

    [SerializeField, Range(0f, 1f)]
    private float m_fSfxVolume = 0.8f;

    [SerializeField]
    private bool m_bIsMuted = false;

    private readonly Dictionary<AudioSource, float> m_dictSfxBaseVolumes = new Dictionary<AudioSource, float>();
    private bool m_bIsInitialized = false;

    private const string PREF_BGM_VOL = "FPS_BGM_VOLUME";
    private const string PREF_SFX_VOL = "FPS_SFX_VOLUME";
    private const string PREF_MUTE = "FPS_AUDIO_MUTE";

    // ==================== 프리팹 배열 기반 씬 이름 및 객체 접근 프로퍼티 ====================
    /// <summary>
    /// 등록된 프리팹 배열/리스트에서 특정 타입의 프리팹 오브젝트명을 동적으로 취득합니다.
    /// </summary>
    public string GetPrefabSceneName<T>() where T : GuiSceneBase
    {
        if (m_listScenePrefabs != null)
        {
            for (int i = 0; i < m_listScenePrefabs.Count; i++)
            {
                if (m_listScenePrefabs[i] is T cPrefab && cPrefab != null)
                {
                    return cPrefab.gameObject.name;
                }
            }
        }
        return string.Empty;
    }

    public string TitleSceneName => GetPrefabSceneName<TitleGuiScene>();
    public string InGameSceneName => GetPrefabSceneName<InGameGuiScene>();
    public string GameOverSceneName => GetPrefabSceneName<GameOverGuiScene>();

    // ==================== 프로퍼티 ====================
    public string CurrentSceneName => m_strCurrentSceneName;
    public bool HasBlockingPopupOpen => m_cPopupManager != null && m_cPopupManager.HasBlockingPopupOpen;
    public bool IsGameplayActive => !string.IsNullOrEmpty(m_strCurrentSceneName) && m_strCurrentSceneName == InGameSceneName && !HasBlockingPopupOpen;
    public GuiPopupManager PopupManager => m_cPopupManager;
    public float BgmVolume => m_fBgmVolume;
    public float SfxVolume => m_fSfxVolume;
    public bool IsMuted => m_bIsMuted;

    public static bool CanProcessGameplayInput()
    {
        return s_cInstance == null || s_cInstance.IsGameplayActive;
    }

    private void Awake()
    {
        if (s_cInstance != null && s_cInstance != this)
        {
            Destroy(this);
            return;
        }
        s_cInstance = this;
    }

    private void Start()
    {
        if (!m_bIsInitialized)
        {
            Initialize();
        }
    }

    public void Initialize()
    {
        if (m_bIsInitialized) return;
        m_bIsInitialized = true;

        s_cInstance = this;

        LoadAudioPreferences();
        EnsureAudioSources();
        InitializeGuiScenes();
        InitializePopupManager();
        RefreshSfxSources();
        SyncAudioUiControls();

        if (s_bStartInGameOnReload)
        {
            s_bStartInGameOnReload = false;
            SetGuiScene(InGameSceneName);
        }
        else
        {
            SetGuiScene(TitleSceneName);
        }
    }

    /// <summary>
    /// 등록된 프리팹들을 생성하여 별도의 뷰 리스트(m_listSceneViews)에 추가하고,
    /// 오브젝트명을 키로 프라이빗 딕셔너리(m_dictGuiScenes)에 등록합니다.
    /// </summary>
    private void InitializeGuiScenes()
    {
        m_dictGuiScenes.Clear();

        if (m_listScenePrefabs == null)
        {
            m_listScenePrefabs = new List<GuiSceneBase>();
        }

        if (m_listSceneViews == null)
        {
            m_listSceneViews = new List<GuiSceneBase>();
        }
        else
        {
            m_listSceneViews.RemoveAll(cView => cView == null);
        }

        if (m_canvasMain == null)
        {
            m_canvasMain = FindFirstObjectByType<Canvas>();
        }

        Transform trCanvas = m_canvasMain != null ? m_canvasMain.transform : transform;

        // 1. 이미 뷰 리스트에 할당된 씬 내 인스턴스가 있다면 우선 등록
        for (int i = 0; i < m_listSceneViews.Count; i++)
        {
            GuiSceneBase cExistingView = m_listSceneViews[i];
            if (cExistingView != null && cExistingView.gameObject.scene.IsValid())
            {
                RegisterViewToDictionary(cExistingView);
            }
        }

        // 2. 프리팹 리스트를 순회하며 인스턴스 생성 후 뷰 리스트 추가 및 프라이빗 딕셔너리 등록
        for (int i = 0; i < m_listScenePrefabs.Count; i++)
        {
            GuiSceneBase prefabScene = m_listScenePrefabs[i];
            if (prefabScene == null) continue;

            string strPrefabName = prefabScene.gameObject.name;

            // 이미 동일 오브젝트명으로 프라이빗 딕셔너리에 등록되어 있다면 중복 생성 건너뜀
            if (m_dictGuiScenes.ContainsKey(strPrefabName)) continue;

            // Canvas 하위에 이미 같은 이름의 오브젝트가 존재하는지 확인
            Transform trExistingChild = trCanvas.Find(strPrefabName);
            GuiSceneBase cViewInstance = null;

            if (trExistingChild != null)
            {
                cViewInstance = trExistingChild.GetComponent<GuiSceneBase>();
            }

            if (cViewInstance == null)
            {
                cViewInstance = Instantiate(prefabScene, trCanvas);
                cViewInstance.gameObject.name = strPrefabName;
            }

            AddGuiSceneView(cViewInstance);
        }
    }

    /// <summary>
    /// 생성된 GUI 씬 객체를 별도의 뷰 리스트(m_listSceneViews)에 추가하고 프라이빗 딕셔너리에 등록합니다.
    /// </summary>
    public void AddGuiSceneView(GuiSceneBase _cSceneView)
    {
        if (_cSceneView == null) return;

        if (!m_listSceneViews.Contains(_cSceneView))
        {
            m_listSceneViews.Add(_cSceneView);
        }

        RegisterViewToDictionary(_cSceneView);
    }

    /// <summary>
    /// 뷰 객체를 오브젝트명(SceneName) 기반 프라이빗 딕셔너리에 등록하고 컨텍스트(this)로 초기화합니다.
    /// </summary>
    private void RegisterViewToDictionary(GuiSceneBase _cSceneView)
    {
        if (_cSceneView == null) return;

        string strKey = _cSceneView.SceneName;
        m_dictGuiScenes[strKey] = _cSceneView;
        _cSceneView.Initialize(this);
    }

    /// <summary>
    /// 프라이빗 딕셔너리에서 타입(T)을 기반으로 GUI 씬 인터페이스를 조회합니다.
    /// 프리팹 목록에서 해당 타입의 이름을 가져와 프라이빗 딕셔너리에서 조회합니다.
    /// </summary>
    public T GetGuiScene<T>() where T : class, IGuiScene
    {
        foreach (KeyValuePair<string, IGuiScene> kvp in m_dictGuiScenes)
        {
            if (kvp.Value is T cTargetScene)
            {
                return cTargetScene;
            }
        }
        return null;
    }

    /// <summary>
    /// 프라이빗 딕셔너리에서 오브젝트명을 통해 특정 타입의 GUI 씬 인터페이스를 조회합니다.
    /// </summary>
    public T GetGuiScene<T>(string _strSceneName) where T : class, IGuiScene
    {
        if (string.IsNullOrEmpty(_strSceneName)) return null;

        if (m_dictGuiScenes.TryGetValue(_strSceneName, out IGuiScene cScene))
        {
            return cScene as T;
        }
        return null;
    }

    /// <summary>
    /// GUIManager가 소유하는 GuiPopupManager를 확보 및 초기화합니다.
    /// </summary>
    private void InitializePopupManager()
    {
        if (m_cPopupManager == null)
        {
            m_cPopupManager = GetComponent<GuiPopupManager>();
            if (m_cPopupManager == null)
            {
                m_cPopupManager = FindFirstObjectByType<GuiPopupManager>();
            }
            if (m_cPopupManager == null)
            {
                m_cPopupManager = gameObject.AddComponent<GuiPopupManager>();
            }
        }

        m_cPopupManager.Initialize(this, m_canvasMain);
    }

    private void Update()
    {
        // ESC 키 우선순위: 열려 있는 비활성화 팝업이 있으면 최상단 팝업 먼저 닫기
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            if (m_cPopupManager != null && m_cPopupManager.CloseTopBlockingPopup())
            {
                PlayUiClickSound();
                return;
            }

            if (m_strCurrentSceneName == InGameSceneName)
            {
                PauseGame();
            }
        }

        // 인게임 중 테스트용 데미지/게임오버 단축키 (K키: 자가 피격 테스트)
        if (IsGameplayActive && Input.GetKeyDown(KeyCode.K))
        {
            if (GameManager.s_cInstance != null && GameManager.s_cInstance.CurrentPlayer != null)
            {
                GameManager.s_cInstance.CurrentPlayer.TakeDamage(50f);
            }
        }
    }

    // ==================== 화면 전환 및 UI 이벤트 핸들러 (컨텍스트 함수) ====================

    /// <summary>
    /// 프라이빗 딕셔너리(m_dictGuiScenes)에서 오브젝트명을 기준으로 화면을 전환하고
    /// Time.timeScale 및 마우스 커서 상태를 제어합니다.
    /// </summary>
    public void SetGuiScene(string _strSceneName)
    {
        if (!m_bIsInitialized)
        {
            Initialize();
        }

        if (string.IsNullOrEmpty(_strSceneName)) return;

        m_strCurrentSceneName = _strSceneName;

        // 프라이빗 딕셔너리를 통해 모든 등록된 GUI 씬에 접근하여 진입/퇴장 처리
        foreach (KeyValuePair<string, IGuiScene> kvp in m_dictGuiScenes)
        {
            IGuiScene cScene = kvp.Value;
            if (cScene == null) continue;

            if (kvp.Key == _strSceneName)
            {
                cScene.OnEnter();
            }
            else
            {
                cScene.OnExit();
            }
        }

        if (m_cPopupManager != null)
        {
            m_cPopupManager.BringPopupRootToFront();
        }

        ApplySceneAndPopupState();
        SyncAudioUiControls();
    }

    /// <summary>
    /// 팝업 열림/닫힘 시 호출되며, 밑에 있는 GUI 씬 화면은 그대로 유지한 채 시간/커서/입력 상태만 갱신합니다.
    /// </summary>
    public void OnPopupStateChanged()
    {
        ApplySceneAndPopupState();
    }

    /// <summary>
    /// 현재 GUI 씬과 비활성화 팝업(BlocksGameplay) 유무를 종합해 Time.timeScale 및 커서 상태를 제어합니다.
    /// </summary>
    private void ApplySceneAndPopupState()
    {
        bool bIsInGameScene = (!string.IsNullOrEmpty(m_strCurrentSceneName) && m_strCurrentSceneName == InGameSceneName);
        bool bCanPlay = bIsInGameScene && !HasBlockingPopupOpen;

        // 3D 탄약 HUD 텍스트 가시성 연동
        if (GameManager.s_cInstance != null && GameManager.s_cInstance.HudBullets != null)
        {
            MeshRenderer mrHud = GameManager.s_cInstance.HudBullets.GetComponent<MeshRenderer>();
            if (mrHud != null)
            {
                mrHud.enabled = bIsInGameScene;
            }
        }

        if (bCanPlay)
        {
            Time.timeScale = 1f;
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
            RefreshSfxSources();
        }
        else
        {
            Time.timeScale = 0f;
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
            StopLoopingCharacterSounds();
        }
    }

    /// <summary>
    /// [시작 화면] -> [인게임] 전환 이벤트 핸들러
    /// </summary>
    public void StartGame()
    {
        PlayUiClickSound();
        SetGuiScene(InGameSceneName);
    }

    /// <summary>
    /// 인게임 <-> 일시정지 팝업 토글
    /// </summary>
    public void TogglePause()
    {
        PauseGuiScene cPausePopup = m_cPopupManager != null ? m_cPopupManager.GetPopup<PauseGuiScene>() : null;
        if (cPausePopup != null && cPausePopup.IsActive)
        {
            ResumeGame();
        }
        else if (m_strCurrentSceneName == InGameSceneName)
        {
            PauseGame();
        }
    }

    /// <summary>
    /// [인게임] -> [일시정지] 팝업 오픈 이벤트 핸들러 (인게임 UI를 유지한 채 팝업 루트 위에 덮어 표시)
    /// </summary>
    public void PauseGame()
    {
        if (m_strCurrentSceneName != InGameSceneName) return;

        PauseGuiScene cPausePopup = m_cPopupManager != null ? m_cPopupManager.GetPopup<PauseGuiScene>() : null;
        if (cPausePopup != null && cPausePopup.IsActive) return;

        PlayUiClickSound();
        if (m_cPopupManager != null)
        {
            m_cPopupManager.OpenPopup<PauseGuiScene>();
        }
    }

    /// <summary>
    /// [일시정지] -> [인게임] 복귀 이벤트 핸들러 (일시정지 팝업 닫기)
    /// </summary>
    public void ResumeGame()
    {
        PauseGuiScene cPausePopup = m_cPopupManager != null ? m_cPopupManager.GetPopup<PauseGuiScene>() : null;
        if (cPausePopup == null || !cPausePopup.IsActive) return;

        PlayUiClickSound();
        m_cPopupManager.ClosePopup<PauseGuiScene>();
    }

    /// <summary>
    /// 플레이어 사망 시 열려 있는 팝업을 닫고 프라이빗 딕셔너리에서 게임오버 씬을 찾아 전환합니다.
    /// </summary>
    public void ShowGameOver(string _strReason = "MISSION FAILED - YOU DIED")
    {
        if (!m_bIsInitialized)
        {
            Initialize();
        }

        string strGameOverName = GameOverSceneName;
        if (m_strCurrentSceneName == strGameOverName) return;

        if (m_cPopupManager != null)
        {
            m_cPopupManager.CloseAllPopups();
        }

        GameOverGuiScene cGameOver = GetGuiScene<GameOverGuiScene>();
        if (cGameOver != null)
        {
            cGameOver.ShowGameOver(_strReason);
        }

        SetGuiScene(strGameOverName);
    }

    /// <summary>
    /// 현재 씬을 재시작하고 즉시 인게임 상태로 진입하는 이벤트 핸들러
    /// </summary>
    public void RestartGame()
    {
        PlayUiClickSound();
        Time.timeScale = 1f;
        s_bStartInGameOnReload = true;
        Scene sActiveScene = SceneManager.GetActiveScene();
        SceneManager.LoadScene(sActiveScene.buildIndex);
    }

    /// <summary>
    /// 음량 조절 팝업(비활성화 팝업)을 현재 GUI 화면 위에 띄웁니다.
    /// </summary>
    public void OpenAudioSettingPopup()
    {
        if (!m_bIsInitialized)
        {
            Initialize();
        }

        PlayUiClickSound();
        if (m_cPopupManager != null)
        {
            m_cPopupManager.OpenPopup<AudioSettingPopup>();
        }
    }

    /// <summary>
    /// 타이틀 돌아가기 전 확인 팝업을 현재 GUI 화면 위에 띄웁니다.
    /// </summary>
    public void RequestReturnToTitle()
    {
        if (!m_bIsInitialized)
        {
            Initialize();
        }

        PlayUiClickSound();
        if (m_cPopupManager != null)
        {
            ConfirmPopup cConfirm = m_cPopupManager.OpenPopup<ConfirmPopup>();
            if (cConfirm != null)
            {
                cConfirm.Setup("RETURN TO TITLE", "Return to the title screen?", ReturnToTitle);
                return;
            }
        }

        ReturnToTitle();
    }

    /// <summary>
    /// 게임 종료 전 확인 팝업을 현재 GUI 화면 위에 띄웁니다.
    /// </summary>
    public void RequestQuitGame()
    {
        if (!m_bIsInitialized)
        {
            Initialize();
        }

        PlayUiClickSound();
        if (m_cPopupManager != null)
        {
            ConfirmPopup cConfirm = m_cPopupManager.OpenPopup<ConfirmPopup>();
            if (cConfirm != null)
            {
                cConfirm.Setup("QUIT GAME", "Are you sure you want to quit the game?", QuitGame);
                return;
            }
        }

        QuitGame();
    }

    /// <summary>
    /// 게임 플레이를 방해하지 않는 활성화 팝업(토스트 알림)을 화면 위에 띄웁니다.
    /// </summary>
    public void ShowNoticeToast(string _strMessage, float _fDuration = 2.5f)
    {
        if (!m_bIsInitialized)
        {
            Initialize();
        }

        if (m_cPopupManager != null)
        {
            NoticeToastPopup cToast = m_cPopupManager.OpenPopup<NoticeToastPopup>();
            if (cToast != null)
            {
                cToast.ShowMessage(_strMessage, _fDuration);
            }
        }
    }

    /// <summary>
    /// 현재 씬을 초기 상태로 리로드하고 시작(타이틀) 화면으로 돌아가는 이벤트 핸들러
    /// </summary>
    public void ReturnToTitle()
    {
        PlayUiClickSound();
        Time.timeScale = 1f;
        s_bStartInGameOnReload = false;
        Scene sActiveScene = SceneManager.GetActiveScene();
        SceneManager.LoadScene(sActiveScene.buildIndex);
    }

    /// <summary>
    /// 게임을 종료하는 이벤트 핸들러
    /// </summary>
    public void QuitGame()
    {
        PlayUiClickSound();
        Time.timeScale = 1f;
        PlayerPrefs.Save();

#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    /// <summary>
    /// BGM 슬라이더 값 변경 UI 이벤트 핸들러
    /// </summary>
    public void OnBgmSliderChanged(float _fVolume)
    {
        SetBgmVolume(_fVolume);
    }

    /// <summary>
    /// SFX 슬라이더 값 변경 UI 이벤트 핸들러
    /// </summary>
    public void OnSfxSliderChanged(float _fVolume)
    {
        SetSfxVolume(_fVolume);
    }

    /// <summary>
    /// 음소거(Mute) 토글 값 변경 UI 이벤트 핸들러
    /// </summary>
    public void OnMuteToggleChanged(bool _bMute)
    {
        PlayUiClickSound();
        SetMute(_bMute);
    }

    // ==================== 사운드 및 시스템 연동 함수 ====================

    public void SetBgmVolume(float _fVolume)
    {
        m_fBgmVolume = Mathf.Clamp01(_fVolume);

        if (m_asBgm != null)
        {
            m_asBgm.volume = m_bIsMuted ? 0f : m_fBgmVolume;
        }

        PlayerPrefs.SetFloat(PREF_BGM_VOL, m_fBgmVolume);
        SyncAudioUiControls();
    }

    public void SetSfxVolume(float _fVolume)
    {
        m_fSfxVolume = Mathf.Clamp01(_fVolume);
        ApplySfxVolumeToAllSources();

        PlayerPrefs.SetFloat(PREF_SFX_VOL, m_fSfxVolume);
        SyncAudioUiControls();
    }

    public void SetMute(bool _bMute)
    {
        m_bIsMuted = _bMute;
        AudioListener.volume = m_bIsMuted ? 0f : 1f;

        if (m_asBgm != null)
        {
            m_asBgm.volume = m_bIsMuted ? 0f : m_fBgmVolume;
        }

        ApplySfxVolumeToAllSources();

        PlayerPrefs.SetInt(PREF_MUTE, m_bIsMuted ? 1 : 0);
        SyncAudioUiControls();
    }

    public void RefreshSfxSources()
    {
        AudioSource[] arrAllSources = FindObjectsByType<AudioSource>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        for (int i = 0; i < arrAllSources.Length; i++)
        {
            AudioSource asTarget = arrAllSources[i];
            if (asTarget == null || asTarget == m_asBgm) continue;

            if (!m_dictSfxBaseVolumes.ContainsKey(asTarget))
            {
                float fBaseVol = asTarget.volume > 0.01f ? asTarget.volume : 1f;
                m_dictSfxBaseVolumes.Add(asTarget, fBaseVol);
            }
        }

        ApplySfxVolumeToAllSources();
    }

    private void ApplySfxVolumeToAllSources()
    {
        List<AudioSource> listDeadRefs = null;

        foreach (KeyValuePair<AudioSource, float> kvp in m_dictSfxBaseVolumes)
        {
            if (kvp.Key == null)
            {
                if (listDeadRefs == null) listDeadRefs = new List<AudioSource>();
                listDeadRefs.Add(kvp.Key);
                continue;
            }

            kvp.Key.volume = m_bIsMuted ? 0f : (kvp.Value * m_fSfxVolume);
        }

        if (listDeadRefs != null)
        {
            for (int i = 0; i < listDeadRefs.Count; i++)
            {
                m_dictSfxBaseVolumes.Remove(listDeadRefs[i]);
            }
        }

        if (m_asUiClick != null)
        {
            m_asUiClick.volume = m_bIsMuted ? 0f : m_fSfxVolume;
        }
    }

    private void StopLoopingCharacterSounds()
    {
        foreach (KeyValuePair<AudioSource, float> kvp in m_dictSfxBaseVolumes)
        {
            if (kvp.Key != null && kvp.Key.isPlaying && kvp.Key != m_asUiClick)
            {
                kvp.Key.Stop();
            }
        }
    }

    public void PlayUiClickSound()
    {
        if (m_asUiClick != null && m_asUiClick.clip != null && !m_bIsMuted)
        {
            m_asUiClick.PlayOneShot(m_asUiClick.clip, m_fSfxVolume);
        }
    }

    // ==================== 인게임 HUD 갱신 함수 (프라이빗 딕셔너리 액세스) ====================

    public void UpdateHpUi(float _fCurrentHp, float _fMaxHp)
    {
        if (!m_bIsInitialized)
        {
            Initialize();
        }

        InGameGuiScene cInGame = GetGuiScene<InGameGuiScene>();
        if (cInGame != null)
        {
            cInGame.UpdateHp(_fCurrentHp, _fMaxHp);
        }
    }

    public void UpdateAmmoUi(string _strWeaponName, float _fBulletsInGun, float _fBulletsTotal)
    {
        if (!m_bIsInitialized)
        {
            Initialize();
        }

        InGameGuiScene cInGame = GetGuiScene<InGameGuiScene>();
        if (cInGame != null)
        {
            cInGame.UpdateAmmo(Mathf.RoundToInt(_fBulletsInGun), Mathf.RoundToInt(_fBulletsTotal));
        }
    }

    // ==================== 내부 오디오 설정 및 동기화 ====================

    private void LoadAudioPreferences()
    {
        m_fBgmVolume = PlayerPrefs.GetFloat(PREF_BGM_VOL, m_fBgmVolume);
        m_fSfxVolume = PlayerPrefs.GetFloat(PREF_SFX_VOL, m_fSfxVolume);
        m_bIsMuted = PlayerPrefs.GetInt(PREF_MUTE, 0) == 1;
        AudioListener.volume = m_bIsMuted ? 0f : 1f;
    }

    private void SyncAudioUiControls()
    {
        // 프라이빗 딕셔너리(m_dictGuiScenes)에 등록된 객체 중 IAudioSyncableGui 구현체에 일괄 동기화
        foreach (KeyValuePair<string, IGuiScene> kvp in m_dictGuiScenes)
        {
            if (kvp.Value is IAudioSyncableGui cAudioSyncable)
            {
                cAudioSyncable.SyncAudioSettings(m_fBgmVolume, m_fSfxVolume, m_bIsMuted);
            }
        }

        // 팝업 관리자(m_cPopupManager)에 등록된 팝업 중 IAudioSyncableGui 구현체에도 동기화
        if (m_cPopupManager != null)
        {
            m_cPopupManager.SyncAudioPopups(m_fBgmVolume, m_fSfxVolume, m_bIsMuted);
        }
    }

    private void EnsureAudioSources()
    {
        if (m_asBgm == null)
        {
            Transform trBgm = transform.Find("BGM_AudioSource");
            if (trBgm != null)
            {
                m_asBgm = trBgm.GetComponent<AudioSource>();
            }
            if (m_asBgm == null)
            {
                GameObject objBgm = new GameObject("BGM_AudioSource");
                objBgm.transform.SetParent(transform, false);
                m_asBgm = objBgm.AddComponent<AudioSource>();
            }
        }

        m_asBgm.loop = true;
        m_asBgm.playOnAwake = false;
        m_asBgm.spatialBlend = 0f;
        m_asBgm.ignoreListenerPause = true;
        m_asBgm.volume = m_bIsMuted ? 0f : m_fBgmVolume;

        if (m_asBgm.clip == null)
        {
            m_asBgm.clip = CreateProceduralBgmClip();
        }

        if (!m_asBgm.isPlaying)
        {
            m_asBgm.Play();
        }

        if (m_asUiClick == null)
        {
            Transform trClick = transform.Find("UIClick_AudioSource");
            if (trClick != null)
            {
                m_asUiClick = trClick.GetComponent<AudioSource>();
            }
            if (m_asUiClick == null)
            {
                GameObject objClick = new GameObject("UIClick_AudioSource");
                objClick.transform.SetParent(transform, false);
                m_asUiClick = objClick.AddComponent<AudioSource>();
            }
        }

        m_asUiClick.loop = false;
        m_asUiClick.playOnAwake = false;
        m_asUiClick.spatialBlend = 0f;
        m_asUiClick.ignoreListenerPause = true;
        m_asUiClick.volume = m_bIsMuted ? 0f : m_fSfxVolume;

        if (m_asUiClick.clip == null)
        {
            m_asUiClick.clip = CreateProceduralClickClip();
        }
    }

    private AudioClip CreateProceduralBgmClip()
    {
        int nSampleRate = 22050;
        int nLengthSeconds = 4;
        int nTotalSamples = nSampleRate * nLengthSeconds;
        float[] arrSamples = new float[nTotalSamples];

        float[] arrChordFreqs = new float[] { 110.0f, 164.81f, 220.0f, 261.63f };
        for (int i = 0; i < nTotalSamples; i++)
        {
            float fTime = (float)i / nSampleRate;
            float fSignal = 0f;
            for (int c = 0; c < arrChordFreqs.Length; c++)
            {
                fSignal += Mathf.Sin(2f * Mathf.PI * arrChordFreqs[c] * fTime) * 0.06f;
            }
            float fLfo = 0.75f + 0.25f * Mathf.Sin(2f * Mathf.PI * 0.5f * fTime);
            arrSamples[i] = fSignal * fLfo;
        }

        AudioClip clipBgm = AudioClip.Create("ProceduralAmbientBGM", nTotalSamples, 1, nSampleRate, false);
        clipBgm.SetData(arrSamples, 0);
        return clipBgm;
    }

    private AudioClip CreateProceduralClickClip()
    {
        int nSampleRate = 22050;
        int nTotalSamples = nSampleRate / 20;
        float[] arrSamples = new float[nTotalSamples];

        for (int i = 0; i < nTotalSamples; i++)
        {
            float fTime = (float)i / nSampleRate;
            float fEnvelope = 1f - ((float)i / nTotalSamples);
            arrSamples[i] = Mathf.Sin(2f * Mathf.PI * 880f * fTime) * fEnvelope * 0.25f;
        }

        AudioClip clipClick = AudioClip.Create("ProceduralUiClick", nTotalSamples, 1, nSampleRate, false);
        clipClick.SetData(arrSamples, 0);
        return clipClick;
    }
}
