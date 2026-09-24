using UnityEngine;
using System;
using System.Collections;

namespace CarChaser
{
    public class ScoreManager : MonoBehaviour
    {
        private static ScoreManager instance;
        public static ScoreManager Instance
        {
            get
            {
                if (instance == null)
                {
                    instance = FindFirstObjectByType<ScoreManager>();
                }
                return instance;
            }
            private set
            {
                instance = value;
            }
        }

        public int Score { get; private set; }

        public int HighScore { get; private set; }

        public bool HasNewHighScore { get; private set; }

        public static event Action<int> ScoreUpdated = delegate {};
        public static event Action<int> HighscoreUpdated = delegate {};

        private const string HIGHSCORE = "HIGHSCORE";
        // key name to store high score in PlayerPrefs
      
        private float surviveTime;
        private int lastAwardedSecond;

        void Awake()
        {
            if (instance != null && instance != this)
            {
                DestroyImmediate(gameObject);
            }
            else
            {
                instance = this;
                DontDestroyOnLoad(gameObject);
                HighScore = PlayerPrefs.GetInt(HIGHSCORE, 0);
            }
        }

        void OnDestroy()
        {
            if (instance == this)
            {
                instance = null;
            }
        }

        void Start()
        {
            Reset();
        }

        public void Reset()
        {
            // Initialize score
            Score = 0;
            surviveTime = 0f;
            lastAwardedSecond = 0;

            // Initialize highscore from PlayerPrefs
            HighScore = PlayerPrefs.GetInt(HIGHSCORE, 0);
            HasNewHighScore = false;
        }

        public void AddScore(int amount)
        {
            if (amount <= 0) return;

            Score += amount;

            // Fire event
            ScoreUpdated(Score);

            if (Score > HighScore)
            {
                UpdateHighScore(Score);
                HasNewHighScore = true;
            }
        }

        public void AddSurvivalTime(float dt)
        {
            if (dt <= 0f) return;
            if (GameManager.Instance.GameState != GameState.Playing) return;
            surviveTime += dt;

            int currentSeconds = Mathf.FloorToInt(surviveTime);
            if (currentSeconds > lastAwardedSecond)
            {
                int secondsToAdd = currentSeconds - lastAwardedSecond;
                lastAwardedSecond = currentSeconds;
                AddScore(secondsToAdd);
            }
        }

        public int GetSurvivalSeconds()
        {
            return Mathf.FloorToInt(surviveTime);
        }

        public void SetScoreToSurvivalTime()
        {
            // Deprecated: survival points are now accumulated incrementally in AddSurvivalTime()
            // to preserve bonus scores (enemy crashes, donut loops) and prevent score value bouncing.
        }

        public void UpdateHighScore(int newHighScore)
        {
            // Update highscore if player has made a new one
            if (newHighScore > HighScore)
            {
                HighScore = newHighScore;
                PlayerPrefs.SetInt(HIGHSCORE, HighScore);
                PlayerPrefs.Save();
                HighscoreUpdated(HighScore);
            }
        }

        public void ResetHighScore()
        {
            HighScore = 0;
            Score = 0;
            HasNewHighScore = false;
            PlayerPrefs.DeleteKey(HIGHSCORE);
            PlayerPrefs.Save();
            HighscoreUpdated(0);
        }
    }
}
