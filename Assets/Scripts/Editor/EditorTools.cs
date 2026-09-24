using UnityEngine;
using UnityEditor;
using System.Collections;

namespace CarChaser
{
    public class EditorTools
    {
        [MenuItem("Tools/Reset PlayerPrefs", false)]
        public static void ResetPlayerPref()
        {
            PlayerPrefs.DeleteAll();
            Debug.Log("*** PlayerPrefs was reset! ***");
        }

        [MenuItem("Tools/Reset High Score", false)]
        public static void ResetHighScore()
        {
            PlayerPrefs.DeleteKey("HIGHSCORE");
            PlayerPrefs.Save();
            if (ScoreManager.Instance != null)
            {
                ScoreManager.Instance.ResetHighScore();
            }
            Debug.Log("*** High Score was reset to 0! ***");
        }
    }
}
