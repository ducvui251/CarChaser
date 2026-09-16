using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace CrashyChasy
{
    public class BoardGame : MonoBehaviour
    {
        public GameObject cellPrefab;

        public GameObject dieCellPrefab;

        public GameObject player;

        public GameObject guideObject;

        public int minHeight;

        public int maxHeight;

        public int length;

        private List<Cell[]> cells;

        private int posY;

        private void Awake()
        {
            cells = new List<Cell[]>();

            posY = -1;

            InitGameStart();
        }

        void InitGameStart()
        {
            InitCellBoard(true);

            InitObject();

            InitCellBoard(false);

            InitBoardGame();
        }

        void InitCellBoard(bool isDeadCell)
        {
            for (int i = 0; i < length; i++)
            {
                if (isDeadCell)
                {
                    GameObject dieCell = Instantiate(dieCellPrefab, SetPosition(i), Quaternion.identity);

                    dieCell.transform.parent = transform;

                    continue;
                }
                else
                {
                    GameObject cell = Instantiate(cellPrefab, SetPosition(i), Quaternion.identity);

                    cell.transform.parent = transform;

                    if (i % 2 == 0)
                    {
                        cell.GetComponent<Cell>().SetCellColor(CellColor.BRIGHTRED);
                    }
                    else
                    {
                        cell.GetComponent<Cell>().SetCellColor(CellColor.FADERED);
                    }

                    cell.GetComponent<Cell>().SetCellState(CellState.FINISH);

                    cell.GetComponent<Cell>().SetCellPathState(PathInit.NONE, PathEnd.NONE);
                }
            }

            posY += 1;
        }

        void InitObject()
        {
            int posX = Random.Range(1, length - 1);

            Vector3 pos = new Vector3(posX, posY, -0.5f);

            Instantiate(player, pos, Quaternion.identity);

            pos.y += 1;

            Instantiate(guideObject, pos, Quaternion.identity);
        }

        void InitBoardGame()
        {
            int height = Random.Range(minHeight, maxHeight);

            for (int h = 0; h < height; h++)
            {
                Cell[] arrCell = new Cell[length];

                for (int w = 0; w < length; w++)
                {
                    if (w == 0 || w == length - 1)
                    {
                        GameObject dieCell = Instantiate(dieCellPrefab, SetPosition(w, h), Quaternion.identity);

                        dieCell.transform.parent = transform;

                        continue;
                    }

                    GameObject cell = Instantiate(cellPrefab, SetPosition(w, h), Quaternion.identity);

                    cell.transform.parent = transform;

                    arrCell[w] = cell.GetComponent<Cell>();

                    //arrCell[w].SetCellGenerate(GetRandomEnum<CellGenerate>());

                    if ((w + h) % 2 == 0)
                    {
                        arrCell[w].SetCellColor(CellColor.BRIGHT);
                    }
                    else
                    {
                        arrCell[w].SetCellColor(CellColor.FADE);
                    }

                    arrCell[w].SetCellState(CellState.NOTPATH);
                }

                cells.Add(arrCell);
            }

            posY += height;
        }

        Vector3 SetPosition(int i, int j = 0)
        {
            return new Vector3(i, j + posY, 0);
        }
    }
}
