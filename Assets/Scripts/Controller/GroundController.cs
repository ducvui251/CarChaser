using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace CrashyChasy
{
    public class GroundController : MonoBehaviour
    {
        public static GroundController Instance { get; private set; }
        [Header("Ground")]
        public Material[] groundMaterials;

        [Range(100, 300)]
        public float groundSize = 150;

        private List<GameObject> groundList = new List<GameObject>();

        public Vector3 centerGroundPoint;

        [Header("Scroll Along Target")]
        public float padding = 54; //This is padding from the border to the player

        [Header("Spawn Enemies")]
        [HideInInspector]
        public int maxEnemyOnRoad;

        [SerializeField]
        private int maxEnemyInOneWave = 2;

        [SerializeField]
        private int maxEnemySpawn = 6;
    
        public GameObject[] enemiesToSpawn;
        [Header("Spawn obstacles")]
        public int maxObstacleOnRoad = 10;
        public GameObject[] obstaclesToSpawn;

        [Header("Spawn Values")]
        [SerializeField]
        private float fromScreenToEnemyZone = 29;//Offset between the screen camera to spawn enemies zone

        [SerializeField]
        private float maxSpawnZoneY = 100f;// The Y offset from the ground to spawn obstacle zone, the obstacle will falling when spawn fromm the sky

        [SerializeField]
        private float spawnDelayTime = 2f;

        [SerializeField]
        private bool drawEnemySpawnZone;//Visualize the enemy spawn zone in the  Scene (not in Game Scene)

        [Header("Object references")]
        public Camera mainCamera;
        public GameObject groundTemplate;

        public static int currentEnemiesCount = 0;
        public static int currentObstacleCount = 0;

        private MeshRenderer meshRenderer;

        //Border position
        private Vector3 maxPosition; //The top right corner of border
        private Vector3 minPosition;//The bottom left corner of border

        //Viewport point
        private Vector3 vpTopLeft;
        private Vector3 vpTopRight;
        private Vector3 vpBottomLeft;
        private Vector3 vpBottomRight;

        //Screen corner point
        private Vector3 topLeft; 
        private Vector3 topRight;
        private Vector3 bottomLeft;
        private Vector3 bottomRight;

        //Spawn zone is a rectangle where enemies is spawned on each edge
        private Vector3[] spawnZonePoints = new Vector3[4]; 

        //Reach border, determine whether the player reach the border
        private bool reachBorder = false;

        //Temp store data, to prevent unecessary initializing
        private Vector3 randomSpawnPosition;
        private Vector3 normalVector;
        private Vector3 currentOffset = Vector3.zero;

        private Vector3 topLeftObstacleZone;
        private Vector3 bottomRightObstacleZone;

        //How long the next wave
        private WaitForSeconds spawnWait;

        private void Awake()
        {
            if (Instance == null)
                Instance = this;
            else
                DestroyImmediate(gameObject);

        }
        // Start is called before the first frame update
        private void Start()
        {
            InitGroundContainer();

            InitializeValue();
            //ChangeGroundMaterial();

            CalculateBorder();//Calculate border in the first time

            StartCoroutine(CR_SpawnObjects(spawnWait));
        }

        // Update is called once per frame
        private void Update()
        {
           
            if (GameManager.Instance.playerController == null)
            {
                return;
            }
            
            if (GameManager.Instance.GameState == GameState.Playing)
            {
                //Update the ground position base on the player position
                UpdateGroundPosition(GameManager.Instance.playerController.transform.position);
                UpdateGroundListPosition(GameManager.Instance.playerController.transform.position);
            }
        }

        private void OnEnable()
        {
            CharacterScroller.ChangeCharacter += OnPlayerChangeCharacter;
        }

        private void OnDisable()
        {
            CharacterScroller.ChangeCharacter -= OnPlayerChangeCharacter;
        }

        private IEnumerator CR_SpawnObjects( WaitForSeconds spWait )
        {
            while (true)
            {
                if (GameManager.Instance.GameState == GameState.Playing)
                {
                    //Calculate four world point screen corners from camera
                    CalculateWorldPointScreenCorners();

                    //Calculate the spawn zone
                    CalculateSpawnZone();

                    //Spawn Enemies
                    SpawnEnemies();
                    
                    //Spawn obstacles
                    SpawnObstacles();
                }
                yield return spWait;
            }
        }

        private void SpawnEnemies()
        {

            //Random spawn zone line (two consecutive points)
            int first = Random.Range(0, spawnZonePoints.Length);
            int second = first + (Random.Range(0, 2) == 1 ? 1 : -1);

            //Check invalid index
            if (second == -1)
            {
                second = first + 1;
            }
            else if (second == spawnZonePoints.Length)
            {
                second = first - 1;
            }

            //Calculate max enemy on road base on the current score, using Mathf.Log
            maxEnemyOnRoad = Mathf.RoundToInt(Mathf.Log(ScoreManager.Instance.Score, GameManager.Instance.IncreasingDiffValue));
            if (maxEnemyOnRoad >= maxEnemySpawn)
                maxEnemyOnRoad = maxEnemySpawn;

            //Random the number of enemies
            int enemiesCount = Random.Range(1, maxEnemyInOneWave + 1);

            //Random position in two points
            for (int i = 0; i < enemiesCount; i++)
            {
                if (currentEnemiesCount >= maxEnemyOnRoad)
                {
                    break;
                }

                currentEnemiesCount++;

                //Random the x values
                randomSpawnPosition.x = Random.Range(spawnZonePoints[first].x, spawnZonePoints[second].x);

                //Set values of normal vector
                normalVector.Set(-(spawnZonePoints[first] - spawnZonePoints[second]).z, 0, (spawnZonePoints[first] - spawnZonePoints[second]).x);

                //The formular to calculate the z values is z = (n.x * xA - n.x*x + n.z*zA)/ n.z (line equation algorithms)
                //With n is a normal vector of vector created from two random points above, xA,zA is first random points values, x is a randomSpawn.x value
                //Caculate the z values formularly
                randomSpawnPosition.z =
                    (normalVector.x * spawnZonePoints[first].x - normalVector.x * randomSpawnPosition.x + normalVector.z * spawnZonePoints[first].z) / normalVector.z;

                GameObject enemyToSpawn = enemiesToSpawn[Random.Range(0, enemiesToSpawn.Length)];
                randomSpawnPosition.y = enemyToSpawn.transform.localPosition.y;
                Instantiate(enemyToSpawn, randomSpawnPosition, Quaternion.identity);
            }
        }

        private void SpawnObstacles()
        {
            if (currentObstacleCount > maxObstacleOnRoad) return;

           int obsLength = obstaclesToSpawn.Length;

           GameObject obstacle = obstaclesToSpawn[Random.Range(0, obsLength)];

           randomSpawnPosition.x = Random.Range(spawnZonePoints[3].x, spawnZonePoints[1].x);
           randomSpawnPosition.y = maxSpawnZoneY;
           randomSpawnPosition.z = Random.Range(spawnZonePoints[3].z, spawnZonePoints[1].z);

           Instantiate(obstacle, randomSpawnPosition, Quaternion.identity);

           currentObstacleCount++;
           
          
        }
        private void CalculateSpawnZone()
        {
            //Set spawn zone
            spawnZonePoints[0].Set(topLeft.x - fromScreenToEnemyZone, 0, topLeft.z + fromScreenToEnemyZone);
            spawnZonePoints[1].Set(topRight.x + fromScreenToEnemyZone, 0, topRight.z + fromScreenToEnemyZone);
            spawnZonePoints[2].Set(bottomRight.x + fromScreenToEnemyZone, 0, bottomRight.z - fromScreenToEnemyZone);
            spawnZonePoints[3].Set(bottomLeft.x - fromScreenToEnemyZone, 0, bottomLeft.z - fromScreenToEnemyZone);
        }

        private void ChangeGroundMaterial()
        {
            meshRenderer = GetComponent<MeshRenderer>();
            var randMaterial = groundMaterials[Random.Range(0, groundMaterials.Length)];

            //Change the ground parent material
            meshRenderer.material = randMaterial;

            //Change the ground child material
            int groundChildCount = transform.childCount;
            for (int i = 0 ; i < groundChildCount ; i++)
            {
                transform.GetChild(i).GetComponent<MeshRenderer>().material = randMaterial;
            }
        }

        private void CalculateWorldPointScreenCorners()
        {
            Plane plane = new Plane(Vector3.up, transform.position);
            Ray ray;
            float distance;

            //Top left
            ray = mainCamera.ViewportPointToRay(vpTopLeft);

            if (plane.Raycast(ray, out distance))
            {
                topLeft = ray.GetPoint(distance);
            }

            //Top right
            ray = mainCamera.ViewportPointToRay(vpTopRight);

            if (plane.Raycast(ray, out distance))
            {
                topRight = ray.GetPoint(distance);
            }

            //Bottom left
            ray = mainCamera.ViewportPointToRay(vpBottomLeft);

            if (plane.Raycast(ray, out distance))
            {
                bottomLeft = ray.GetPoint(distance);
            }

            //Bottom right
            ray = mainCamera.ViewportPointToRay(vpBottomRight);

            if (plane.Raycast(ray, out distance))
            {
                bottomRight = ray.GetPoint(distance);
            }
        }

        private void InitializeValue()
        {
           currentEnemiesCount = 0;
           currentObstacleCount = 0;
           maxEnemyOnRoad = 0;
           spawnWait = new WaitForSeconds(spawnDelayTime);

           for (int i = 0; i < 4; i++)
           {
               spawnZonePoints[i] = new Vector3();
           }

           float depth = (mainCamera.transform.position.y - transform.position.y) * 2;

           vpTopLeft = new Vector3(0, 1, depth);
           vpTopRight = new Vector3(1, 1, depth);
           vpBottomLeft = new Vector3(0, 0, depth);
           vpBottomRight = new Vector3(1, 0, depth);

           topLeft = new Vector3();
           topRight = new Vector3();
           bottomLeft = new Vector3();
           bottomRight = new Vector3();

           randomSpawnPosition = new Vector3();
           StartCoroutine(UpdateGroundWhenCreateNewCharacter());          
        }

        private void CalculateBorder()
        {
            //Calculate max position of the ground ( the top right corner of the ground)
            maxPosition.x = transform.position.x + transform.localScale.x / 2;
            maxPosition.y = transform.position.y;
            maxPosition.z = transform.position.z + transform.localScale.y / 2;

            //Calculate min position of the ground (the bottom left corder of the grouund)
            minPosition.x = transform.position.x - transform.localScale.x / 2;
            minPosition.y = transform.position.y;
            minPosition.z = transform.position.z - transform.localScale.y / 2;

        }

        //Update ground position
        private void UpdateGroundPosition(Vector3 relativePosition)
        {
            //Check if the player is outside the border
            if (relativePosition.x >= (maxPosition.x - padding) || relativePosition.z >= (maxPosition.z - padding))
            {
                //Reposition the ground
                if (relativePosition.x >= (maxPosition.x - padding))
                {
                    transform.position = new Vector3(maxPosition.x, transform.position.y, transform.position.z);
                }

                else if (relativePosition.z >= (maxPosition.z - padding))
                {
                    transform.position = new Vector3(transform.position.x, transform.position.y, maxPosition.z);
                }
                reachBorder = true;
            }

            //The same as min position
            if (relativePosition.x <= (minPosition.x + padding) || relativePosition.z <= (minPosition.z + padding))
            {
                //Reposition the ground
                if (relativePosition.x <= (minPosition.x + padding))
                {
                    transform.position = new Vector3(minPosition.x, transform.position.y, transform.position.z);
                }

                else if (relativePosition.z <= (minPosition.z + padding))
                {
                    transform.position = new Vector3(transform.position.x, transform.position.y, minPosition.z);
                }
                reachBorder = true;
            }

            if (reachBorder)
            {
                //Recalculate the max and min border position
                CalculateBorder();
            }
            reachBorder = false;
        }

        private void OnPlayerChangeCharacter(int cur)
        {
            StartCoroutine(UpdateGroundWhenCreateNewCharacter());

            if (GameManager.Instance.GameState == GameState.GameOver)
            {
                centerGroundPoint = GameManager.Instance.StartPlayerPosition;
                ClearGroundContainer();
                InitGroundContainer();
            }
        }

        private IEnumerator UpdateGroundWhenCreateNewCharacter()
        {
            yield return new WaitForSeconds(0.05f);

            if (currentOffset != Vector3.zero)
            {
                transform.position = GameManager.Instance.playerController.transform.position + currentOffset;
            }
            currentOffset = transform.position - GameManager.Instance.playerController.transform.position;
        }

        private void OnDrawGizmos()
        {
            if (drawEnemySpawnZone)
            {
                DrawRect(Color.red, spawnZonePoints);
            }

        }
       
        private void DrawRect(Color drawColor,params Vector3[] rect)
        {
            Gizmos.color = drawColor;

            for (int i = 0; i < rect.Length -1;i++)
            {
               //Debug.Log("Point " + i + " " +  rect[i]);
               Gizmos.DrawLine(rect[i], rect[i + 1]);
            }

            Gizmos.DrawLine(rect[rect.Length-1],rect[0]);
        }

        private void InitGroundContainer()
        {
            var currentGroundMaterial = groundMaterials[Random.Range(0, groundMaterials.Length)];

            GameObject groundContainer = new GameObject("GroundContainer");
            groundContainer.transform.localScale = Vector3.one;
            groundContainer.transform.position = Vector3.zero;
            centerGroundPoint = GameManager.Instance.StartPlayerPosition;
            for (int i = -1; i < 2; i++)
            {
                for (int j = -1; j < 2; j++)
                {
                    Vector3 newPos = centerGroundPoint;
                    newPos.x = centerGroundPoint.x + groundSize * j;
                    newPos.z = centerGroundPoint.z + groundSize * i;
                    newPos.y = centerGroundPoint.y - 2;
                    GameObject ground = Instantiate(groundTemplate, newPos, Quaternion.Euler(90, 0, 0), groundContainer.transform);
                    ground.transform.localScale = new Vector3(groundSize, groundSize, 1);
                    ground.GetComponent<MeshRenderer>().material = currentGroundMaterial;
                    groundList.Add(ground);
                }
            }
        }

        private void UpdateGroundListPosition(Vector3 playerPosition)
        {
            Vector3 distanceFromCenter = playerPosition - centerGroundPoint;
            if (Mathf.Abs(distanceFromCenter.x) < groundSize / 2 && Mathf.Abs(distanceFromCenter.z) < groundSize / 2)
                return;
            for (int i = 0; i < groundList.Count; i++)
            {
                if (playerPosition.x - groundList[i].transform.position.x >= (groundSize + groundSize/2))
                {
                    groundList[i].transform.position = new Vector3(groundList[i].transform.position.x + groundSize * 3, groundList[i].transform.position.y, groundList[i].transform.position.z);
                }
                else
                {
                    if (playerPosition.x - groundList[i].transform.position.x <= (-groundSize - groundSize/2))
                    {
                        groundList[i].transform.position = new Vector3(groundList[i].transform.position.x - groundSize * 3, groundList[i].transform.position.y, groundList[i].transform.position.z);
                    }
                }

                if (playerPosition.z - groundList[i].transform.position.z >= (groundSize + groundSize / 2))
                {
                    groundList[i].transform.position = new Vector3(groundList[i].transform.position.x, groundList[i].transform.position.y, groundList[i].transform.position.z + groundSize * 3);
                }
                else
                {
                    if (playerPosition.z - groundList[i].transform.position.z <= (-groundSize - groundSize / 2))
                    {
                        groundList[i].transform.position = new Vector3(groundList[i].transform.position.x, groundList[i].transform.position.y, groundList[i].transform.position.z - groundSize * 3);
                    }
                }
            }

            if (Mathf.Abs(distanceFromCenter.x) >= groundSize / 2)
                centerGroundPoint.x += Mathf.Clamp(distanceFromCenter.x, -groundSize / 2, groundSize / 2);

            if (Mathf.Abs(distanceFromCenter.z) >= groundSize / 2)
                centerGroundPoint.z += Mathf.Clamp(distanceFromCenter.z, -groundSize / 2, groundSize / 2);
        }

        private void ClearGroundContainer()
        {
            if (groundList.Count > 0)
                Destroy(groundList[0].transform.gameObject);
        }
    }
}



