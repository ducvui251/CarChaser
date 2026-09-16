using UnityEngine;

namespace CrashyChasy
{
    public class Player : MonoBehaviour
    {
        private PathInit pathInit;

        private PathEnd pathEnd;

        public float moveSpeed;

        private void Update()
        {
            pathInit = PathInit.NONE;

            pathEnd = PathEnd.NONE;

            Move();
        }

        void Move()
        {
            Vector3 pos = Vector3.zero;

            if (Input.GetKeyDown(KeyCode.A))
            {
                pos = Vector3.left;
            }
            else if (Input.GetKeyDown(KeyCode.D))
            {
                pos = Vector3.right;
            }
            else if (Input.GetKeyDown(KeyCode.W) || Input.GetKeyDown(KeyCode.S))
            {
                pos = Vector3.up;
            }

            transform.position += pos;
        }

        private void OnTriggerEnter(Collider other)
        {
            if (other.CompareTag("DieCell"))
            {
                Debug.Log("die");
            }
        }

        private void OnTriggerExit(Collider other)
        {
            if (other.GetComponent<Cell>() != null)
            {
                other.GetComponent<Cell>().SetCellPathState(pathInit, pathEnd);
            }
        }
    }
}
