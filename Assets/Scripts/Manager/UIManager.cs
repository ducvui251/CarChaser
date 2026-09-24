using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using System.Collections;
using System;

namespace CarChaser
{
    public class UIManager : MonoBehaviour
    {
        public static UIManager Instance { get; private set; }

        [Header("Object References")]
        public GameObject mainCanvas;
        public GameObject characterSelectionUI;
        public GameObject header;
        public GameObject title;
        public Text score;
        public Text bestScore;
        public Text coinText;
        public GameObject newBestScore;
        public GameObject playBtn;
        public GameObject restartBtn;
        public GameObject menuButtons;
        public GameObject dailyRewardBtn;
        public Text dailyRewardBtnText;
        public GameObject rewardUI;
        public GameObject settingsUI;
        public GameObject soundOnBtn;
        public GameObject soundOffBtn;
        public GameObject musicOnBtn;
        public GameObject musicOffBtn;
        public GameObject[] hearts;
        public GameObject revivalUI;
        private Text revivalBestScore;
        Animator scoreAnimator;
        Animator dailyRewardAnimator;
        private int currentHeartIndex;
        private Transform bestScoreOriginalParent;
        private bool bestScoreInRevivalPanel;
        private int bestScoreOriginalFontSize;
        private Color bestScoreOriginalColor;
        private TextAnchor bestScoreOriginalAlignment;
        private HorizontalWrapMode bestScoreOriginalHorizontalOverflow;
        private VerticalWrapMode bestScoreOriginalVerticalOverflow;
        private Vector2 bestScoreOriginalAnchorMin;
        private Vector2 bestScoreOriginalAnchorMax;
        private Vector2 bestScoreOriginalPivot;
        private Vector2 bestScoreOriginalSizeDelta;
        private Vector2 bestScoreOriginalAnchoredPosition;
        private RectTransform revivalPromptRect;
        private const int RevivalTextFontSize = 32;
        private Vector2 revivalPromptOriginalAnchorMin;
        private Vector2 revivalPromptOriginalAnchorMax;
        private Vector2 revivalPromptOriginalPivot;
        private Vector2 revivalPromptOriginalSizeDelta;
        private Vector2 revivalPromptOriginalAnchoredPosition;
        private int revivalPromptOriginalFontSize;
        private TextAnchor revivalPromptOriginalAlignment;
        private HorizontalWrapMode revivalPromptOriginalHorizontalOverflow;
        private VerticalWrapMode revivalPromptOriginalVerticalOverflow;
        private bool revivalPromptLayoutCached;

        void OnEnable()
        {
            if (Instance == null)
                Instance = this;
            else if (Instance != this)
                Destroy(gameObject);

            GameManager.GameStateChanged += GameManager_GameStateChanged;
            GameManager.RevivalGameEvent += OnPlayerRevived;
            ScoreManager.ScoreUpdated += OnScoreUpdated;
            ScoreManager.HighscoreUpdated += OnHighscoreUpdated;
            PlayerController.PlayerTakeDamage += OnPlayerTakeDamage;
            PlayerController.PlayerDied += OnPlayerDied;

        }

        void OnDisable()
        {
            if (Instance == this)
                Instance = null;

            GameManager.GameStateChanged -= GameManager_GameStateChanged;
            GameManager.RevivalGameEvent -= OnPlayerRevived;
            ScoreManager.ScoreUpdated -= OnScoreUpdated;
            ScoreManager.HighscoreUpdated -= OnHighscoreUpdated;
            PlayerController.PlayerTakeDamage -= OnPlayerTakeDamage;
            PlayerController.PlayerDied -= OnPlayerDied;

        }

        // Use this for initialization
        void Start()
        {
            InitializeValue();

            Reset();
            ShowStartUI();
        }

        private void InitializeValue()
        {
            scoreAnimator = score != null ? score.GetComponent<Animator>() : null;
            if (scoreAnimator != null)
            {
                scoreAnimator.enabled = false;
            }
            if (score != null)
            {
                score.rectTransform.localScale = Vector3.one;
            }
            if (bestScore != null)
            {
                bestScore.rectTransform.localScale = Vector3.one;
                bestScoreOriginalParent = bestScore.transform.parent;
                bestScoreInRevivalPanel = false;
                bestScoreOriginalFontSize = bestScore.fontSize;
                bestScoreOriginalColor = bestScore.color;
                bestScoreOriginalAlignment = bestScore.alignment;
                bestScoreOriginalHorizontalOverflow = bestScore.horizontalOverflow;
                bestScoreOriginalVerticalOverflow = bestScore.verticalOverflow;
                bestScoreOriginalAnchorMin = bestScore.rectTransform.anchorMin;
                bestScoreOriginalAnchorMax = bestScore.rectTransform.anchorMax;
                bestScoreOriginalPivot = bestScore.rectTransform.pivot;
                bestScoreOriginalSizeDelta = bestScore.rectTransform.sizeDelta;
                bestScoreOriginalAnchoredPosition = bestScore.rectTransform.anchoredPosition;
            }
            CacheRevivalPromptLayout();
            dailyRewardAnimator = dailyRewardBtn.GetComponent<Animator>();

            UpdateScoreDisplay();
        }

