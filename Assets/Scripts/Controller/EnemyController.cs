using System.Collections;
using System.Collections.Generic;
using UnityEngine;

//This class will implement simple AI which chasing the target

namespace CarChaser
{
   
    public class EnemyController : CarController
    {

        private class CarSide
        {
            public Vector3 left { get; set; }
            public Vector3 right { get; set; }
        }

        [Header("Target")]
        public string targetTag;

        [Header("Values")]
    
        [Range(0,0.1f)]
        public float speedIncreaseFactor = 0.05f;
        [Range(0, 0.1f)]
        public float rotateIncreaseFactor = 0.01f;
        public float offsetToCarSide = 10f;
        [Tooltip("When the relative distance between the enemy and the target is equal to this value, the enemy will follow the target")]
        public float offsetToFollowTarget = 30f;
        [Range(0, 3)]
        public int damageAmount = 1;
        

        [SerializeField]
        private Vector2 deathAngle = new Vector2(3, 40); // When the angle between two collision object's velocity is in range of death angle, the car object will be destroyee

        [SerializeField]
        private float maxMoveSpeed = 70f;

        [SerializeField]
        private float maxTurnSpeed = 70f;

        [Tooltip("The delta angle epsilon to compare with two velocity between two frames")]
        public float deltaAngleEpsilon = 0.2f;//deltaAngleEpsilon

        [Header("Fly away")]
        public float flyAwayForce = 200f;

        //Private variables
        private CarSide carSide;
    
        //Temp varibles to store data, to avoid unnecessary initializing
        private Vector3 offset;//The offset between the target and the enemy position
        private Vector3 preVelocity = Vector3.forward;
        private Vector3 carSideTarget;
        private float sqrOffsetToFollowTarget;


        private GameObject target;

        private bool flying = false;

        // Start is called before the first frame update
        protected override void Start()
        {
            base.Start();

            InitializeValue();
        }

        // Update is called once per frame
        protected override void FixedUpdate()
        {
            //Check if target == null( meaning target is destroyed or not found yet)
            if (target == null || GameManager.Instance.GameState == GameState.GameOver)
            {
                carRb.angularVelocity = Vector3.zero;
                SetTrailEffect(false);
                GetComponent<AutoDestroy>().enabled = true;
                return;
            }

            //Check if the car is flying, then we dont need the trail 
            if (flying)
            {
                SetTrailEffect(false);
                return;
            }

            //Calculate the car side target for the enemy to follow
            carSideTarget = DeterminedCarSideTarget(target.transform);

            //Move and turn enemy towards the target
            base.FixedUpdate();
        }

        private void InitializeValue()
        {
            //Find the target from the hierachy
            target = GameObject.FindGameObjectWithTag(targetTag);
            if (target == null) throw new UnityException("The target has not been taged or not on the heirachy");

            //offset = target.transform.position - transform.position;

            carSide = new CarSide() { left = new Vector3(), right = new Vector3() };

            carSideTarget = DeterminedCarSideTarget(target.transform);
            sqrOffsetToFollowTarget = offsetToFollowTarget * offsetToFollowTarget;

            var carModelTransform = transform.Find(CAR_MODEL_NAME);

            if (carModelTransform == null) throw new UnityException("The model has not been set parent to the player prefabs!!");
            carModel = carModelTransform.gameObject;
            wheels = new List<GameObject>();

            //Find all the wheels from the car model and add to the wheels list
            FindWheels(wheels);

            //Add the wheel controller script to each wheel from the wheels list if find all the four wheel
            if (wheels.Count == 4)
            {
                AddWheelController(wheels);
            }
        }


        //Iterate to all the wheel and add the wheel controller component

        private Vector3 DeterminedCarSideTarget(Transform targetTrans)
        {
            //Calculate the car side base on the target current position
            carSide.left = targetTrans.position - targetTrans.right * offsetToCarSide;
            carSide.right = targetTrans.position + targetTrans.right * offsetToCarSide;

            //Determine the target car side base on the enemy current position and the target current position
            return (transform.position.x <= targetTrans.position.x) ? carSide.left : carSide.right;
        }

