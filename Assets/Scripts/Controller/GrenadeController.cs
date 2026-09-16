using System.Collections;
using System.Collections.Generic;
using UnityEngine;


namespace CrashyChasy
{
    public class GrenadeController : MonoBehaviour
    {
        //Public / Serialized variables
        public float explodeDelay = 1f;
        public float blastRadius =100f;
        public float scaleSpeed = 100f;
        public Vector3 targetScale = new Vector3(10, 10, 10); 

        [Range(0,3)]
        public int damageAmount = 2;

        //Private variables
        //Components
        private Collider[] overlapColliders;
        

        //Values
        private float countdown;
        private bool hasExploded = false;
        private bool attempExplode = false;
      

        // Start is called before the first frame update
        void Start()
        {
            countdown = explodeDelay;

            StartCoroutine(ShowUp());
        }

        // Update is called once per frame
        void Update()
        {
            if (attempExplode)
            {
                StartExploding();
            }
           
        }

        private void  StartExploding()
        {
            //Count down the time
            countdown -= Time.deltaTime;

            if (countdown <= 0 && !hasExploded)
            {
                overlapColliders = Physics.OverlapSphere(transform.position, blastRadius);

                foreach (var collider in overlapColliders)
                {
                   if (collider.CompareTag("Player"))
                   {
                       
                       var playerController = collider.gameObject.GetComponent<PlayerController>();
                       if (playerController != null)
                       {
                           playerController.TakeDamage(damageAmount);
                       }
                   }
                }
                
                EffectManager.instance.ShowVFX(EffectManager.instance.grenadeExplosion,transform.position,Quaternion.identity);

                if (SoundManager.Instance != null)
                    SoundManager.Instance.PlaySound(SoundManager.Instance.explode);
                Destroy(gameObject);
                hasExploded = true;
            }
        }

        private IEnumerator ShowUp()
        {
            float sqrScaleDif = (transform.localScale - targetScale).sqrMagnitude; 

            while (sqrScaleDif >= float.Epsilon)
            {
                transform.localScale = Vector3.MoveTowards(transform.localScale, targetScale, scaleSpeed * Time.deltaTime);
                sqrScaleDif = (transform.localScale - targetScale).sqrMagnitude;
                yield return null;
            }

            attempExplode = true;

        }
    }
}