        public void UpdateScoreDisplay()
        {
            if (ScoreManager.Instance != null)
            {
                if (score != null)
                {
                    string scoreStr = ScoreManager.Instance.Score.ToString();
                    if (score.text != scoreStr)
                        score.text = scoreStr;
                }
                if (bestScore != null)
                {
                    string bestStr = ScoreManager.Instance.HighScore.ToString();
                    string bestDisplay = bestScoreInRevivalPanel ? $"BEST SCORE: {bestStr}" : bestStr;
                    if (bestScore.text != bestDisplay)
                        bestScore.text = bestDisplay;
                }
                if (revivalBestScore != null)
                    revivalBestScore.text = $"BEST SCORE: {ScoreManager.Instance.HighScore}";
            }

            if (CoinManager.Instance != null && coinText != null)
            {
                string coinStr = CoinManager.Instance.Coins.ToString();
                if (coinText.text != coinStr)
                    coinText.text = coinStr;
            }
        }

        // Update is called once per frame
        void Update()
        {
            UpdateScoreDisplay();

            if (!DailyRewardController.Instance.disable && dailyRewardBtn.gameObject.activeInHierarchy)
            {
                if (DailyRewardController.Instance.CanRewardNow())
                {
                    dailyRewardBtnText.text = "GRAB YOUR REWARD!";
                    dailyRewardAnimator.SetTrigger("activate");
                }
                else
                {
                    TimeSpan timeToReward = DailyRewardController.Instance.TimeUntilReward;
                    dailyRewardBtnText.text = string.Format("REWARD IN {0:00}:{1:00}:{2:00}", timeToReward.Hours, timeToReward.Minutes, timeToReward.Seconds);
                    dailyRewardAnimator.SetTrigger("deactivate");
                }
            }

            if (settingsUI.activeSelf)
            {
                UpdateSoundButtons();
                UpdateMusicButtons();
            }
        }

        void GameManager_GameStateChanged(GameState newState, GameState oldState)
        {
            if (newState == GameState.Playing)
            {
                //Initialize all the value when we need a player reference
                InitializeBeforePlay();

                //Show game UI
                ShowGameUI();
            }
            else if (newState == GameState.GameOver)
            {
                Invoke("ShowGameOverUI", 2);
            }
            else if (newState == GameState.PreGameOver)
            {
                Invoke("ShowRevivalUI", 2);
            }
        }

        void InitializeBeforePlay()
        {
            currentHeartIndex = GameManager.Instance.playerController.playerHealth - 1;
        }

        void OnPlayerDied()
        {
            foreach (var h in hearts)
            {
                h.SetActive(false);
            }
        }

        void OnPlayerRevived()
        {
            RefreshHearts();
        }

        public void RefreshHearts()
        {
            if (GameManager.Instance != null && GameManager.Instance.playerController != null && hearts != null)
            {
                int health = GameManager.Instance.playerController.playerHealth;
                for (int i = 0; i < hearts.Length; i++)
                {
                    hearts[i].SetActive(i < health);
                }
                currentHeartIndex = health - 1;
            }
        }

        void OnScoreUpdated(int newScore)
        {
            UpdateScoreDisplay();
        }

        void OnHighscoreUpdated(int newHighScore)
        {
            if (bestScore != null)
            {
                bestScore.text = bestScoreInRevivalPanel
                    ? $"BEST SCORE: {newHighScore}"
                    : newHighScore.ToString();
            }
        }

        void Reset()
        {
            mainCanvas.SetActive(true);
            characterSelectionUI.SetActive(false);
            header.SetActive(false);
            title.SetActive(false);
            score.gameObject.SetActive(false);
            RestoreBestScoreToHeader();
            if (bestScore != null)
                bestScore.gameObject.SetActive(false);
            newBestScore.SetActive(false);
            playBtn.SetActive(false);
            menuButtons.SetActive(false);
            dailyRewardBtn.SetActive(false);
            revivalUI.SetActive(false);
            foreach ( var h in hearts)
            {
                h.SetActive(false);
            }
            settingsUI.SetActive(false);

        }

