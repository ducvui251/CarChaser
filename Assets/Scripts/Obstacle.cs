using System.Collections;
using System.Collections.Generic;
using UnityEngine;


namespace CrashyChasy
{
   
    [RequireComponent(typeof(Collider))]
    [RequireComponent(typeof(Rigidbody))]
    public class Obstacle : MonoBehaviour
    {
        [Range(0, 3)]
        public int damageAmount = 1;
        public float flyAwayForce = 100f;
        public float spinningSpeed = 8f;
        public float fallingSpeed = 30f;
        public float groundPositionY;

        private Collider obsCollider;
        private Rigidbody obsRb;
        private Vector3 targetPos;

        private bool isFlying = false;
        private float remainingDistance;
        private float yModelHaflSize;

        private const float epsilon = 0.01f;
       
        // Start is called before the first frame update
        void Start()
        {
            obsCollider = GetComponent<Collider>();
            obsRb = GetComponent<Rigidbody>();

            yModelHaflSize = obsCollider.bounds.size.y / 2;

            remainingDistance = transform.position.y - groundPositionY - yModelHaflSize;    

            //Debug.Log("Remaning distance: " + remainingDistance);
            targetPos = transform.position;

            //Debug.Log("obs collider: " + obsCollider.bounds.size.y);
        }

        private void OnTriggerEnter(Collider other)
        {

            if (other.CompareTag("Player"))
            {
                var playerController = other.GetComponent<PlayerController>();
                if (playerController != null)
                    playerController.TakeDamage(damageAmount);

                FlyAway(other.transform);
            }
        }

        private void OnTriggerExit(Collider other)
        {
           if (other.CompareTag("Boundary"))
           {
               Destroy(gameObject);
           }
        }

        private void FixedUpdate()
        {
            AutoFalling();
        }

        private void AutoFalling()
        {
            remainingDistance = transform.position.y - groundPositionY - yModelHaflSize;
            //Debug.Log("Collider: " + obsCollider.enabled);    

            if (remainingDistance <= epsilon)
            {
                if (!isFlying)
                {
                    obsCollider.enabled = true;
                    return;
                }
            }

            if (!isFlying)
            {
                obsCollider.enabled = false;
                targetPos.y = Mathf.MoveTowards(targetPos.y, groundPositionY + yModelHaflSize, fallingSpeed * Time.deltaTime);
                transform.position = targetPos;

            }
            
        }

        private void FlyAway(Transform otherTransform)
        {
            isFlying = true;
            obsCollider.enabled = false;

            EffectManager.instance.ShowVFX(EffectManager.instance.impactSpark, transform.position, Quaternion.identity);
            SoundManager.Instance.PlaySound(SoundManager.Instance.flyAway, false, false, 0.75f);

            Vector3 forceDirection = (otherTransform.forward + transform.up);

            obsRb.AddForce(forceDirection * flyAwayForce, ForceMode.Impulse);
            StartCoroutine(Spining(forceDirection));
         
            //Destroy the gameobject
            Destroy(gameObject, 2f);
        }

        private IEnumerator Spining(Vector3 axis)
        {
            while (true)
            {
                transform.Rotate(axis * spinningSpeed, Space.World);
                yield return null;
            }
        }

        private void OnDestroy()
        {
            GroundController.currentObstacleCount--;
        }
    }
}

