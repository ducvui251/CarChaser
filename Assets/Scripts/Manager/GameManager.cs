using UnityEngine;
using System.Collections.Generic;
using System.Collections;
using UnityEngine.SceneManagement;

namespace CrashyChasy
{
    public enum GameState
    {
        Prepare,
        Playing,
        Paused,
        PreGameOver,
        GameOver
    }

    public class GameManager : MonoBehaviour
    {
        public static GameManager Instance { get; private set; }

        public static event System.Action<GameState, GameState> GameStateChanged;

        public static event System.Action RevivalGameEvent = delegate { };

        private static bool isRestart;

        //Testing only
        //[Header("Test onlny")]
        //public GameObject enemyToSpawn;

        public GameState GameState
        {
            get
            {
                return _gameState;
            }
            private set
            {
                if (value != _gameState)
                {
                    GameState oldState = _gameState;
                    _gameState = value;

                    if (GameStateChanged != null)
                        GameStateChanged(_gameState, oldState);
                }
            }
        }

        public static int GameCount
        {
            get { return _gameCount; }
            private set { _gameCount = value; }
        }

        private static int _gameCount = 0;

        [Header("Set the target frame rate for this game")]
        [Tooltip("Use 60 for games requiring smooth quick motion, set -1 to use platform default frame rate")]
        public int targetFrameRate = 30;

        [Header("Current game state")]
        [SerializeField]
        private GameState _gameState = GameState.Prepare;

        // List of public variable for gameplay tweaking
        [Header("Gameplay Config")]
        [Header("Difficulty")]
        [SerializeField]
        [Range(1, 10)]
        private float increasingDiffValue;
        public float IncreasingDiffValue
        {
            get { return 12 - increasingDiffValue; }
        }
        [Header("Revival")]
        [SerializeField]
        private int revivalAmount = 1;
        private int currentRevivalAmount;

        [Header("Score")]
        [SerializeField]
        private float movingDistancePerScore = 10;
        public float MovingDistancePerScore
        {
            get { return movingDistancePerScore; }
            private set { movingDistancePerScore = value; }
        }

        [SerializeField]
        private int scorePerLoop = 15;
        public int ScorePerLoop
        {
            get { return scorePerLoop; }
            private set { scorePerLoop = value; }
        }

        [SerializeField]
        private int scorePerCar = 30;
        public int ScorePerCar
        {
            get { return scorePerCar; }
            private set { scorePerCar = value; }
        }

        [Header("Player")]
        [SerializeField]
        private Vector3 startPlayerPosition;
        public Vector3 StartPlayerPosition
        {
            get { return startPlayerPosition; }
            private set { startPlayerPosition = value; }
        }

        // List of public variables referencing other objects
        [Header("Object References")]
        public GameObject playerToSpawn;
        public PlayerController playerController;

        void OnEnable()
        {
            PlayerController.PlayerDied += PlayerController_PlayerDied;
            CharacterScroller.ChangeCharacter += CreateNewCharacter;
        }

        void OnDisable()
        {
            PlayerController.PlayerDied -= PlayerController_PlayerDied;
            CharacterScroller.ChangeCharacter -= CreateNewCharacter;
        }

        void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
            }
            else
            {
                DestroyImmediate(Instance.gameObject);
                Instance = this;
            }
            currentRevivalAmount = revivalAmount;
            CreateNewCharacter(CharacterManager.Instance.CurrentCharacterIndex);
        }

        void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
            }
        }

        // Use this for initialization
        void Start()
        {
            // Initial setup
            Application.targetFrameRate = targetFrameRate;
            ScoreManager.Instance.Reset();

            PrepareGame();
        }

        // Listens to the event when player dies and call GameOver
        void PlayerController_PlayerDied()
        {
#if EASY_MOBILE
#if !UNITY_EDITOR
            if (currentRevivalAmount > 0 && AdDisplayer.Instance.CanShowRewardedAd())
            {
                PreGameOver();
            }
            else
            {
                GameOver();
            }
#else
            if (currentRevivalAmount <= 0)
                GameOver();
            else
                PreGameOver();
    #endif
#else
            GameOver();
#endif
        }

        // Make initial setup and preparations before the game can be played
        public void PrepareGame()
        {
            GameState = GameState.Prepare;

            // Automatically start the game if this is a restart.
            if (isRestart)
            {
                isRestart = false;
                StartGame();
            }
        }

        // A new game official starts
        public void StartGame()
        {
            StartCoroutine(DelayStartGame());
        }

        IEnumerator DelayStartGame()
        {
            yield return new WaitForEndOfFrame();
            GameState = GameState.Playing;
            if (SoundManager.Instance.background != null)
            {
                SoundManager.Instance.PlayMusic(SoundManager.Instance.background);
            }
        }

        // Called when the player died
        public void GameOver()
        {
            if (SoundManager.Instance.background != null)
            {
                SoundManager.Instance.StopMusic();
            }

            SoundManager.Instance.PlaySound(SoundManager.Instance.gameOver);
            GameState = GameState.GameOver;
            GameCount++;

            // Add other game over actions here if necessary
        }

        public void PreGameOver()
        {
            GameState = GameState.PreGameOver;
            if (SoundManager.Instance.background != null)
            {
                SoundManager.Instance.StopMusic();
            }
        }

        // Start a new game
        public void RestartGame(float delay = 0)
        {
            isRestart = true;
            StartCoroutine(CRRestartGame(delay));
        }

        IEnumerator CRRestartGame(float delay = 0)
        {
            yield return new WaitForSeconds(delay);
            SceneManager.LoadScene(SceneManager.GetActiveScene().name);
        }

        public void HidePlayer()
        {
            if (playerController != null)
                playerController.gameObject.SetActive(false);
        }

        public void ShowPlayer()
        {
            if (playerController != null)
                playerController.gameObject.SetActive(true);
        }

        void CreateNewCharacter(int curChar)
        {
            if (playerController != null)
            {
                DestroyImmediate(playerController.gameObject);
                playerController = null;
            }
            StartCoroutine(CR_DelayCreateNewCharacter(curChar));
        }

        IEnumerator CR_DelayCreateNewCharacter(int curChar)
        {
            yield return new WaitForEndOfFrame();

            //Instantiate new player character from the character list
            Vector3 newCharOriginalPosition = CharacterManager.Instance.characters[curChar].transform.localPosition;
            GameObject newCharacter = Instantiate(CharacterManager.Instance.characters[curChar]);

            //Instantiate new player from player prefabs
            GameObject player = Instantiate(playerToSpawn);
            player.transform.position = startPlayerPosition;
            playerController = player.GetComponent<PlayerController>();

            //Destroy the old character
            var oldCharacter = player.transform.Find(CarController.CAR_MODEL_NAME);
            if (oldCharacter != null)
            {
                //We're using DestroyImmediate because this is effect in editor code, not in game logic code
                DestroyImmediate(oldCharacter.gameObject);
            }

            //Set the new player character
            newCharacter.name = CarController.CAR_MODEL_NAME;
            newCharacter.transform.SetParent(player.transform);
            newCharacter.transform.localPosition = newCharOriginalPosition;
            
            //Testing only
            //Instantiate(enemyToSpawn, enemyToSpawn.transform.position, Quaternion.identity);
        }

        public void RevivalGame()
        {
            currentRevivalAmount--;
            StartGame();
            RevivalGameEvent();
        }
    }
}