        public void StartGame()
        {
            GameManager.Instance.StartGame();
        }

        public void EndGame()
        {
            GameManager.Instance.GameOver();
        }

        public void RestartGame()
        {
            GameManager.Instance.RestartGame(0.2f);
        }

        public void ShowStartUI()
        {
            settingsUI.SetActive(false);
            RestoreBestScoreToHeader();

            header.SetActive(true);
            title.SetActive(true);
            // Show the actual best (time) on the start screen
            if (bestScore != null) bestScore.gameObject.SetActive(true);
            playBtn.SetActive(true);
            restartBtn.SetActive(false);
            menuButtons.SetActive(true);
            revivalUI.SetActive(false);
            // If first launch: show the daily reward button if available
            if (GameManager.GameCount == 0)
            {
                ShowDailyRewardBtn();
            }

            UpdateScoreDisplay();
        }

        public void ShowGameUI()
        {
            RestoreBestScoreToHeader();
            header.SetActive(true);
            title.SetActive(false);
            score.gameObject.SetActive(true);
            if (bestScore != null)
                bestScore.gameObject.SetActive(false);
            playBtn.SetActive(false);
            menuButtons.SetActive(false);
            dailyRewardBtn.SetActive(false);
            revivalUI.SetActive(false);
            if (GameManager.Instance != null && GameManager.Instance.playerController != null && hearts != null)
            {
                for (int i = 0; i < GameManager.Instance.playerController.playerHealth; i++)
                {
                    if (i < hearts.Length && hearts[i] != null)
                    {
                        hearts[i].SetActive(true);
                    }
                }
            }

            UpdateScoreDisplay();
        }

        public void ShowGameOverUI()
        {
            RestoreBestScoreToHeader();
            header.SetActive(true);
            title.SetActive(false);
            score.gameObject.SetActive(true);
            if (bestScore != null)
                bestScore.gameObject.SetActive(true);
            newBestScore.SetActive(ScoreManager.Instance.HasNewHighScore);

            playBtn.SetActive(false);
            restartBtn.SetActive(true);
            menuButtons.SetActive(true);
            settingsUI.SetActive(false);
            revivalUI.SetActive(false);
            // Show 'daily reward' button
            ShowDailyRewardBtn();

            UpdateScoreDisplay();
        }

        private void OnPlayerTakeDamage(int amount)
        {
            for (int i = 0; i < amount; i++ )
            {
                hearts[currentHeartIndex].SetActive(false);
                currentHeartIndex--;
                if (currentHeartIndex < 0)
                {
                    currentHeartIndex = 0;
                    return;
                }
            }
        }

        void ShowDailyRewardBtn()
        {
            // Not showing the daily reward button if the feature is disabled
            if (!DailyRewardController.Instance.disable)
            {
                dailyRewardBtn.SetActive(true);
            }
        }

        public void ShowSettingsUI()
        {
            settingsUI.SetActive(true);
        }

        public void HideSettingsUI()
        {
            settingsUI.SetActive(false);
        }

        public void ShowCharacterSelectionScene()
        {
            mainCanvas.SetActive(false);
            characterSelectionUI.SetActive(true);
        }

        public void CloseCharacterSelectionScene()
        {
            mainCanvas.SetActive(true);
            characterSelectionUI.SetActive(false);
        }

        public void ShowRevivalUI()
        {
            revivalUI.SetActive(true);
            MoveBestScoreToRevivalPanel();

            if (bestScore != null)
                bestScore.gameObject.SetActive(false);
        }

        public void HideRevivalUI()
        {
            RestoreBestScoreToHeader();
            RestoreRevivalPromptLayout();
            if (bestScore != null)
                bestScore.gameObject.SetActive(false);
            revivalUI.SetActive(false);
        }

