using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace CrashyChasy
{
    public class NearmissManager : MonoBehaviour
    {
        // Start is called before the first frame update
        void Start()
        {

        }

        private void OnTriggerExit(Collider other)
        {
            if (other.CompareTag("Enemy"))
            {
                //Start nearmiss implement here
                Debug.Log("Nearmiss!!");
            }
        }
    }
}
