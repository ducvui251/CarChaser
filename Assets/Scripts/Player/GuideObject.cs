using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace CrashyChasy
{
    public class GuideObject : MonoBehaviour
    {
        //[SerializeField]
        //private BoardGame board;

        public int step;

        public int minStep;

        public int maxStep;

        public float moveSpeed;

        public float maxDelayStep;

        public float delayForThinking;

        private float delayThink;

        private float delayStep;

        bool hasStartOnNewLine;

        PathInit pathInit
        {
            get
            {
                return PathInit;
            }
            set
            {
                PathInit = value;

                switch (PathInit)
                {
                    case PathInit.UP:
                        transform.position += Vector3.up;
                        hasStartOnNewLine = false;
                        break;
                    case PathInit.LEFT:
                        transform.position += Vector3.left;
                        hasStartOnNewLine = true;
                        break;
                    case PathInit.RIGHT:
                        transform.position += Vector3.right;
                        hasStartOnNewLine = true;
                        break;
                }
            }
        }
        [SerializeField]
        private PathInit PathInit;

        PathEnd pathEnd
        {
            get
            {
                return PathEnd;
            }
            set
            {
                PathEnd = value;

                switch (PathEnd)
                {
                    case PathEnd.RIGHTUP:
                    case PathEnd.LEFTUP:
                        transform.position += Vector3.up;
                        hasStartOnNewLine = false;
                        break;
                    case PathEnd.NORMAL:
                        if (hasStartOnNewLine)
                        {
                            if (pathInit == PathInit.LEFT)
                            {
                                transform.position += Vector3.left;
                            }
                            else
                            {
                                transform.position += Vector3.right;
                            }
                        }
                        break;
                    default:
                        break;
                }
            }
        }
        [SerializeField]
        private PathEnd PathEnd;

        private void Awake()
        {
            hasStartOnNewLine = false;

            delayStep = maxDelayStep;

            delayThink = delayForThinking;

            pathEnd = PathEnd.NONE;

            pathInit = PathInit.NONE;

            //board = GameObject.FindGameObjectWithTag("BoardGame").GetComponent<BoardGame>();
        }

        private void Update()
        {
            SelfMove();
        }

        void SelfMove()
        {
            if (step <= 0)
            {
                if (delayThink <= 0)
                {
                    step = Random.Range(minStep, maxStep);

                    delayThink = delayForThinking;

                    return;
                }

                delayThink -= Time.deltaTime;
            }

            if (step > 0)
            {
                if (delayStep <= 0)
                {
                    if (!hasStartOnNewLine)
                    {
                        PathInit init = GetRandomEnum<PathInit>();

                        while (init == PathInit.NONE)
                        {
                            init = GetRandomEnum<PathInit>();
                        }

                        pathInit = init;

                        pathEnd = PathEnd.NONE;
                    }
                    else
                    {
                        PathEnd end = GetRandomEnum<PathEnd>();

                        while (end == PathEnd.NONE)
                        {
                            end = GetRandomEnum<PathEnd>();
                        }

                        pathEnd = end;
                    }

                    step -= 1;

                    delayStep = maxDelayStep;

                    return;
                }

                delayStep -= Time.deltaTime;
            }
        }

        T GetRandomEnum<T>()
        {
            System.Array A = System.Enum.GetValues(typeof(T));

            return (T)A.GetValue(Random.Range(0, A.Length));
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