        private void MoveBestScoreToRevivalPanel()
        {
            if (revivalUI == null)
                return;

            Transform container = revivalUI.transform.Find("Container");
            if (container == null)
                return;
            RectTransform containerRect = container as RectTransform;
            Text prompt = container.Find("Text")?.GetComponent<Text>();
            Text reviveTitle = container.Find("ReviveTitle")?.GetComponent<Text>();
            Transform buttonRow = container.Find("ReviveButtonRow");
            Transform reviveButton = buttonRow != null ? buttonRow.Find("RevivalBtn") : container.Find("RevivalBtn");
            Transform exitButton = buttonRow != null ? buttonRow.Find("ExitBtn") : container.Find("ExitBtn");
            if (prompt == null || reviveButton == null || exitButton == null)
                return;

            VerticalLayoutGroup layout = container.GetComponent<VerticalLayoutGroup>();
            if (layout == null)
                layout = container.gameObject.AddComponent<VerticalLayoutGroup>();

            layout.padding = new RectOffset(5, 5, 5, 5);
            layout.spacing = 5f;
            layout.childAlignment = TextAnchor.MiddleCenter;
            layout.childControlWidth = true;
            layout.childControlHeight = false;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;

            if (reviveTitle != null)
                reviveTitle.gameObject.SetActive(false);
            ConfigureLayoutElement(prompt.rectTransform, 24f);
            prompt.alignment = TextAnchor.MiddleCenter;
            prompt.horizontalOverflow = HorizontalWrapMode.Wrap;
            prompt.verticalOverflow = VerticalWrapMode.Truncate;

            if (revivalBestScore == null)
            {
                GameObject scoreObject = new GameObject("RevivalBestScore", typeof(RectTransform), typeof(CanvasRenderer), typeof(Text), typeof(LayoutElement));
                scoreObject.transform.SetParent(container, false);
                revivalBestScore = scoreObject.GetComponent<Text>();
                revivalBestScore.font = prompt.font;
                revivalBestScore.fontSize = RevivalTextFontSize;
                revivalBestScore.alignment = TextAnchor.MiddleCenter;
                revivalBestScore.color = new Color(1f, 0.84f, 0.4f, 1f);
                revivalBestScore.raycastTarget = false;
                revivalBestScore.verticalOverflow = VerticalWrapMode.Overflow;
                revivalBestScore.rectTransform.sizeDelta = new Vector2(
                    revivalBestScore.rectTransform.sizeDelta.x, 36f);
            }
            ConfigureLayoutElement(revivalBestScore.rectTransform, 36f);

            if (buttonRow == null)
            {
                GameObject rowObject = new GameObject("ReviveButtonRow", typeof(RectTransform), typeof(HorizontalLayoutGroup), typeof(LayoutElement));
                rowObject.transform.SetParent(container, false);
                buttonRow = rowObject.transform;
                reviveButton.SetParent(buttonRow, false);
                exitButton.SetParent(buttonRow, false);
            }

            HorizontalLayoutGroup buttonLayout = buttonRow.GetComponent<HorizontalLayoutGroup>();
            buttonLayout.spacing = 5f;
            buttonLayout.childAlignment = TextAnchor.MiddleCenter;
            buttonLayout.childControlWidth = false;
            buttonLayout.childControlHeight = false;
            buttonLayout.childForceExpandWidth = false;
            buttonLayout.childForceExpandHeight = false;
            ConfigureLayoutElement(reviveButton as RectTransform, 70f, 150f);
            ConfigureLayoutElement(exitButton as RectTransform, 70f, 150f);
            ConfigureLayoutElement(buttonRow as RectTransform, 70f);

            revivalBestScore.transform.SetSiblingIndex(0);
            prompt.transform.SetSiblingIndex(1);
            buttonRow.SetSiblingIndex(2);
            revivalBestScore.text = ScoreManager.Instance != null ? $"BEST SCORE: {ScoreManager.Instance.HighScore}" : "BEST SCORE";
            LayoutRebuilder.ForceRebuildLayoutImmediate(container as RectTransform);
        }

        private static void ConfigureLayoutElement(RectTransform rect, float height, float width = -1f)
        {
            LayoutElement element = rect.GetComponent<LayoutElement>();
            if (element == null)
                element = rect.gameObject.AddComponent<LayoutElement>();

            element.minHeight = height;
            element.preferredHeight = height;
            if (width > 0f)
            {
                element.minWidth = width;
                element.preferredWidth = width;
            }
        }

        private void CacheRevivalPromptLayout()
        {
            if (revivalPromptLayoutCached || revivalUI == null)
                return;

            Transform container = revivalUI.transform.Find("Container");
            Transform prompt = container != null ? container.Find("Text") : null;
            if (prompt == null)
                return;

            revivalPromptRect = prompt.GetComponent<RectTransform>();
            if (revivalPromptRect == null)
                return;

            revivalPromptOriginalAnchorMin = revivalPromptRect.anchorMin;
            revivalPromptOriginalAnchorMax = revivalPromptRect.anchorMax;
            revivalPromptOriginalPivot = revivalPromptRect.pivot;
            revivalPromptOriginalSizeDelta = revivalPromptRect.sizeDelta;
            revivalPromptOriginalAnchoredPosition = revivalPromptRect.anchoredPosition;

            Text promptText = revivalPromptRect.GetComponent<Text>();
            if (promptText != null)
            {
                revivalPromptOriginalFontSize = promptText.fontSize;
                revivalPromptOriginalAlignment = promptText.alignment;
                revivalPromptOriginalHorizontalOverflow = promptText.horizontalOverflow;
                revivalPromptOriginalVerticalOverflow = promptText.verticalOverflow;
            }
            revivalPromptLayoutCached = true;
        }

