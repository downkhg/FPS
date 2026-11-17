using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.EventSystems;
using TMPro;

public enum EGuiState
{
    Title,      // 시작(타이틀) 화면
    InGame,     // 인게임 HUD 화면
    Pause,      // 일시정지 및 환경설정 화면
    GameOver    // 게임오버 화면
}

public class GUIManager : MonoBehaviour
{
    public static GUIManager s_cInstance;

    // 씬 리로드 시 타이틀을 건너뛰고 바로 인게임으로 진입할지 여부 (Restart 기능용)
    private static bool s_bStartInGameOnReload = false;

    [Header("GUI State")]
    [SerializeField]
    private EGuiState m_eCurrentState = EGuiState.Title;

    [Header("Root Canvas & Panels")]
    [SerializeField]
    private Canvas m_canvasMain;

    [SerializeField]
    private GameObject m_objTitlePanel;

    [SerializeField]
    private GameObject m_objInGamePanel;

    [SerializeField]
    private GameObject m_objPausePanel;

    [SerializeField]
    private GameObject m_objGameOverPanel;

    [Header("Title Panel Controls")]
    [SerializeField]
    private UnityEngine.UI.Button m_btnTitleStart;

    [SerializeField]
    private UnityEngine.UI.Button m_btnTitleQuit;

    [SerializeField]
    private UnityEngine.UI.Slider m_sliderTitleBgm;

    [SerializeField]
    private UnityEngine.UI.Slider m_sliderTitleSfx;

    [SerializeField]
    private UnityEngine.UI.Toggle m_toggleTitleMute;

    [SerializeField]
    private TextMeshProUGUI m_txtTitleBgmValue;

    [SerializeField]
    private TextMeshProUGUI m_txtTitleSfxValue;

    [Header("InGame HUD Controls")]
    [SerializeField]
    private TextMeshProUGUI m_txtInGameGuide;

    [SerializeField]
    private TextMeshProUGUI m_txtInGameHp;

    [SerializeField]
    private UnityEngine.UI.Image m_imgInGameHpFill;

    [SerializeField]
    private TextMeshProUGUI m_txtInGameAmmo;

    [Header("Pause Panel Controls")]
    [SerializeField]
    private UnityEngine.UI.Button m_btnPauseResume;

    [SerializeField]
    private UnityEngine.UI.Button m_btnPauseRestart;

    [SerializeField]
    private UnityEngine.UI.Button m_btnPauseTitle;

    [SerializeField]
    private UnityEngine.UI.Button m_btnPauseQuit;

    [SerializeField]
    private UnityEngine.UI.Slider m_sliderPauseBgm;

    [SerializeField]
    private UnityEngine.UI.Slider m_sliderPauseSfx;

    [SerializeField]
    private UnityEngine.UI.Toggle m_togglePauseMute;

    [SerializeField]
    private TextMeshProUGUI m_txtPauseBgmValue;

    [SerializeField]
    private TextMeshProUGUI m_txtPauseSfxValue;

    [Header("GameOver Panel Controls")]
    [SerializeField]
    private TextMeshProUGUI m_txtGameOverMessage;

    [SerializeField]
    private UnityEngine.UI.Button m_btnGameOverRestart;

    [SerializeField]
    private UnityEngine.UI.Button m_btnGameOverTitle;

    [SerializeField]
    private UnityEngine.UI.Button m_btnGameOverQuit;

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
    private bool m_bSuppressUiCallbacks = false;

    private const string PREF_BGM_VOL = "FPS_BGM_VOLUME";
    private const string PREF_SFX_VOL = "FPS_SFX_VOLUME";
    private const string PREF_MUTE = "FPS_AUDIO_MUTE";

    // ==================== 프로퍼티 ====================
    public EGuiState CurrentState => m_eCurrentState;
    public bool IsGameplayActive => m_eCurrentState == EGuiState.InGame;
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
        EnsureUiHierarchy();
        EnsureAudioSources();
        BindUiEvents();
        RefreshSfxSources();
        SyncAudioUiControls();

