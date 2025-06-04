using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [Header("Managers")]
    public SFXManager sfxManager; // 인스펙터에서 SFXManager 오브젝트 연결

    [Header("UI Prefabs")]
    public GameObject menuPrefab; // ESC 메뉴 UI 프리팹
    public GameObject gameOverUIPrefab; // 게임 오버 UI 프리팹
    public GameObject gameClearUIPrefab; // 게임 클리어 UI 프리팹

    private GameObject _currentMenuInstance;
    private GameObject _currentGameOverUIInstance;
    private GameObject _currentGameClearUIInstance;

    public bool IsPaused { get; private set; } = false;

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject); // 씬이 변경되어도 파괴되지 않도록 설정

            // SFXManager가 자식으로 이미 존재한다면 여기서 찾아 할당할 수도 있습니다.
            // 또는 인스펙터에서 직접 할당합니다.
            if (sfxManager == null)
            {
                sfxManager = GetComponentInChildren<SFXManager>();
            }
        }
        else if (Instance != this)
        {
            Destroy(gameObject);
            return;
        }
    }

    void Start()
    {
        // 초기 BGM 재생 (예: 타이틀 씬이라면 타이틀 BGM)
        if (SceneManager.GetActiveScene().name == "TitleScene" && sfxManager != null) // 씬 이름은 실제 사용하는 이름으로 변경
        {
            sfxManager.PlayBGM(sfxManager.mainBgm);
        }
    }


    void Update()
    {
        // ESC 키로 메뉴 열고 닫기 (GameScene에서만 작동하도록 조건 추가 가능)
        if (SceneManager.GetActiveScene().name == "GameScene" && Input.GetKeyDown(KeyCode.Escape))
        {
            if (_currentMenuInstance == null && _currentGameOverUIInstance == null && _currentGameClearUIInstance == null) // 다른 UI가 없을 때만 메뉴 열기
            {
                OpenMenu();
            }
            else if (_currentMenuInstance != null)
            {
                CloseMenu();
            }
        }
    }

    // --- 씬 관리 ---
    public void LoadTitleScene()
    {
        Time.timeScale = 1f; // 게임 시간 정상화
        IsPaused = false;
        CloseAllPopups(); // 모든 팝업 UI 닫기
        SceneManager.LoadScene("TitleScene"); // "TitleScene"은 실제 씬 이름으로 변경
        if (sfxManager != null) sfxManager.PlayBGM(sfxManager.mainBgm);
    }

    public void LoadGameScene()
    {
        Time.timeScale = 1f; // 게임 시간 정상화
        IsPaused = false;
        CloseAllPopups();
        SceneManager.LoadScene("GameScene"); // "GameScene"은 실제 씬 이름으로 변경
        if (sfxManager != null) sfxManager.PlayBGM(sfxManager.gameSceneBgm);
    }

    public void RestartGame()
    {
        Time.timeScale = 1f;
        IsPaused = false;
        CloseAllPopups();
        SceneManager.LoadScene("GameScene"); // 또는 SceneManager.LoadScene(SceneManager.GetActiveScene().name);
        if (sfxManager != null) sfxManager.PlayBGM(sfxManager.gameSceneBgm);
        // 추가적으로 플레이어, 보스 위치/상태 초기화 로직이 필요하다면 여기서 호출하거나,
        // 각 캐릭터의 Awake/Start에서 처리
        // 아니면 PlayerController나 BossController를 찾아서 ResetStats() 같은 메서드를 호출.
    }

    // --- UI 관리 ---
    public void OpenMenu()
    {
        if (menuPrefab == null)
        {
            return;
        }
        if (_currentMenuInstance == null)
        {
            _currentMenuInstance = Instantiate(menuPrefab);
            // 메뉴 UI 내 버튼들에 필요한 메서드 연결 (예: UI 스크립트에서 GameManager.Instance 참조)
            Time.timeScale = 0f; // 게임 일시 정지
            IsPaused = true;
            if (sfxManager != null) sfxManager.PlayButtonClickSound();
        }
    }

    public void CloseMenu()
    {
        if (_currentMenuInstance != null)
        {
            Destroy(_currentMenuInstance);
            _currentMenuInstance = null;
            Time.timeScale = 1f; // 게임 재개
            IsPaused = false;
            if (sfxManager != null) sfxManager.PlayButtonClickSound();
        }
    }

    public void ShowGameOverUI()
    {
        if (gameOverUIPrefab == null)
        {
            return;
        }
        if (_currentGameOverUIInstance == null)
        {
            _currentGameOverUIInstance = Instantiate(gameOverUIPrefab);
            Time.timeScale = 0f; // 게임 일시 정지 (선택적)
            IsPaused = true;
        }
    }

    public void ShowGameClearUI()
    {
        if (gameClearUIPrefab == null)
        {
            return;
        }
        if (_currentGameClearUIInstance == null)
        {
            _currentGameClearUIInstance = Instantiate(gameClearUIPrefab);
            Time.timeScale = 0f; // 게임 일시 정지 (선택적)
            IsPaused = true;
        }
    }

    private void CloseAllPopups()
    {
        if (_currentMenuInstance != null) Destroy(_currentMenuInstance);
        if (_currentGameOverUIInstance != null) Destroy(_currentGameOverUIInstance);
        if (_currentGameClearUIInstance != null) Destroy(_currentGameClearUIInstance);
        _currentMenuInstance = null;
        _currentGameOverUIInstance = null;
        _currentGameClearUIInstance = null;
    }


    public void QuitGame()
    {
        Debug.Log("게임 종료");
        Application.Quit(); // 에디터에서는 작동 안 함, 빌드된 게임에서만 작동
        UnityEditor.EditorApplication.isPlaying = false; // 에디터에서 플레이 모드 종료
    }
}