        private void RestoreRevivalPromptLayout()
        {
            if (!revivalPromptLayoutCached || revivalPromptRect == null)
                return;

            revivalPromptRect.anchorMin = revivalPromptOriginalAnchorMin;
            revivalPromptRect.anchorMax = revivalPromptOriginalAnchorMax;
            revivalPromptRect.pivot = revivalPromptOriginalPivot;
            revivalPromptRect.sizeDelta = revivalPromptOriginalSizeDelta;
            revivalPromptRect.anchoredPosition = revivalPromptOriginalAnchoredPosition;

            Text promptText = revivalPromptRect.GetComponent<Text>();
            if (promptText != null)
            {
                promptText.fontSize = revivalPromptOriginalFontSize;
                promptText.alignment = revivalPromptOriginalAlignment;
                promptText.horizontalOverflow = revivalPromptOriginalHorizontalOverflow;
                promptText.verticalOverflow = revivalPromptOriginalVerticalOverflow;
            }
        }

        private void RestoreBestScoreToHeader()
        {
            if (bestScore == null)
                return;

            if (bestScoreOriginalParent != null && bestScore.transform.parent != bestScoreOriginalParent)
                bestScore.rectTransform.SetParent(bestScoreOriginalParent, false);

            bestScore.rectTransform.anchorMin = bestScoreOriginalAnchorMin;
            bestScore.rectTransform.anchorMax = bestScoreOriginalAnchorMax;
            bestScore.rectTransform.pivot = bestScoreOriginalPivot;
            bestScore.rectTransform.sizeDelta = bestScoreOriginalSizeDelta;
            bestScore.rectTransform.anchoredPosition = bestScoreOriginalAnchoredPosition;

            bestScoreInRevivalPanel = false;
            bestScore.fontSize = bestScoreOriginalFontSize;
            bestScore.color = bestScoreOriginalColor;
            bestScore.alignment = bestScoreOriginalAlignment;
            bestScore.horizontalOverflow = bestScoreOriginalHorizontalOverflow;
            bestScore.verticalOverflow = bestScoreOriginalVerticalOverflow;
            UpdateScoreDisplay();
        }

        public void GrabDailyReward()
        {
            if (DailyRewardController.Instance.CanRewardNow())
            {
                int reward = DailyRewardController.Instance.GetRandomReward();

                // Round the number and make it mutiplies of 5 only.
                int roundedReward = (reward / 5) * 5;

                // Show the reward UI
                ShowRewardUI(roundedReward);

                // Update next time for the reward
                DailyRewardController.Instance.ResetNextRewardTime();
            }
        }

        public void ShowRewardUI(int reward)
        {
            rewardUI.SetActive(true);
            rewardUI.GetComponent<RewardUIController>().Reward(reward);
        }

        public void HideRewardUI()
        {
            rewardUI.GetComponent<RewardUIController>().Close();
        }

        public void ReviveForFree()
        {
            GameManager.Instance.RevivalGame();
            HideRevivalUI();
        }

        public void ToggleSound()
        {
            SoundManager.Instance.ToggleSound();
        }

        public void ToggleMusic()
        {
            SoundManager.Instance.ToggleMusic();
        }

        public void OpenFacebookPage()
        {
            Utilities.OpenFacebookPage();
        }

        public void ButtonClickSound()
        {
            Utilities.ButtonClickSound();
        }

        void UpdateSoundButtons()
        {
            if (SoundManager.Instance.IsSoundOff())
            {
                soundOnBtn.gameObject.SetActive(false);
                soundOffBtn.gameObject.SetActive(true);
            }
            else
            {
                soundOnBtn.gameObject.SetActive(true);
                soundOffBtn.gameObject.SetActive(false);
            }
        }

        void UpdateMusicButtons()
        {
            if (SoundManager.Instance.IsMusicOff())
            {
                musicOffBtn.gameObject.SetActive(true);
                musicOnBtn.gameObject.SetActive(false);
            }
            else
            {
                musicOffBtn.gameObject.SetActive(false);
                musicOnBtn.gameObject.SetActive(true);
            }
        }

    }
}