        if (s_bStartInGameOnReload)
        {
            s_bStartInGameOnReload = false;
            SetGuiState(EGuiState.InGame);
        }
        else
        {
            SetGuiState(EGuiState.Title);
        }
    }

    private void Update()
    {
        // ESC 키로 인게임 <-> 일시정지 토글
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            if (m_eCurrentState == EGuiState.InGame || m_eCurrentState == EGuiState.Pause)
            {
                TogglePause();
            }
        }

        // 인게임 중 테스트용 데미지/게임오버 단축키 (K키: 자가 피격 테스트)
        if (m_eCurrentState == EGuiState.InGame && Input.GetKeyDown(KeyCode.K))
        {
            if (GameManager.s_cInstance != null && GameManager.s_cInstance.CurrentPlayer != null)
            {
                GameManager.s_cInstance.CurrentPlayer.TakeDamage(50f);
            }
        }
    }

    // ==================== 화면 전환 제어 함수 ====================

    /// <summary>
    /// 지정한 GUI 상태로 화면을 전환하고 Time.timeScale 및 마우스 커서 상태를 제어합니다.
    /// </summary>
    public void SetGuiState(EGuiState _eNewState)
    {
        m_eCurrentState = _eNewState;

        if (m_objTitlePanel) m_objTitlePanel.SetActive(_eNewState == EGuiState.Title);
        if (m_objInGamePanel) m_objInGamePanel.SetActive(_eNewState == EGuiState.InGame);
        if (m_objPausePanel) m_objPausePanel.SetActive(_eNewState == EGuiState.Pause);
        if (m_objGameOverPanel) m_objGameOverPanel.SetActive(_eNewState == EGuiState.GameOver);

        bool bIsInGame = (_eNewState == EGuiState.InGame);

        // 3D 탄약 HUD 텍스트 가시성 연동
        if (GameManager.s_cInstance != null && GameManager.s_cInstance.HudBullets != null)
        {
            MeshRenderer mrHud = GameManager.s_cInstance.HudBullets.GetComponent<MeshRenderer>();
            if (mrHud != null)
            {
                mrHud.enabled = bIsInGame;
            }
        }

        if (bIsInGame)
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

        SyncAudioUiControls();
    }

    /// <summary>
    /// [시작 화면] -> [인게임] 전환
    /// </summary>
    public void StartGame()
    {
        PlayUiClickSound();
        SetGuiState(EGuiState.InGame);
    }

    /// <summary>
    /// 인게임 <-> 일시정지 토글
    /// </summary>
    public void TogglePause()
    {
        if (m_eCurrentState == EGuiState.InGame)
        {
            PauseGame();
        }
        else if (m_eCurrentState == EGuiState.Pause)
        {
            ResumeGame();
        }
    }

    /// <summary>
    /// [인게임] -> [일시정지] 전환
    /// </summary>
    public void PauseGame()
    {
        if (m_eCurrentState != EGuiState.InGame) return;
        PlayUiClickSound();
        SetGuiState(EGuiState.Pause);
    }

    /// <summary>
    /// [일시정지] -> [인게임] 복귀
    /// </summary>
    public void ResumeGame()
    {
        if (m_eCurrentState != EGuiState.Pause) return;
        PlayUiClickSound();
        SetGuiState(EGuiState.InGame);
    }

    /// <summary>
    /// 플레이어 사망 시 [게임오버] 화면으로 전환
    /// </summary>
    public void ShowGameOver(string _strReason = "MISSION FAILED - YOU DIED")
    {
        if (m_eCurrentState == EGuiState.GameOver) return;

        if (m_txtGameOverMessage != null && !string.IsNullOrEmpty(_strReason))
        {
            m_txtGameOverMessage.text = _strReason;
        }

        SetGuiState(EGuiState.GameOver);
    }

    /// <summary>
    /// 현재 씬을 재시작하고 즉시 인게임 상태로 진입합니다.
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
    /// 현재 씬을 초기 상태로 리로드하고 시작(타이틀) 화면으로 돌아갑니다.
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
    /// 게임을 종료합니다 (에디터 플레이 모드 종료 및 빌드 애플리케이션 종료 지원).
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

    // ==================== 사운드 및 시스템 연동 함수 ====================

    /// <summary>
    /// 배경음악(BGM) 볼륨을 조절합니다 (0.0 ~ 1.0).
    /// </summary>
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

    /// <summary>
    /// 효과음(SFX) 볼륨을 조절합니다 (0.0 ~ 1.0).
    /// </summary>
    public void SetSfxVolume(float _fVolume)
    {
        m_fSfxVolume = Mathf.Clamp01(_fVolume);
        ApplySfxVolumeToAllSources();

        PlayerPrefs.SetFloat(PREF_SFX_VOL, m_fSfxVolume);
        SyncAudioUiControls();
    }

    /// <summary>
    /// 전체 사운드 음소거(Mute) 여부를 설정합니다.
    /// </summary>
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

    /// <summary>
    /// 씬 내 모든 효과음 AudioSource를 탐색하여 원래 볼륨 대비 SFX 볼륨 비율을 적용합니다.
    /// </summary>
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

    // ==================== 인게임 HUD 갱신 함수 ====================

    public void UpdateHpUi(float _fCurrentHp, float _fMaxHp)
    {
        float fSafeMax = Mathf.Max(1f, _fMaxHp);
        float fRatio = Mathf.Clamp01(_fCurrentHp / fSafeMax);

        if (m_txtInGameHp != null)
        {
            m_txtInGameHp.text = $"HP: {Mathf.CeilToInt(_fCurrentHp)} / {Mathf.CeilToInt(fSafeMax)}";
        }

        if (m_imgInGameHpFill != null)
        {
            RectTransform rtFill = m_imgInGameHpFill.rectTransform;
            rtFill.anchorMax = new Vector2(fRatio, 1f);
            m_imgInGameHpFill.color = Color.Lerp(new Color(0.9f, 0.15f, 0.15f, 0.9f), new Color(0.2f, 0.85f, 0.35f, 0.9f), fRatio);
        }
    }

    public void UpdateAmmoUi(string _strWeaponName, float _fBulletsInGun, float _fBulletsTotal)
    {
        if (m_txtInGameAmmo != null)
        {
            m_txtInGameAmmo.text = $"AMMO: {Mathf.RoundToInt(_fBulletsInGun)} / {Mathf.RoundToInt(_fBulletsTotal)}";
        }
    }

    // ==================== 내부 초기화 및 UI 자동 구축 ====================

    private void LoadAudioPreferences()
    {
        m_fBgmVolume = PlayerPrefs.GetFloat(PREF_BGM_VOL, m_fBgmVolume);
        m_fSfxVolume = PlayerPrefs.GetFloat(PREF_SFX_VOL, m_fSfxVolume);
        m_bIsMuted = PlayerPrefs.GetInt(PREF_MUTE, 0) == 1;
        AudioListener.volume = m_bIsMuted ? 0f : 1f;
    }

    private void SyncAudioUiControls()
    {
        m_bSuppressUiCallbacks = true;

        int nBgmPercent = Mathf.RoundToInt(m_fBgmVolume * 100f);
        int nSfxPercent = Mathf.RoundToInt(m_fSfxVolume * 100f);

        if (m_sliderTitleBgm) m_sliderTitleBgm.value = m_fBgmVolume;
        if (m_sliderPauseBgm) m_sliderPauseBgm.value = m_fBgmVolume;

        if (m_sliderTitleSfx) m_sliderTitleSfx.value = m_fSfxVolume;
        if (m_sliderPauseSfx) m_sliderPauseSfx.value = m_fSfxVolume;

        if (m_toggleTitleMute) m_toggleTitleMute.isOn = m_bIsMuted;
        if (m_togglePauseMute) m_togglePauseMute.isOn = m_bIsMuted;

        if (m_txtTitleBgmValue) m_txtTitleBgmValue.text = $"BGM Volume: {nBgmPercent}%";
        if (m_txtPauseBgmValue) m_txtPauseBgmValue.text = $"BGM Volume: {nBgmPercent}%";

        if (m_txtTitleSfxValue) m_txtTitleSfxValue.text = $"SFX Volume: {nSfxPercent}%";
        if (m_txtPauseSfxValue) m_txtPauseSfxValue.text = $"SFX Volume: {nSfxPercent}%";

        m_bSuppressUiCallbacks = false;
    }

    private void BindUiEvents()
    {
        if (m_btnTitleStart)
        {
            m_btnTitleStart.onClick.RemoveAllListeners();
            m_btnTitleStart.onClick.AddListener(StartGame);
        }
        if (m_btnTitleQuit)
        {
            m_btnTitleQuit.onClick.RemoveAllListeners();
            m_btnTitleQuit.onClick.AddListener(QuitGame);
        }
        if (m_sliderTitleBgm)
        {
            m_sliderTitleBgm.onValueChanged.RemoveAllListeners();
            m_sliderTitleBgm.onValueChanged.AddListener(OnBgmSliderChanged);
        }
        if (m_sliderTitleSfx)
        {
            m_sliderTitleSfx.onValueChanged.RemoveAllListeners();
            m_sliderTitleSfx.onValueChanged.AddListener(OnSfxSliderChanged);
        }
        if (m_toggleTitleMute)
        {
            m_toggleTitleMute.onValueChanged.RemoveAllListeners();
            m_toggleTitleMute.onValueChanged.AddListener(OnMuteToggleChanged);
        }

        if (m_btnPauseResume)
        {
            m_btnPauseResume.onClick.RemoveAllListeners();
            m_btnPauseResume.onClick.AddListener(ResumeGame);
        }
        if (m_btnPauseRestart)
        {
            m_btnPauseRestart.onClick.RemoveAllListeners();
            m_btnPauseRestart.onClick.AddListener(RestartGame);
        }
        if (m_btnPauseTitle)
        {
            m_btnPauseTitle.onClick.RemoveAllListeners();
            m_btnPauseTitle.onClick.AddListener(ReturnToTitle);
        }
        if (m_btnPauseQuit)
        {
            m_btnPauseQuit.onClick.RemoveAllListeners();
            m_btnPauseQuit.onClick.AddListener(QuitGame);
        }
        if (m_sliderPauseBgm)
        {
            m_sliderPauseBgm.onValueChanged.RemoveAllListeners();
            m_sliderPauseBgm.onValueChanged.AddListener(OnBgmSliderChanged);
        }
        if (m_sliderPauseSfx)
        {
            m_sliderPauseSfx.onValueChanged.RemoveAllListeners();
            m_sliderPauseSfx.onValueChanged.AddListener(OnSfxSliderChanged);
        }
        if (m_togglePauseMute)
        {
            m_togglePauseMute.onValueChanged.RemoveAllListeners();
            m_togglePauseMute.onValueChanged.AddListener(OnMuteToggleChanged);
        }

        if (m_btnGameOverRestart)
        {
            m_btnGameOverRestart.onClick.RemoveAllListeners();
            m_btnGameOverRestart.onClick.AddListener(RestartGame);
        }
        if (m_btnGameOverTitle)
        {
            m_btnGameOverTitle.onClick.RemoveAllListeners();
            m_btnGameOverTitle.onClick.AddListener(ReturnToTitle);
        }
        if (m_btnGameOverQuit)
        {
            m_btnGameOverQuit.onClick.RemoveAllListeners();
            m_btnGameOverQuit.onClick.AddListener(QuitGame);
        }
    }

    private void OnBgmSliderChanged(float _fVal)
    {
        if (m_bSuppressUiCallbacks) return;
        SetBgmVolume(_fVal);
    }

    private void OnSfxSliderChanged(float _fVal)
    {
        if (m_bSuppressUiCallbacks) return;
        SetSfxVolume(_fVal);
    }

    private void OnMuteToggleChanged(bool _bMute)
    {
        if (m_bSuppressUiCallbacks) return;
        PlayUiClickSound();
        SetMute(_bMute);
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

    /// <summary>
    /// 외부 BGM 오디오 파일이 없을 때도 BGM 볼륨 조절을 즉시 체감할 수 있도록 앰비언트 루프 클립을 생성합니다.
    /// </summary>
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
            // 부드러운 볼륨 맥동(LFO)
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
        int nTotalSamples = nSampleRate / 20; // 0.05초
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

    // ==================== UI 계층 확인 및 생성 ====================

    [ContextMenu("Generate GUI Hierarchy In Scene")]
    public void EnsureUiHierarchy()
    {
        if (m_canvasMain == null)
        {
            m_canvasMain = FindFirstObjectByType<Canvas>();
            if (m_canvasMain == null)
            {
                GameObject objCanvas = new GameObject("Canvas");
                m_canvasMain = objCanvas.AddComponent<Canvas>();
                m_canvasMain.renderMode = RenderMode.ScreenSpaceOverlay;
                objCanvas.AddComponent<UnityEngine.UI.CanvasScaler>();
                objCanvas.AddComponent<UnityEngine.UI.GraphicRaycaster>();
            }
        }

        UnityEngine.UI.CanvasScaler cScaler = m_canvasMain.GetComponent<UnityEngine.UI.CanvasScaler>();
        if (cScaler != null)
        {
            cScaler.uiScaleMode = UnityEngine.UI.CanvasScaler.ScaleMode.ScaleWithScreenSize;
            cScaler.referenceResolution = new Vector2(1920f, 1080f);
            cScaler.matchWidthOrHeight = 0.5f;
        }

        if (FindFirstObjectByType<EventSystem>() == null)
        {
            GameObject objEventSystem = new GameObject("EventSystem");
            objEventSystem.AddComponent<EventSystem>();
            objEventSystem.AddComponent<StandaloneInputModule>();
        }

        Transform trCanvas = m_canvasMain.transform;

        // 기존 씬의 안내 텍스트(Text (TMP))가 Canvas 직속 자식으로 있으면 InGamePanel 내부로 편입
        Transform trExistingTmp = trCanvas.Find("Text (TMP)");

        if (m_objInGamePanel == null)
        {
            Transform trFound = trCanvas.Find("InGamePanel");
            m_objInGamePanel = trFound != null ? trFound.gameObject : BuildInGamePanel(trCanvas, trExistingTmp);
        }

        if (m_objTitlePanel == null)
        {
            Transform trFound = trCanvas.Find("TitlePanel");
            m_objTitlePanel = trFound != null ? trFound.gameObject : BuildTitlePanel(trCanvas);
        }

        if (m_objPausePanel == null)
        {
            Transform trFound = trCanvas.Find("PausePanel");
            m_objPausePanel = trFound != null ? trFound.gameObject : BuildPausePanel(trCanvas);
        }

        if (m_objGameOverPanel == null)
        {
            Transform trFound = trCanvas.Find("GameOverPanel");
            m_objGameOverPanel = trFound != null ? trFound.gameObject : BuildGameOverPanel(trCanvas);
        }
    }

    private GameObject BuildInGamePanel(Transform _trCanvas, Transform _trExistingGuide)
    {
        GameObject objPanel = CreateFullScreenPanel(_trCanvas, "InGamePanel", new Color(0f, 0f, 0f, 0f), false);
        RectTransform rtPanel = objPanel.GetComponent<RectTransform>();

        if (_trExistingGuide != null)
        {
            _trExistingGuide.SetParent(rtPanel, false);
            m_txtInGameGuide = _trExistingGuide.GetComponent<TextMeshProUGUI>();
            if (m_txtInGameGuide != null)
            {
                m_txtInGameGuide.raycastTarget = false;
                m_txtInGameGuide.fontSize = 20f;
                m_txtInGameGuide.text = "Move: WASD | Reload: R | Melee: Q | Crouch: C\nFire: LMB | Aim: RMB | Switch Gun: E / 1,2\n[ESC] Pause & Audio Menu | [K] Test Damage";
            }
        }
        else
        {
            m_txtInGameGuide = CreateTmpText(rtPanel, "GuideText",
                "Move: WASD | Reload: R | Melee: Q | Crouch: C\nFire: LMB | Aim: RMB | Switch Gun: E / 1,2\n[ESC] Pause & Audio Menu | [K] Test Damage",
                20, TextAlignmentOptions.TopLeft, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(24f, -24f), new Vector2(700f, 120f));
        }

        // 좌측 하단 HP 바 컨테이너
        GameObject objHpBg = CreateUiBox(rtPanel, "HpBarBackground", new Color(0.1f, 0.1f, 0.12f, 0.8f),
            new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(30f, 30f), new Vector2(340f, 42f));

        GameObject objHpFill = CreateUiBox(objHpBg.GetComponent<RectTransform>(), "HpBarFill", new Color(0.2f, 0.85f, 0.35f, 0.9f),
            new Vector2(0f, 0f), new Vector2(1f, 1f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(-8f, -8f));
        m_imgInGameHpFill = objHpFill.GetComponent<UnityEngine.UI.Image>();

        m_txtInGameHp = CreateTmpText(objHpBg.GetComponent<RectTransform>(), "HpText", "HP: 100 / 100",
            22, TextAlignmentOptions.Center, new Vector2(0f, 0f), new Vector2(1f, 1f), new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);

        // 우측 하단 탄약 표시
        GameObject objAmmoBg = CreateUiBox(rtPanel, "AmmoBackground", new Color(0.1f, 0.1f, 0.12f, 0.8f),
            new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(-30f, 30f), new Vector2(300f, 42f));

        m_txtInGameAmmo = CreateTmpText(objAmmoBg.GetComponent<RectTransform>(), "AmmoText", "AMMO: -- / --",
            22, TextAlignmentOptions.Center, new Vector2(0f, 0f), new Vector2(1f, 1f), new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);

        return objPanel;
    }

    private GameObject BuildTitlePanel(Transform _trCanvas)
    {
        GameObject objPanel = CreateFullScreenPanel(_trCanvas, "TitlePanel", new Color(0.05f, 0.07f, 0.1f, 0.92f), true);
        RectTransform rtPanel = objPanel.GetComponent<RectTransform>();

        GameObject objCard = CreateUiBox(rtPanel, "TitleCard", new Color(0.12f, 0.15f, 0.2f, 0.96f),
            new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(560f, 620f));
        RectTransform rtCard = objCard.GetComponent<RectTransform>();

        CreateTmpText(rtCard, "TitleHeader", "EASY FPS COMBAT",
            44, TextAlignmentOptions.Center, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -35f), new Vector2(500f, 60f));

        CreateTmpText(rtCard, "TitleSubHeader", "SYSTEM & AUDIO CONTROLLER READY",
            18, TextAlignmentOptions.Center, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -90f), new Vector2(480f, 30f));

        m_btnTitleStart = CreateButton(rtCard, "StartGameButton", "START GAME",
            new Vector2(0f, 145f), new Vector2(380f, 58f), new Color(0.18f, 0.62f, 0.35f, 1f));

        m_txtTitleBgmValue = CreateTmpText(rtCard, "TitleBgmLabel", "BGM Volume: 50%",
            20, TextAlignmentOptions.Left, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, 65f), new Vector2(380f, 28f));

        m_sliderTitleBgm = CreateSlider(rtCard, "TitleBgmSlider", new Vector2(0f, 32f), new Vector2(380f, 26f), m_fBgmVolume);

        m_txtTitleSfxValue = CreateTmpText(rtCard, "TitleSfxLabel", "SFX Volume: 80%",
            20, TextAlignmentOptions.Left, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, -15f), new Vector2(380f, 28f));

        m_sliderTitleSfx = CreateSlider(rtCard, "TitleSfxSlider", new Vector2(0f, -48f), new Vector2(380f, 26f), m_fSfxVolume);

        m_toggleTitleMute = CreateToggle(rtCard, "TitleMuteToggle", "Mute All Audio", new Vector2(0f, -105f), new Vector2(380f, 36f), m_bIsMuted);

        m_btnTitleQuit = CreateButton(rtCard, "QuitGameButton", "QUIT GAME",
            new Vector2(0f, -195f), new Vector2(380f, 54f), new Color(0.68f, 0.2f, 0.2f, 1f));

        return objPanel;
    }

    private GameObject BuildPausePanel(Transform _trCanvas)
    {
        GameObject objPanel = CreateFullScreenPanel(_trCanvas, "PausePanel", new Color(0.03f, 0.04f, 0.06f, 0.85f), true);
        RectTransform rtPanel = objPanel.GetComponent<RectTransform>();

        GameObject objCard = CreateUiBox(rtPanel, "PauseCard", new Color(0.12f, 0.15f, 0.2f, 0.96f),
            new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(560f, 680f));
        RectTransform rtCard = objCard.GetComponent<RectTransform>();

        CreateTmpText(rtCard, "PauseHeader", "PAUSED",
            42, TextAlignmentOptions.Center, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -30f), new Vector2(460f, 55f));

        m_txtPauseBgmValue = CreateTmpText(rtCard, "PauseBgmLabel", "BGM Volume: 50%",
            20, TextAlignmentOptions.Left, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, 195f), new Vector2(380f, 28f));

        m_sliderPauseBgm = CreateSlider(rtCard, "PauseBgmSlider", new Vector2(0f, 162f), new Vector2(380f, 26f), m_fBgmVolume);

        m_txtPauseSfxValue = CreateTmpText(rtCard, "PauseSfxLabel", "SFX Volume: 80%",
            20, TextAlignmentOptions.Left, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, 115f), new Vector2(380f, 28f));

        m_sliderPauseSfx = CreateSlider(rtCard, "PauseSfxSlider", new Vector2(0f, 82f), new Vector2(380f, 26f), m_fSfxVolume);

        m_togglePauseMute = CreateToggle(rtCard, "PauseMuteToggle", "Mute All Audio", new Vector2(0f, 30f), new Vector2(380f, 36f), m_bIsMuted);

        m_btnPauseResume = CreateButton(rtCard, "ResumeButton", "RESUME (ESC)",
            new Vector2(0f, -35f), new Vector2(380f, 52f), new Color(0.18f, 0.62f, 0.35f, 1f));

        m_btnPauseRestart = CreateButton(rtCard, "RestartButton", "RESTART MISSION",
            new Vector2(0f, -100f), new Vector2(380f, 52f), new Color(0.24f, 0.45f, 0.72f, 1f));

        m_btnPauseTitle = CreateButton(rtCard, "TitleMenuButton", "RETURN TO TITLE",
            new Vector2(0f, -165f), new Vector2(380f, 52f), new Color(0.45f, 0.45f, 0.5f, 1f));

        m_btnPauseQuit = CreateButton(rtCard, "PauseQuitButton", "QUIT GAME",
            new Vector2(0f, -230f), new Vector2(380f, 52f), new Color(0.68f, 0.2f, 0.2f, 1f));

        return objPanel;
    }

    private GameObject BuildGameOverPanel(Transform _trCanvas)
    {
        GameObject objPanel = CreateFullScreenPanel(_trCanvas, "GameOverPanel", new Color(0.18f, 0.02f, 0.02f, 0.9f), true);
        RectTransform rtPanel = objPanel.GetComponent<RectTransform>();

        GameObject objCard = CreateUiBox(rtPanel, "GameOverCard", new Color(0.12f, 0.08f, 0.08f, 0.96f),
            new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(560f, 500f));
        RectTransform rtCard = objCard.GetComponent<RectTransform>();

        TextMeshProUGUI txtHeader = CreateTmpText(rtCard, "GameOverHeader", "GAME OVER",
            52, TextAlignmentOptions.Center, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -40f), new Vector2(480f, 65f));
        txtHeader.color = new Color(1f, 0.25f, 0.25f, 1f);

        m_txtGameOverMessage = CreateTmpText(rtCard, "GameOverReasonText", "MISSION FAILED - YOU DIED",
            22, TextAlignmentOptions.Center, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -115f), new Vector2(480f, 45f));

        m_btnGameOverRestart = CreateButton(rtCard, "GameOverRestartButton", "RETRY / RESTART",
            new Vector2(0f, 20f), new Vector2(380f, 56f), new Color(0.18f, 0.62f, 0.35f, 1f));

        m_btnGameOverTitle = CreateButton(rtCard, "GameOverTitleButton", "RETURN TO TITLE",
            new Vector2(0f, -55f), new Vector2(380f, 56f), new Color(0.35f, 0.4f, 0.5f, 1f));

        m_btnGameOverQuit = CreateButton(rtCard, "GameOverQuitButton", "QUIT GAME",
            new Vector2(0f, -130f), new Vector2(380f, 56f), new Color(0.68f, 0.2f, 0.2f, 1f));

        return objPanel;
    }

    // ==================== uGUI 요소 생성 헬퍼 ====================

    private GameObject CreateFullScreenPanel(Transform _trParent, string _strName, Color _sColor, bool _bRaycastTarget)
    {
        GameObject objPanel = new GameObject(_strName);
        objPanel.transform.SetParent(_trParent, false);

        RectTransform rt = objPanel.AddComponent<RectTransform>();
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = Vector2.zero;
        rt.sizeDelta = Vector2.zero;

        UnityEngine.UI.Image img = objPanel.AddComponent<UnityEngine.UI.Image>();
        img.color = _sColor;
        img.raycastTarget = _bRaycastTarget;

        return objPanel;
    }

    private GameObject CreateUiBox(RectTransform _rtParent, string _strName, Color _sColor,
        Vector2 _vAnchorMin, Vector2 _vAnchorMax, Vector2 _vPivot, Vector2 _vAnchoredPos, Vector2 _vSizeDelta)
    {
        GameObject objBox = new GameObject(_strName);
        objBox.transform.SetParent(_rtParent, false);

        RectTransform rt = objBox.AddComponent<RectTransform>();
        rt.anchorMin = _vAnchorMin;
        rt.anchorMax = _vAnchorMax;
        rt.pivot = _vPivot;
        rt.anchoredPosition = _vAnchoredPos;
        rt.sizeDelta = _vSizeDelta;

        UnityEngine.UI.Image img = objBox.AddComponent<UnityEngine.UI.Image>();
        img.color = _sColor;
        img.raycastTarget = false;

        return objBox;
    }

    private TextMeshProUGUI CreateTmpText(RectTransform _rtParent, string _strName, string _strText,
        float _fFontSize, TextAlignmentOptions _eAlign,
        Vector2 _vAnchorMin, Vector2 _vAnchorMax, Vector2 _vPivot, Vector2 _vAnchoredPos, Vector2 _vSizeDelta)
    {
        GameObject objTxt = new GameObject(_strName);
        objTxt.transform.SetParent(_rtParent, false);

        RectTransform rt = objTxt.AddComponent<RectTransform>();
        rt.anchorMin = _vAnchorMin;
        rt.anchorMax = _vAnchorMax;
        rt.pivot = _vPivot;
        rt.anchoredPosition = _vAnchoredPos;
        rt.sizeDelta = _vSizeDelta;

        TextMeshProUGUI txt = objTxt.AddComponent<TextMeshProUGUI>();
        txt.text = _strText;
        txt.fontSize = _fFontSize;
        txt.alignment = _eAlign;
        txt.color = Color.white;
        txt.raycastTarget = false;

        return txt;
    }

    private UnityEngine.UI.Button CreateButton(RectTransform _rtParent, string _strName, string _strLabel,
        Vector2 _vAnchoredPos, Vector2 _vSizeDelta, Color _sBgColor)
    {
        GameObject objBtn = new GameObject(_strName);
        objBtn.transform.SetParent(_rtParent, false);

        RectTransform rt = objBtn.AddComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.5f, 0.5f);
        rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = _vAnchoredPos;
        rt.sizeDelta = _vSizeDelta;

        UnityEngine.UI.Image img = objBtn.AddComponent<UnityEngine.UI.Image>();
        img.color = _sBgColor;
        img.raycastTarget = true;

        UnityEngine.UI.Button btn = objBtn.AddComponent<UnityEngine.UI.Button>();
        btn.targetGraphic = img;

        TextMeshProUGUI txtLabel = CreateTmpText(rt, "Label", _strLabel, 22f, TextAlignmentOptions.Center,
            Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
        txtLabel.fontStyle = FontStyles.Bold;

        return btn;
    }

    private UnityEngine.UI.Slider CreateSlider(RectTransform _rtParent, string _strName, Vector2 _vAnchoredPos, Vector2 _vSizeDelta, float _fInitialValue)
    {
        GameObject objSlider = new GameObject(_strName);
        objSlider.transform.SetParent(_rtParent, false);

        RectTransform rtSlider = objSlider.AddComponent<RectTransform>();
        rtSlider.anchorMin = new Vector2(0.5f, 0.5f);
        rtSlider.anchorMax = new Vector2(0.5f, 0.5f);
        rtSlider.pivot = new Vector2(0.5f, 0.5f);
        rtSlider.anchoredPosition = _vAnchoredPos;
        rtSlider.sizeDelta = _vSizeDelta;

        // Background
        GameObject objBg = CreateUiBox(rtSlider, "Background", new Color(0.22f, 0.25f, 0.3f, 1f),
            new Vector2(0f, 0.25f), new Vector2(1f, 0.75f), new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
        objBg.GetComponent<UnityEngine.UI.Image>().raycastTarget = true;

        // Fill Area
        GameObject objFillArea = new GameObject("Fill Area");
        objFillArea.transform.SetParent(rtSlider, false);
        RectTransform rtFillArea = objFillArea.AddComponent<RectTransform>();
        rtFillArea.anchorMin = new Vector2(0f, 0.25f);
        rtFillArea.anchorMax = new Vector2(1f, 0.75f);
        rtFillArea.offsetMin = new Vector2(5f, 0f);
        rtFillArea.offsetMax = new Vector2(-5f, 0f);

        GameObject objFill = CreateUiBox(rtFillArea, "Fill", new Color(0.25f, 0.68f, 0.95f, 1f),
            Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);

        // Handle Slide Area
        GameObject objHandleArea = new GameObject("Handle Slide Area");
        objHandleArea.transform.SetParent(rtSlider, false);
        RectTransform rtHandleArea = objHandleArea.AddComponent<RectTransform>();
        rtHandleArea.anchorMin = Vector2.zero;
        rtHandleArea.anchorMax = Vector2.one;
        rtHandleArea.offsetMin = new Vector2(10f, 0f);
        rtHandleArea.offsetMax = new Vector2(-10f, 0f);

        GameObject objHandle = CreateUiBox(rtHandleArea, "Handle", Color.white,
            new Vector2(0f, 0f), new Vector2(0f, 1f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(20f, 0f));
        UnityEngine.UI.Image imgHandle = objHandle.GetComponent<UnityEngine.UI.Image>();
        imgHandle.raycastTarget = true;

        UnityEngine.UI.Slider slider = objSlider.AddComponent<UnityEngine.UI.Slider>();
        slider.fillRect = objFill.GetComponent<RectTransform>();
        slider.handleRect = objHandle.GetComponent<RectTransform>();
        slider.targetGraphic = imgHandle;
        slider.direction = UnityEngine.UI.Slider.Direction.LeftToRight;
        slider.minValue = 0f;
        slider.maxValue = 1f;
        slider.value = _fInitialValue;

        return slider;
    }

    private UnityEngine.UI.Toggle CreateToggle(RectTransform _rtParent, string _strName, string _strLabel, Vector2 _vAnchoredPos, Vector2 _vSizeDelta, bool _bInitialState)
    {
        GameObject objToggle = new GameObject(_strName);
        objToggle.transform.SetParent(_rtParent, false);

        RectTransform rtToggle = objToggle.AddComponent<RectTransform>();
        rtToggle.anchorMin = new Vector2(0.5f, 0.5f);
        rtToggle.anchorMax = new Vector2(0.5f, 0.5f);
        rtToggle.pivot = new Vector2(0.5f, 0.5f);
        rtToggle.anchoredPosition = _vAnchoredPos;
        rtToggle.sizeDelta = _vSizeDelta;

        GameObject objBg = CreateUiBox(rtToggle, "Background", new Color(0.22f, 0.25f, 0.3f, 1f),
            new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(0f, 0f), new Vector2(30f, 30f));
        UnityEngine.UI.Image imgBg = objBg.GetComponent<UnityEngine.UI.Image>();
        imgBg.raycastTarget = true;

        GameObject objCheck = CreateUiBox(objBg.GetComponent<RectTransform>(), "Checkmark", new Color(0.95f, 0.35f, 0.35f, 1f),
            new Vector2(0.15f, 0.15f), new Vector2(0.85f, 0.85f), new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
        UnityEngine.UI.Image imgCheck = objCheck.GetComponent<UnityEngine.UI.Image>();

        CreateTmpText(rtToggle, "Label", _strLabel, 20f, TextAlignmentOptions.Left,
            new Vector2(0f, 0f), new Vector2(1f, 1f), new Vector2(0f, 0.5f), new Vector2(44f, 0f), new Vector2(-44f, 0f));

        UnityEngine.UI.Toggle toggle = objToggle.AddComponent<UnityEngine.UI.Toggle>();
        toggle.targetGraphic = imgBg;
        toggle.graphic = imgCheck;
        toggle.isOn = _bInitialState;

        return toggle;
    }
}
