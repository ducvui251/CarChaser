using UnityEngine;
using System.Collections;
using System.Collections.Generic;

namespace CrashyChasy
{
    public class PlayerController : CarController
    {
        public static event System.Action PlayerDied;

        public static event System.Action<int> PlayerTakeDamage;

        [Header("Object references")]
        public AudioClip carDrift;

        [Header("Tilt")]
        [SerializeField]
        private float tilt = 3f;

        [SerializeField]
        private float maxLeftTilt = 7f;

        [SerializeField]
        private float minRightTilt = 352f;

        [Header("Spawn Grenade")]
        public GameObject grenadeToSpawn;
        public int detonateCount; // How many times the grenade is detonate

        [Header("Values")]
        [SerializeField]
        [Range(1,5)]
        private int _playerHealth = 2;

        [Header("Destroy behaviour value")]
        public float blowUpForce = 100f;
        public float blowRotateSpeed = 200f;

        public int playerHealth { get { return _playerHealth; } private set { _playerHealth = value; } }

        private static readonly float MIN_LEFT_TILT = 0;
        private static readonly float MAX_RIGHT_TILT = 360;

        private const float epsilon = 0.6f;

        private AudioSource auSource;

        //Temp store data, avoid unnecessary initialization
        private Vector3 vpMousePosition;
        private Vector3 firstTurnPosition;//The position when the player first turn
        private Vector3 preAngularVelocity;
        private Vector3 vector3Zero;
        private Coroutine randomRotateCoroutine;

        private int currentDetonate;
        private int currentHealth;
        private bool turning = false;
        private bool blowUp = false;
        private float movingDistance;
        private Vector3 prePlayerPos;
         void OnEnable()
        {
            GameManager.GameStateChanged += OnGameStateChanged;
            GameManager.RevivalGameEvent += OnRevivalGameEvent;
        }

        void OnDisable()
        {
            GameManager.GameStateChanged -= OnGameStateChanged;
            GameManager.RevivalGameEvent -= OnRevivalGameEvent;
        }

        void OnRevivalGameEvent()
        {
            transform.position = new Vector3(transform.position.x, GameManager.Instance.StartPlayerPosition.y, transform.position.z);
            transform.rotation = Quaternion.Euler(Vector3.zero);
            carModel.transform.rotation = Quaternion.Euler(originalEulerAngles);
            currentHealth = playerHealth;
            carCollider.enabled = true;
            SetTrailEffect(true);
            carRb.useGravity = false;
            carRb.isKinematic = false;
            blowUp = false;
            if (randomRotateCoroutine != null)
                StopCoroutine(randomRotateCoroutine);
        }

        // Listens to changes in game state
        void OnGameStateChanged(GameState newState, GameState oldState)
        {
            if (newState == GameState.Playing)
            {
                var carModelTransform = transform.Find(CAR_MODEL_NAME);

                //Debug.Log("Car transform find:" + carModelTransform.GetInstanceID());

                if (carModelTransform == null) throw new UnityException("The model has not been set parent to the player prefabs!!");

                //Debug.Log(carModelTransform);
                carModel = carModelTransform.gameObject;

                wheels = new List<GameObject>();

                //Find all the wheels
                FindWheels(wheels);

                if (wheels.Count == 4)
                {
                    AddWheelController(wheels);
                }

                prePlayerPos = transform.position;
                originalEulerAngles = carModel.transform.eulerAngles;
            }
        }

     
        protected override void Start()
        {
            base.Start();

            InitializeValues();

        }

        private void InitializeValues()
        {
            //Set the is kinematic to true to prevent unexpected result
            carRb.isKinematic = true;

            Input.simulateMouseWithTouches = true;

            vector3Zero = Vector3.zero;

            currentDetonate = 0;
            currentHealth = playerHealth;
            preAngularVelocity = vector3Zero;
            

            auSource = GetComponent<AudioSource>();
        }

        // Update is called once per frame
        protected override void FixedUpdate()
        {
            if (GameManager.Instance.GameState != GameState.Playing) return;

            if (carRb.isKinematic)
                carRb.isKinematic = false;

            CheckAddScore();

            base.FixedUpdate();
        }

        protected void CheckAddScore()
        {
            if (GameManager.Instance != null && ScoreManager.Instance != null)
            {
                movingDistance += Vector3.Distance(transform.position, prePlayerPos);
                prePlayerPos = transform.position;
                if (movingDistance >= GameManager.Instance.MovingDistancePerScore)
                {
                    ScoreManager.Instance.AddScore(1);
                    movingDistance = 0;
                }
            }
        }

        // Calls this when the player dies and game over
        public void Die()
        {
            // Fire event
            if (PlayerDied != null)
                PlayerDied();
        }


