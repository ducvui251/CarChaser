using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace CrashyChasy
{
    public class ResourceController : MonoBehaviour
    {
        public static ResourceController Instance;
        [HideInInspector]
        public Material bright;
        [HideInInspector]
        public Material fade;
        [HideInInspector]
        public Material brightRed;
        [HideInInspector]
        public Material fadeRed;

        public Sprite up;
        public Sprite right;
        public Sprite left;
        public Sprite rightUp;
        public Sprite leftUp;
        public Sprite normal;

        private void Awake()
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

            bright = Resources.Load("Materials/BrightYellow", typeof(Material)) as Material;
            fade = Resources.Load("Materials/FadeYellow", typeof(Material)) as Material;
            brightRed = Resources.Load("Materials/BrightRed", typeof(Material)) as Material;
            fadeRed = Resources.Load("Materials/FadeRed", typeof(Material)) as Material;
        }

    }
}
