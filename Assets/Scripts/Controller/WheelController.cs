using System.Collections;
using System.Collections.Generic;
using UnityEngine;


namespace CrashyChasy
{
    public class WheelController : MonoBehaviour
    {

        public float rollingSpeed = 20f;
        
        // Start is called before the first frame update
        void Start()
        {
            
        }



        // Update is called once per frame
        void Update()
        {
            if (GameManager.Instance.GameState == GameState.Playing)
            {
                AutoRolling();
            }
        }


        private void AutoRolling()
        {
            transform.Rotate(Vector3.right * rollingSpeed, Space.Self);
        }


    }
}

