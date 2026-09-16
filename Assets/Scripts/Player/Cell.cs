using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace CrashyChasy
{
    public class Cell : MonoBehaviour
    {
        [SerializeField]
        private SpriteRenderer path;

        [SerializeField]
        private GameObject cube;

        public CellState cellState;

        public CellGenerate cellGenerate
        {
            get
            {
                return CellGenerate;
            }
            private set
            {
                CellGenerate = value;

                switch (CellGenerate)
                {
                    case CellGenerate.YES_0:
                    case CellGenerate.YES_1:
                    case CellGenerate.YES_2:
                    case CellGenerate.YES_3:
                        path.gameObject.SetActive(true);
                        cube.SetActive(false);
                        break;
                    default:
                        path.gameObject.SetActive(false);
                        cube.SetActive(false);
                        break;
                }
            }
        }

        private CellGenerate CellGenerate;

        public PathInit pathInit
        {
            get
            {
                return PathInit;
            }
            private set
            {
                PathInit = value;

                switch (PathInit)
                {
                    case PathInit.UP:
                        path.sprite = ResourceController.Instance.up;
                        break;
                    case PathInit.LEFT:
                        path.sprite = ResourceController.Instance.left;
                        break;
                    case PathInit.RIGHT:
                        path.sprite = ResourceController.Instance.right;
                        break;
                    default:
                        if (pathEnd == PathEnd.NONE)
                        {
                            path.gameObject.SetActive(false);
                        }
                        break;
                }
            }
        }

        private PathInit PathInit;

        public PathEnd pathEnd
        {
            get
            {
                return PathEnd;
            }
            private set
            {
                PathEnd = value;

                switch (PathEnd)
                {
                    case PathEnd.LEFTUP:
                        if (pathInit == PathInit.LEFT)
                        {
                            path.sprite = ResourceController.Instance.leftUp;
                        }
                        break;
                    case PathEnd.RIGHTUP:
                        if (pathInit == PathInit.RIGHT)
                        {
                            path.sprite = ResourceController.Instance.rightUp;
                        }
                        break;
                    case PathEnd.NORMAL:
                        path.sprite = ResourceController.Instance.normal;
                        break;
                    default:
                        if (pathInit == PathInit.NONE)
                        {
                            path.gameObject.SetActive(false);
                        }
                        break;
                }
            }
        }

        private PathEnd PathEnd;

        public CellColor cellColor
        {
            get
            {
                return CellColor;
            }
            private set
            {
                CellColor = value;

                switch (CellColor)
                {
                    case CellColor.BRIGHT:
                        cube.GetComponent<Renderer>().material = ResourceController.Instance.bright;
                        path.gameObject.SetActive(true);
                        path.GetComponent<Renderer>().material = ResourceController.Instance.bright;
                        break;
                    case CellColor.FADE:
                        cube.GetComponent<Renderer>().material = ResourceController.Instance.fade;
                        path.gameObject.SetActive(true);
                        path.GetComponent<Renderer>().material = ResourceController.Instance.fade;
                        break;
                    case CellColor.BRIGHTRED:
                        cube.GetComponent<Renderer>().material = ResourceController.Instance.brightRed;
                        path.gameObject.SetActive(false);
                        break;
                    case CellColor.FADERED:
                        cube.GetComponent<Renderer>().material = ResourceController.Instance.fadeRed;
                        path.gameObject.SetActive(false);
                        break;
                }
            }
        }

        private CellColor CellColor;

        public void SetCellColor(CellColor color)
        {
            cellColor = color;
        }

        public void SetCellState(CellState state)
        {
            cellState = state;
        }

        public void SetCellPathState(PathInit pathInit, PathEnd pathEnd)
        {
            this.pathInit = pathInit;

            this.pathEnd = pathEnd;
        }

        /*public void SetCellGenerate(CellGenerate cell)
        {
            cellGenerate = cell;
        }*/
    }
}
