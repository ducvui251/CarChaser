using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using System.Collections;
using System;

#if EASY_MOBILE
using EasyMobile;
#endif

namespace CrashyChasy
{
    public class UIManager : MonoBehaviour
    {
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

        [Header("Premium Features Buttons")]
        public GameObject watchRewardedAdBtn;
        public GameObject leaderboardBtn;
        public GameObject achievementBtn;
        public GameObject shareBtn;
        public GameObject iapPurchaseBtn;
        public GameObject removeAdsBtn;
        public GameObject restorePurchaseBtn;

        [Header("In-App Purchase Store")]
        public GameObject storeUI;

        [Header("Sharing-Specific")]
        public GameObject shareUI;
        public ShareUIController shareUIController;

        Animator scoreAnimator;
        Animator dailyRewardAnimator;
        bool isWatchAdsForCoinBtnActive;

        private int currentHeartIndex;

        void OnEnable()
        {
            GameManager.GameStateChanged += GameManager_GameStateChanged;
            ScoreManager.ScoreUpdated += OnScoreUpdated;
            PlayerController.PlayerTakeDamage += OnPlayerTakeDamage;
            PlayerController.PlayerDied += OnPlayerDied;
        
        }

        void OnDisable()
        {
            GameManager.GameStateChanged -= GameManager_GameStateChanged;
            ScoreManager.ScoreUpdated -= OnScoreUpdated;
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
            scoreAnimator = score.GetComponent<Animator>();
            dailyRewardAnimator = dailyRewardBtn.GetComponent<Animator>();

           
        }

