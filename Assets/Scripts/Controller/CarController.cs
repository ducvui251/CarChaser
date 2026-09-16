using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace CrashyChasy
{
    [RequireComponent(typeof(Rigidbody))]
    [RequireComponent(typeof(Collider))]
    public abstract class CarController : MonoBehaviour
    {

        //Properties

        [Header("Base car values")]
        //Public variables
        public float moveSpeed = 25f;
        public float turnSpeed = 10f;

        //[Range(0,1f)]
        //public float driftFactor = 0.95f;//How much the drift will effect

        [Header("Trails")]
        public GameObject leftTrail;
        public GameObject rightTrail;

        [Header("Destroy Behaviour")]
        public float behaviourDuration = 0f;

        protected int turnDirection = 1;

        //Protected variables
        //Components
        protected Rigidbody carRb;
        protected Collider carCollider;
        protected Vector3 originalEulerAngles;
        protected GameObject carModel; //This is the game object named Car in the heirachy
        protected List<GameObject> wheels;
        protected WaitForSeconds desBehaveWait;
        //Values
        protected float angle = 10;

        public static readonly string CAR_MODEL_NAME = "Car";
        public static readonly string CAR_WHEEL_NAME = "wheel";

        protected virtual void Start()
        {
            carRb = GetComponent<Rigidbody>();
            carCollider = GetComponent<Collider>();
            desBehaveWait = new WaitForSeconds(behaviourDuration);
           
        }

        protected virtual void MoveTowards()
        {

            ////Simulate the drift effect, we must get the sum of the two velocity, forward and right
            //carRb.velocity = ForwardVelocity() + driftFactor * RightVelocity();

            ////Clamp the velocity magnitude
            //carRb.velocity = Vector3.ClampMagnitude(carRb.velocity, maxVelocityMagnitude);

            carRb.linearVelocity = transform.forward * moveSpeed;

        }

        protected virtual void FixedUpdate()
        {
            if (GameManager.Instance.GameState == GameState.Playing)
            {
                MoveTowards();

                TurnModel();
            }
        }

        protected abstract void TurnModel();

        protected void SetTrailEffect(bool isStarted)
        {
            leftTrail.GetComponent<TrailRenderer>().emitting = isStarted;
            rightTrail.GetComponent<TrailRenderer>().emitting = isStarted;
        }

        protected void FindWheels(List<GameObject> wheelList)
        {
            wheelList.Clear();

            //Find all the wheels of car player in the heirachy Player->Car->model->parts
            var actualModel = carModel.transform.GetChild(0);
            int partsCount = actualModel.transform.childCount;

            //Get all the wheel in the car part
            for (int i = 0; i < partsCount; i++)
            {
                var carPart = actualModel.GetChild(i);

                if (carPart.name.Contains(CAR_WHEEL_NAME))
                    wheelList.Add(carPart.gameObject);

            }
        }

        protected void AddWheelController(List<GameObject> wheelList)
        {
            int wheelCount = wheelList.Count;

            for (int i = 0; i < wheelCount; i++)
            {
                wheelList[i].AddComponent<WheelController>();
            }

        }

        protected virtual void DestroyCar(bool showFullVFX = true) //Using template design pattern
        {
           
            //Show effect
            if (showFullVFX) //If we want to show full vfx
            {
                EffectManager.instance.destroyCount++;
                EffectManager.instance.StartDestroyEffect(carCollider);
            }
            else
            {
                //Do what you want to do before destroying the object in the derived class
                StartCoroutine(DestroyBehaviour());
            }

           //Destroy(gameObject,behaviourDuration + 0.05f);
        }

        protected abstract IEnumerator DestroyBehaviour();
       
        protected abstract void AddSeperateVFX();

    }

}
