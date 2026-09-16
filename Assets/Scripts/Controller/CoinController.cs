using UnityEngine;
using System.Collections;

namespace CrashyChasy
{
    public class CoinController : MonoBehaviour
    {

        private bool stop;
        // Use this for initialization
        private void Start()
        {
            StartCoroutine(Bounce());
            StartCoroutine(Rotate());

            Destroy(gameObject, 8f);

        }

        private void Update()
        {
            if (GameManager.Instance.GameState == GameState.GameOver)
            {
                GetComponent<AutoDestroy>().enabled = true;
            }
        }

        public void GoUp()
        {
            stop = true;
            StartCoroutine(Up());
        }

        IEnumerator Rotate()
        {
            while (true)
            {
                transform.Rotate(transform.up * 2f,Space.Self);
                yield return null;
            }
        }

        IEnumerator Bounce()
        {
            while (true)
            {
                float bounceTime = 1f;

                float startY = transform.position.y;
                float endY = startY + 0.5f;

                float t = 0;
                while (t < bounceTime / 2f)
                {
                    if (stop)
                        yield break;
                    t += Time.deltaTime;
                    float fraction = t / (bounceTime / 2f);
                    float newY = Mathf.Lerp(startY, endY, fraction);
                    Vector3 newPos = transform.position;
                    newPos.y = newY;
                    transform.position = newPos;
                    yield return null;
                }

                float r = 0;
                while (r < bounceTime / 2f)
                {
                    if (stop)
                        yield break;
                    r += Time.deltaTime;
                    float fraction = r / (bounceTime / 2f);
                    float newY = Mathf.Lerp(endY, startY, fraction);
                    Vector3 newPos = transform.position;
                    newPos.y = newY;
                    transform.position = newPos;
                    yield return null;
                }
            }        
        }

        //Move up
        IEnumerator Up()
        {
            float time = 1f;

            float startY = transform.position.y;
            float endY = startY + 10f;

            float t = 0;
            while (t < time / 2f)
            {
                t += Time.deltaTime;
                float fraction = t / (time / 2f);
                float newY = Mathf.Lerp(startY, endY, fraction);
                Vector3 newPos = transform.position;
                newPos.y = newY;
                transform.position = newPos;
                yield return null;
            }

            gameObject.SetActive(false);
            GetComponent<MeshCollider>().enabled = true;
            transform.position = Vector3.zero;
            transform.parent = CoinManager.Instance.transform;
        }

        private void OnTriggerEnter(Collider other)
        {
            if (other.CompareTag("Player"))
            {
                EffectManager.instance.StartCoinEffect(transform);
                SoundManager.Instance.PlaySound(SoundManager.Instance.coin, false, false, 0.75f);

                Destroy(gameObject);
            }
        }

        private void OnDestroy()
        {
            
            CoinManager.Instance.AddCoins(1);
        }
    }
}