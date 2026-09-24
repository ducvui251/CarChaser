using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace CarChaser
{
    public class AutoDestroy : MonoBehaviour
    {

        public float destroyTime = 4f;

        // Start is called before the first frame update
        void Start()
        {
            Destroy(gameObject, destroyTime);
        }
    }
}