        protected override void TurnModel()
        {
            //Calculate the delta angle between previous velocity vs current velocity
            float deltaAngle = Vector3.Angle(preVelocity, carRb.linearVelocity);

            if (deltaAngle >= deltaAngleEpsilon)
            {
                SetTrailEffect(true);
            }
            else
            {
                SetTrailEffect(false);
            }

            preVelocity = carRb.linearVelocity;

            //Calculate the relative square distance between the target position and the enemy position
            float relativeSqrDistance = (target.transform.position - transform.position).sqrMagnitude;

            //Check if the relative distance still greater than the square off set to follow target
            if (relativeSqrDistance > sqrOffsetToFollowTarget)
            {
                //Turn the model to car side target
                TurnModelToTarget(carSideTarget);
            }
            else
            {
                //Turn the model directly to target
                TurnModelToTarget(target.transform.position);
            }
        }

        protected override void AddSeperateVFX()
        {
           
        }

        protected override void MoveTowards()
        {
            base.MoveTowards();
            moveSpeed += speedIncreaseFactor;

            //Clamp the move speed
            moveSpeed = Mathf.Clamp(moveSpeed, 0, maxMoveSpeed);
        }
        private void TurnModelToTarget(Vector3 targetPos)
        {
           // transform.LookAt(target.transform.position, transform.up * turnSpeed);

            Vector3 offset = targetPos - transform.position;

            carRb.angularVelocity = -Vector3.Cross(offset.normalized,transform.forward) * turnSpeed;

            turnSpeed += rotateIncreaseFactor;
  
            //Clamp the turn speed
            turnSpeed = Mathf.Clamp(turnSpeed, 0, maxTurnSpeed);

            //carRb.AddTorque(-Vector3.Cross(offset.normalized,transform.forward) * turnSpeed);
        }

        private void OnTriggerEnter(Collider collider)
        {
            if (collider.CompareTag("Player")) //If enemy collide with player
            {
                //Calculate the angle between the forward vector of player and enmy
                float relativeAngle = Vector3.Angle(transform.forward, collider.gameObject.transform.forward);

                //Check if this angle is in range of  enemy death angle
                if (relativeAngle >= deathAngle.x && relativeAngle <= deathAngle.y) //If true, destroy this gameobject
                {

                    ScoreManager.Instance.AddScore(GameManager.Instance.ScorePerCar);
                    //Also destroy car
                    DestroyCar(false);
                }
                else //Take damage the player
                {

                    //Show a destroy effect to easily know
                    var playerController = collider.gameObject.GetComponent<PlayerController>();
                    if (playerController != null)
                        playerController.TakeDamage(damageAmount);

                    //Also destroy car
                    DestroyCar(false);
                }
            }
            else if (collider.CompareTag("Enemy")) //if enemy collide with enemy
            {
               //Increase destroy count for spawning one coin for two collided enemy car 
                EffectManager.instance.destroyCount++;
                EffectManager.instance.SpawnCoin(transform.position,Quaternion.identity);

               //Also destroy car
                DestroyCar(false);
            }
        }

        protected override IEnumerator DestroyBehaviour()
        {
            FlyAway();

            yield return desBehaveWait;

            Destroy(gameObject);
        }

        private void FlyAway()
        {
            carRb.linearVelocity = Vector3.zero;
            carRb.angularVelocity = Vector3.zero;
            carCollider.enabled = false;

            EffectManager.instance.ShowVFX(EffectManager.instance.impactSpark,transform.position,Quaternion.identity);
            SoundManager.Instance.PlaySound(SoundManager.Instance.flyAway, false, false, 1f);

            Vector3 forceDirection = (-transform.forward + transform.up);

            carRb.AddForce(forceDirection * flyAwayForce, ForceMode.Impulse);
            StartCoroutine(Spining(forceDirection));
            flying = true;
        }

   

        private IEnumerator Spining(Vector3 axis)
        {
            while (true)
            {
                carModel.transform.Rotate(axis * turnSpeed, Space.Self);
                yield return null;
            }
        }

        private void OnDestroy()
        {
            GroundController.currentEnemiesCount--;
        }

    }
}


