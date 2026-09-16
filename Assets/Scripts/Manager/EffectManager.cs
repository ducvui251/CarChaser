using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace CrashyChasy
{
    public class EffectManager : MonoBehaviour
    {
        public static EffectManager instance = null;

        //Properties
        //Count the destroy to make sure the effect only called once
        public int destroyCount { get; set; }

        //References
        [Header("References")]
        public GameObject cam;
        public GameObject coin;
        
        //Effects to instantiate
        [Header("Effect to instantiate")]
        public GameObject carExplosion;
        public GameObject grenadeExplosion;
        public GameObject coinCollectedEffect;
        public GameObject fireSpark;
        public GameObject impactSpark;

        private CameraController camController;

        void OnEnable()
        {
            PlayerController.PlayerTakeDamage += OnPlayerDamage;
        }

        void OnDisable()
        {
            PlayerController.PlayerTakeDamage -= OnPlayerDamage;
        }

        
        private void Awake()
        {
            if (instance == null)
            {
                instance = this;
            }
            else if (instance != this)
            {
                Destroy(gameObject);
            }

            camController = cam.GetComponent<CameraController>();
        }

        // Start is called before the first frame update
        void Start()
        {

        }

        private void ShakingCamera(string targetTag)
        {
            switch (targetTag)
            {
                case "Player":
                    camController.ShakeStaticCamera();
                    break;
                case "Enemy":
                    camController.ShakeMovingCamera();
                    break;
            }
        }

       private void OnPlayerDamage(int amount)
       {
           camController.ShakeMovingCamera();
        }

        public void StartDestroyEffect(Collider collider)
        {
            if (destroyCount == 1) //Make sure the destroy effect start only once when there're two same collider tag
            {
                //Shaking camera base on the collider
                //ShakingCamera(collider.tag);

                //Show a destroy visual destroy effect
                if (carExplosion != null)
                {
                    Instantiate(carExplosion, collider.transform.position, Quaternion.identity);
                }

                if (collider.CompareTag("Enemy")) //Check if enemy destroy each other 
                {
                   //Instantiate a coin (only one coin is spawn for two enemy
                    Instantiate(coin, collider.transform.position, Quaternion.identity);
                }
            }
            else
            {
                destroyCount = 0;
            }
        }

        public void StartCoinEffect(Transform coinTransform)
        {
            Instantiate(coinCollectedEffect, coinTransform.position, Quaternion.identity);
        }

        public void ShowVFX(GameObject vfx,Vector3 pos, Quaternion rot)
        {
            if (vfx != null)
                Instantiate(vfx, pos, rot);
            
        }

        public void SpawnCoin(Vector3 pos, Quaternion rot)
        {
            if (destroyCount == 1)
            {
                Instantiate(coin, pos, rot);
            }
            else
            {
                destroyCount = 0; 
            }

        }

        public void ShakeStaticCamera()
        {
            camController.ShakeStaticCamera();
        }

    }

}