        //Turn the car model by touching or clicking mouse button
        private void TurnModelByTouch()
        {
            if (Input.GetMouseButton(0)) //Can have a multitouch situation
            {
                vpMousePosition = Camera.main.ScreenToViewportPoint(Input.mousePosition);
                turnDirection = vpMousePosition.x <= 0.5 ? -1 : 1;

                //Rotate along the y-axis
                carRb.angularVelocity = transform.up * turnDirection * turnSpeed;
                //transform.Rotate(transform.up * turnDirection * turnSpeed, Space.Self);

                //Rotate model a bit along the z-axis
                carModel.transform.Rotate(-Vector3.forward * turnDirection * tilt, Space.Self);

                //Clamp the how much car model rotate along the z axis
                float eulerZ = 0;
                if (turnDirection == 1)
                {
                    eulerZ = Mathf.Clamp(carModel.transform.localEulerAngles.z, minRightTilt, MAX_RIGHT_TILT);
                }
                else if (turnDirection == -1)
                {
                    eulerZ = Mathf.Clamp(carModel.transform.localEulerAngles.z, MIN_LEFT_TILT, maxLeftTilt);
                }
                carModel.transform.localEulerAngles = new Vector3(carModel.transform.localEulerAngles.x, carModel.transform.localEulerAngles.y, eulerZ);

                //Checking detonate grenade
                CheckDetonatingGrenade();

                //If player begin to turn, but turn on the first time
                if (!turning)
                {
                    turning = true; 
                    firstTurnPosition = transform.position;
                }
              
                //Handle the multitouch situation
                if (preAngularVelocity != carRb.angularVelocity)
                {
                    turning = false;
                    currentDetonate = 0;
                }

                preAngularVelocity = carRb.angularVelocity;

                //Set Trail effect to true
                SetTrailEffect(true);
  
                //PlaySound
                if (!auSource.isPlaying && !SoundManager.Instance.IsSoundOff())
                {
                    auSource.Play();
                   // Debug.Log("ausource time: " );
                }
            }
            else
            {
                //Debug.Log("untouch");
                //Set Trail effect to false
                SetTrailEffect(false);

                //Cancel the angular velocity
                carRb.angularVelocity = vector3Zero;

                //Lerp the current car model angles to the original euler angles
                carModel.transform.localEulerAngles = Vector3.Lerp(carModel.transform.localEulerAngles, originalEulerAngles, turnSpeed);

                turning = false;
                currentDetonate = 0;
                auSource.Stop();
            }
        }

        protected override void TurnModel()
        {
            TurnModelByTouch();
        }

        //protected override void MoveTowards()
        //{
        //    base.MoveTowards();

        //    //Add the force to the rigidbody to move forward the car
        //    carRb.AddForce(transform.forward * moveSpeed);
        //}

        //Check if the player detonate the grenade
        private void CheckDetonatingGrenade()
        {

            //We need to calculate the radius R of the circle, by get the R = (velocity /angular velocity),
            float radius = (carRb.linearVelocity.magnitude / carRb.angularVelocity.magnitude);
            
            if (turning)
            {
                //Debug.Log("first turn pos:" + turnPosition + " current pos:" + transform.position);

                if (Mathf.Abs(transform.position.x - firstTurnPosition.x) <= epsilon && Mathf.Abs(transform.position.z - firstTurnPosition.z) <= epsilon)
                {
                    EffectManager.instance.ShowVFX(EffectManager.instance.fireSpark, transform.position, Quaternion.identity);
                    currentDetonate++;
                    ScoreManager.Instance.AddScore(GameManager.Instance.ScorePerLoop);
                    SoundManager.Instance.PlaySound(SoundManager.Instance.score);
                }

                if (currentDetonate == detonateCount)
                {
                    //Calculate the center of circle when then car move and rotate around
                    //then multiply the radius by turnDirection*transform.right (the normal vector of car velocity), then add the current position, therefore we have a cicle center
                    Vector3 centerCircle = transform.position + transform.right * radius * turnDirection;

                    //Then spawn a grenade at that position
                    Instantiate(grenadeToSpawn, centerCircle, Quaternion.identity);

                    //Reset values
                    turning = false; 
                    currentDetonate = 0;
                }
            }
        }

        public void TakeDamage(int amount)
        {
           
            //Decrease the current player health
            currentHealth -= amount;

            //Fire the event
            if (PlayerTakeDamage != null)
                PlayerTakeDamage(amount);

            if (currentHealth <= 0)
            {
                //Destroy car but use seperated effect
                DestroyCar(false);
                return;
            }
        }

        protected override IEnumerator DestroyBehaviour()
        {
            //Disable the car collider
            carCollider.enabled = false;
            SetTrailEffect(false);
            auSource.Stop();

            //Start the blow up and rotate effect
            BlowUpAndRandomRotate();

            //Add seprate VFX
            AddSeperateVFX();

            //Fire the die event
            Die();

            yield return desBehaveWait;

            //Make an car on ground effect
            carRb.isKinematic = true;
            if (randomRotateCoroutine != null)
                StopCoroutine(randomRotateCoroutine);
        }

        private void BlowUpAndRandomRotate()
        {
            //Blow up the rigidbody
            //carRb.useGravity = true;
            if (blowUp == false)
            {
                //carRb.velocity = -transform.up * gravityScale;
                carRb.linearVelocity = vector3Zero;
                carRb.useGravity = true;
                carRb.AddForce(transform.up * blowUpForce, ForceMode.Impulse);
                
                //Rotate random the car model
                carRb.angularVelocity = vector3Zero;

                randomRotateCoroutine =  StartCoroutine(RandomRotate());

                blowUp = true;
            }
        }


        private IEnumerator RandomRotate()
        {
            Vector3 randomRotate = Random.insideUnitSphere;

            while (true)
            {
                carModel.transform.Rotate(randomRotate * blowRotateSpeed, Space.Self);
                yield return null;
            }
        }
 
        protected override void AddSeperateVFX()
        {
            EffectManager.instance.ShakeStaticCamera();
        }

    }
}