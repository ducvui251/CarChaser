using UnityEngine;
using System.Collections;

namespace CarChaser
{
    public class CameraController : MonoBehaviour
    {
        
        public float smoothing = 5f;

        private Vector3 offset;
        private Transform playerTransform;

        [Header("Camera Follow Smooth-Time")]
        public float smoothTime = 0.1f;

        [Header("Shaking Effect")]
        // How long the camera shaking.
    public float shakeDuration = 0.1f;
        // Amplitude of the shake. A larger value shakes the camera harder.
        public float shakeAmount = 0.2f;
        public float decreaseFactor = 0.3f;
        [HideInInspector]
        public Vector3 originalPos;

        private float currentShakeDuration;
        private float currentDistance;

        private bool isShaking;

        void OnEnable()
        {
            CharacterScroller.ChangeCharacter += ChangeCharacter;
        }

        void OnDisable()
        {
            CharacterScroller.ChangeCharacter -= ChangeCharacter;
        }

        void Start()
        {
            StartCoroutine(WaitingPlayerController());
        }

        void Update()
        {
            //Debug.Log(playerTransform);

            if (GameManager.Instance.GameState == GameState.Playing)
            {
                FollowTarget();
            }
        }

        public void ShakeStaticCamera()
        {
            StartCoroutine(Shake());
        }

        public void ShakeMovingCamera()
        {
            StartCoroutine(ShakingWhileMoving());//This just simply set the isShaking to true, when the shaking work is carried by the FollowTarget function
        }

        private void FollowTarget()
        {

            if (playerTransform == null) return;

            Vector3 targetCamPos = playerTransform.position + offset;

            transform.position = Vector3.Lerp(transform.position, targetCamPos, smoothing * Time.deltaTime)
                + (isShaking == true ? Random.insideUnitSphere * shakeAmount : Vector3.zero);

        }

        IEnumerator Shake()
        {

            yield return null;

            originalPos = transform.position;
            currentShakeDuration = shakeDuration;
            while (currentShakeDuration > 0)
            {
                transform.position = originalPos + Random.insideUnitSphere * shakeAmount;
                currentShakeDuration -= Time.deltaTime * decreaseFactor;
                yield return null;
            }
            transform.position = originalPos;
        }

        //Just simply set the isShaking to true and wait for the shake is over then set back to false, 
        //the shaking work will be carried by the FollowTarget function
        private IEnumerator ShakingWhileMoving()
        {
            currentShakeDuration = shakeDuration;

            while (currentShakeDuration > 0)
            {

                isShaking = true;

                currentShakeDuration -= Time.deltaTime * decreaseFactor;

                yield return null;
            }

            isShaking = false;
        }

        void ChangeCharacter(int cur)
        {
            StartCoroutine(WaitingPlayerController());
        }

        IEnumerator WaitingPlayerController()
        {
            yield return new WaitForSeconds(0.05f);

            float remainingWait = 2f;
            while ((GameManager.Instance == null || GameManager.Instance.playerController == null) && remainingWait > 0f)
            {
                remainingWait -= Time.unscaledDeltaTime;
                yield return null;
            }

            if (GameManager.Instance == null || GameManager.Instance.playerController == null)
                yield break;

            playerTransform = GameManager.Instance.playerController.transform;

            if (offset != Vector3.zero)
            {
                transform.position = playerTransform.position + offset; 
            }
            offset = transform.position - playerTransform.transform.position;
           
        }
    }
}