        // Update is called once per frame
        void Update()
        {
            score.text = ScoreManager.Instance.Score.ToString();
            bestScore.text = ScoreManager.Instance.HighScore.ToString();
            coinText.text = CoinManager.Instance.Coins.ToString();

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
            else if (newState == GameState.PreGameOver)
            {
                // Before game over, i.e. game potentially will be recovered
                Invoke("ShowRevivalUI", 2f);
            }
            else if (newState == GameState.GameOver)
            {
                Invoke("ShowGameOverUI", 2);
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

        void OnScoreUpdated(int newScore)
        {
            scoreAnimator.Play("NewScore");
        }

        void Reset()
        {
            revivalUI.SetActive(false);
            mainCanvas.SetActive(true);
            characterSelectionUI.SetActive(false);
            header.SetActive(false);
            title.SetActive(false);
            score.gameObject.SetActive(false);
            newBestScore.SetActive(false);
            playBtn.SetActive(false);
            menuButtons.SetActive(false);
            dailyRewardBtn.SetActive(false);
            foreach ( var h in hearts)
            {
                h.SetActive(false);
            }


            // Enable or disable premium stuff
            bool enablePremium = IsPremiumFeaturesEnabled();
            leaderboardBtn.SetActive(enablePremium);
            shareBtn.SetActive(enablePremium);
            iapPurchaseBtn.SetActive(enablePremium);
            removeAdsBtn.SetActive(enablePremium);
            restorePurchaseBtn.SetActive(enablePremium);

            // Hidden by default
            storeUI.SetActive(false);
            settingsUI.SetActive(false);
            shareUI.SetActive(false);

            // These premium feature buttons are hidden by default
            // and shown when certain criteria are met (e.g. rewarded ad is loaded)
            watchRewardedAdBtn.gameObject.SetActive(false);
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

            header.SetActive(true);
            title.SetActive(true);
            playBtn.SetActive(true);
            restartBtn.SetActive(false);
            menuButtons.SetActive(true);
            shareBtn.SetActive(false);
            revivalUI.SetActive(false);
            // If first launch: show "WatchForCoins" and "DailyReward" buttons if the conditions are met
            if (GameManager.GameCount == 0)
            {
                ShowWatchForCoinsBtn();
                ShowDailyRewardBtn();
            }
        }

        public void ShowGameUI()
        {
            header.SetActive(true);
            title.SetActive(false);
            score.gameObject.SetActive(true);
            playBtn.SetActive(false);
            menuButtons.SetActive(false);
            dailyRewardBtn.SetActive(false);
            watchRewardedAdBtn.SetActive(false);
            revivalUI.SetActive(false);
            for (int i = 0; i < GameManager.Instance.playerController.playerHealth;i++)
            {
                hearts[i].SetActive(true);
            }
        }

        public void ShowGameOverUI()
        {
            header.SetActive(true);
            title.SetActive(false);
            score.gameObject.SetActive(true);
            newBestScore.SetActive(ScoreManager.Instance.HasNewHighScore);

            playBtn.SetActive(false);
            restartBtn.SetActive(true);
            menuButtons.SetActive(true);
            settingsUI.SetActive(false);
            revivalUI.SetActive(false);

            // Show 'daily reward' button
            ShowDailyRewardBtn();

            // Show these if premium features are enabled (and relevant conditions are met)
            if (IsPremiumFeaturesEnabled())
            {
                ShowShareUI();
                ShowWatchForCoinsBtn();
            }
        }

        public void ShowRevivalUI()
        {
            revivalUI.SetActive(true);
        }

        public void HideRevivalUI()
        {
            revivalUI.SetActive(false);
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

        void ShowWatchForCoinsBtn()
        {
            // Only show "watch for coins button" if a rewarded ad is loaded and premium features are enabled
            #if EASY_MOBILE
        if (IsPremiumFeaturesEnabled() && AdDisplayer.Instance.CanShowRewardedAd() && AdDisplayer.Instance.watchAdToEarnCoins)
        {
            watchRewardedAdBtn.SetActive(true);
            watchRewardedAdBtn.GetComponent<Animator>().SetTrigger("activate");
        }
        else
        {
            watchRewardedAdBtn.SetActive(false);
        }
            #endif
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

        public void ShowStoreUI()
        {
            storeUI.SetActive(true);
        }

        public void HideStoreUI()
        {
            storeUI.SetActive(false);
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

        public void WatchRewardedAdToEarnCoin()
        {
            #if EASY_MOBILE
        // Hide the button
        watchRewardedAdBtn.SetActive(false);

        AdDisplayer.CompleteRewardedAdToEarnCoins += OnCompleteRewardedAdToEarnCoins;
        AdDisplayer.Instance.ShowRewardedAdToEarnCoins();
            #endif
        }

        void OnCompleteRewardedAdToEarnCoins()
        {
            #if EASY_MOBILE
        // Unsubscribe
        AdDisplayer.CompleteRewardedAdToEarnCoins -= OnCompleteRewardedAdToEarnCoins;

        // Give the coins!
        ShowRewardUI(AdDisplayer.Instance.rewardedCoins);
            #endif
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

        public void ShowLeaderboardUI()
        {
            #if EASY_MOBILE
        if (GameServices.IsInitialized())
        {
            GameServices.ShowLeaderboardUI();
        }
        else
        {
#if UNITY_IOS
            NativeUI.Alert("Service Unavailable", "The user is not logged in to Game Center.");
#elif UNITY_ANDROID
            GameServices.Init();
            #endif
        }
            #endif
        }

        public void ShowAchievementsUI()
        {
            #if EASY_MOBILE
        if (GameServices.IsInitialized())
        {
            GameServices.ShowAchievementsUI();
        }
        else
        {
#if UNITY_IOS
            NativeUI.Alert("Service Unavailable", "The user is not logged in to Game Center.");
#elif UNITY_ANDROID
            GameServices.Init();
            #endif
        }
            #endif
        }

        public void PurchaseRemoveAds()
        {
            #if EASY_MOBILE
        InAppPurchaser.Instance.Purchase(InAppPurchaser.Instance.removeAds);
            #endif
        }

        public void RestorePurchase()
        {
            #if EASY_MOBILE
        InAppPurchaser.Instance.RestorePurchase();
            #endif
        }

        public void ShowShareUI()
        {
            if (!ScreenshotSharer.Instance.disableSharing)
            {
                Texture2D texture = ScreenshotSharer.Instance.CapturedScreenshot;
                shareUIController.ImgTex = texture;

#if EASY_MOBILE
            AnimatedClip clip = ScreenshotSharer.Instance.RecordedClip;
            shareUIController.AnimClip = clip;
#endif

                shareUI.SetActive(true);
            }
        }

        public void HideShareUI()
        {
            shareUI.SetActive(false);
        }

        public void ToggleSound()
        {
            SoundManager.Instance.ToggleSound();
        }

        public void ToggleMusic()
        {
            SoundManager.Instance.ToggleMusic();
        }

        public void RateApp()
        {
            Utilities.RateApp();
        }

        public void OpenTwitterPage()
        {
            Utilities.OpenTwitterPage();
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

        bool IsPremiumFeaturesEnabled()
        {
            return PremiumFeaturesManager.Instance != null && PremiumFeaturesManager.Instance.enablePremiumFeatures;
        }

        public void WatchRewardedAdToRecover()
        {
#if EASY_MOBILE
#if !UNITY_EDITOR
            // Hide the button
            HideRevivalUI();

            AdDisplayer.CompleteRewardedAdToRecoverLostGame += OnCompleteRewardedAdToRecover;
            AdDisplayer.SkipedRewardedAdToRecoverLostGame += OnSkipRewardedAdToRecover;
            AdDisplayer.Instance.ShowRewardedAdToRecoverLostGame();
#else
            GameManager.Instance.RevivalGame();
            HideRevivalUI();
#endif
#endif
        }

        void OnCompleteRewardedAdToRecover()
        {
#if EASY_MOBILE
            // Unsubscribe
            AdDisplayer.CompleteRewardedAdToEarnCoins -= OnCompleteRewardedAdToRecover;
            AdDisplayer.SkipedRewardedAdToRecoverLostGame -= OnSkipRewardedAdToRecover;

            // Give the coins!

            GameManager.Instance.RevivalGame();
            HideRevivalUI();
#endif
        }

        void OnSkipRewardedAdToRecover()
        {
#if EASY_MOBILE
            // Unsubscribe
            AdDisplayer.CompleteRewardedAdToEarnCoins -= OnCompleteRewardedAdToRecover;
            AdDisplayer.SkipedRewardedAdToRecoverLostGame -= OnSkipRewardedAdToRecover;

            HideRevivalUI();
            GameManager.Instance.GameOver();
#endif
        }
    }
}