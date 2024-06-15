using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

using Unity.MLAgents;
using Unity.MLAgents.Sensors;
using Unity.MLAgents.Actuators;

public class CarAgentParallel : Agent {
    private CarController carController;
    public CarSpotsManager carSpotsManager;
    public float spawnRadiusX = 4f;
    public float spawnRadiusZ = 0.7f;
    public float envRadiusX = 7f;
    public float envRadiusZ = 7f;
    public bool findParkingSpot = true;
    public bool learningModel = false; // Falso durante l'addestramento e durante registrazione demo
    public float maxIdleTime = 10f; // Tempo massimo di inattività dell'agente
    public GameObject currentEnvironment; // Aggiungi questo campo alla tua classe

    private Rigidbody rb;
    private int steps = 0; // Numero di passi compiuti dall'agente
    private bool inTarget = false;
    private Vector3 startPosition;
    private Quaternion startRotation;
    private Vector3 lastPosition;
    private bool isLookingForSpot;
    private bool isPositioning;
    private RayPerceptionSensorComponent3D RayPerceptionSensorComponent;
    private Vector3 detectedSpotLocation;
    private float predictedSpotSize = 0f;
    private List<Transform> targets; // Lista di target
    private Collider[] stripeColliders;
    private Collision lastCollision;
    private float timeSinceLastAction = 0f; // Tempo trascorso dall'ultima azione
    private float timeToParking = 0f; // Tempo totale trascorso
    private int randomPositionIndex;
    public int RandomPositionIndex{ get { return randomPositionIndex; }} 
    private Transform closestTarget; // Variabile di istanza per memorizzare il target più vicino
    private bool personDetectedFront = false;

    
    public void UpdateTargets() {
        if (currentEnvironment != null) {
            // Trova l'oggetto padre "Targets" nell'ambiente corrente
            Transform targetsParent = currentEnvironment.transform.Find("Targets");
            if (targetsParent != null) {
                // Trova tutti i GameObject con il tag "Finish" che sono figli di "Targets"
                targets = new List<Transform>();
                foreach (Transform child in targetsParent.GetComponentsInChildren<Transform>()) {
                    if (child.CompareTag("Finish")) {
                        targets.Add(child);
                    }
                }
            }
        } 
    }

    void FixedUpdate() {
        UpdateClosestTarget();
        
        // Se la ricerca di un posto è abilitata, la macchina guiderà e cercherà un posto
        if(isLookingForSpot) {
            CruiseControl(6.5f); // Controllo della velocità della macchina
            FindParkingSpot(); // Trova un posto auto
        }

        if(isPositioning && findParkingSpot) { 
            PositionCar(+5f); // Posiziona la macchina
        }

        // Se non si sta cercando un posto (fase di addestramento/parcheggio libero trovato) e ci sono dei target
        if(!isLookingForSpot && targets != null) {
            RequestDecision();
            timeToParking += Time.fixedDeltaTime; // Incrementa il tempo totale trascorso

            if(closestTarget != null && (Mathf.Abs(transform.position.x - closestTarget.transform.position.x) > envRadiusX || Mathf.Abs(transform.position.z - closestTarget.transform.position.z) > envRadiusZ)){
                Debug.Log("Fuori dal raggio");
                AddReward(-12f);
                EndEpisode();
            }
            
        }

        // Controllo l'inattività dell'agente
        if(Mathf.Abs(carController.CurrentSpeed) < 2f) {
            // Debug.Log(timeSinceLastAction);
            timeSinceLastAction += Time.fixedDeltaTime; // Incrementa il tempo trascorso dall'ultima azione
            
            if(timeSinceLastAction > maxIdleTime) {
                Debug.Log("Inattività dell'agente");
                AddReward(-50f);
                EndEpisode();
            }
        } else {
            timeSinceLastAction = 0f; // Azzera il tempo trascorso dall'ultima azione
        }
    }

    private void Reset() {

        timeSinceLastAction = 0f;
        timeToParking = 0f;

        // Ottieni la posizione di partenza dell'ambiente corrente
        Vector3 startPosition = currentEnvironment.transform.position;

        if(findParkingSpot) {
            isLookingForSpot = true;
            isPositioning = false; 
        }
        
        float spawnX = Random.Range(startPosition.x - spawnRadiusX, startPosition.x + spawnRadiusX);
        float spawnZ = Random.Range((startPosition.z + 2.9f) - spawnRadiusZ, (startPosition.z + 2.9f) + spawnRadiusZ);
        
        randomPositionIndex = Random.Range(0, 2);
        Vector3 randomPosition;
        Quaternion randomRotation;


        if(randomPositionIndex == 0) { // Parcheggio a sinistra (-1)
            randomPosition = new Vector3(startPosition.x - 43.8f, 0.23f, spawnZ);
            randomRotation = Quaternion.Euler(0f, 90f, 0f);
        } else { // Parcheggio a destra (1)
            randomPosition = new Vector3(startPosition.x + 43.8f, 0.23f, spawnZ);
            randomRotation = Quaternion.Euler(0f, -90f, 0f);
        }

        rb.transform.position = randomPosition;
        rb.transform.rotation = randomRotation;

        rb.velocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;

        steps = 0;
    }

    public override void Initialize() {
        carController = GetComponent<CarController>();
        rb = GetComponent<Rigidbody>();

        isLookingForSpot = findParkingSpot;

        RayPerceptionSensorComponent = GetComponent<RayPerceptionSensorComponent3D>(); 
        
        startPosition = transform.position;
        startRotation = transform.rotation;

        lastPosition = startPosition;

        Reset();
    }

    private void FindParkingSpot() {

        var RpMeasurements = RayPerceptionMeasurements();
  
        int LeftLikelihoodScore = 0;
        int RightLikelihoodScore = 0; 

        // Controlla se i sensori laterali sinistri e destri rilevano una distanza lunga
        if(RpMeasurements.RDistL[2] > 0.5f) {
            LeftLikelihoodScore += 1;
        }
        if(RpMeasurements.RDistR[2] > 0.5f) {
            RightLikelihoodScore += 1;
        }

        if((RpMeasurements.RDistL.Sum() < RpMeasurements.RDistR.Sum()) || (RpMeasurements.hitRoadR == false && RpMeasurements.RDistL[2] < 0.5f)) {
            LeftLikelihoodScore += 1;
        } 

        if((RpMeasurements.RDistL.Sum() > RpMeasurements.RDistR.Sum()) || (RpMeasurements.hitRoadL == false && RpMeasurements.RDistR[2] < 0.5f)) {
            RightLikelihoodScore += 1;
        }
        
        // Se i sensori laterali sinistri e destri rilevano due strisce allora la macchina si trova in uno spazio di parcheggio libero 
        if(RpMeasurements.hitStripeLF && RpMeasurements.hitStripeLB) {
            LeftLikelihoodScore += 1;
        } 
        if(RpMeasurements.hitStripeRF && RpMeasurements.hitStripeRB) {
            RightLikelihoodScore += 1;
        }
        
        // Se uno dei lati soddisfa tutti i requisiti, l'agente è nel mezzo di uno spazio
        if(LeftLikelihoodScore == 3) {
            // Verifica che lo spazio sia abbastanza grande
            float PredictedSpace = (RpMeasurements.RDistL[1] * Mathf.Cos(60*Mathf.Deg2Rad)) + (RpMeasurements.RDistL[3] * Mathf.Cos(60*Mathf.Deg2Rad)); 
            // Le distanze sono in unità normalizzate, quindi moltiplica per la lunghezza del raggio per ottenere la distanza effettiva
            PredictedSpace *= 7;
            
            if(PredictedSpace > 3f) {
                isLookingForSpot = false;
                isPositioning = true;
                predictedSpotSize = PredictedSpace;
                detectedSpotLocation = new Vector3(transform.position.x, transform.position.y, transform.position.z);
                if(learningModel != true) {
                    Debug.Log("Trovato posto a sinistra");
                }
            }
        } else if(RightLikelihoodScore == 3) {
            // Verifica che lo spazio sia abbastanza grande
            float PredictedSpace = (RpMeasurements.RDistR[1] * Mathf.Cos(60*Mathf.Deg2Rad)) + (RpMeasurements.RDistR[3] * Mathf.Cos(60*Mathf.Deg2Rad));
            // Le distanze sono in unità normalizzate, quindi moltiplica per la lunghezza del raggio per ottenere la distanza effettiva
            PredictedSpace *= 7;
            
            if(PredictedSpace > 3f) {
                isLookingForSpot = false;
                isPositioning = true;
                predictedSpotSize = PredictedSpace;
                detectedSpotLocation = new Vector3(transform.position.x, transform.position.y, transform.position.z);
                if(learningModel != true) {
                    Debug.Log("Trovato posto a destra");
                }
                
            }
        } else {
            isLookingForSpot = true;
        }
    }

    private (float[] RDistL, float[] RDistR, float RDistF, float RDistB, bool hitStripeLF, bool hitStripeLB, bool hitStripeRF, bool hitStripeRB, bool hitPlantL, bool hitPlantR, bool hitRoadL, bool hitRoadR) RayPerceptionMeasurements() {
        RayPerceptionInput RayPerceptionIn = RayPerceptionSensorComponent.GetRayPerceptionInput(); // Ottieni l'input del raggio di percezione
        RayPerceptionOutput RayPerceptionOut = RayPerceptionSensor.Perceive(RayPerceptionIn); // Percezione del raggio
        RayPerceptionOutput.RayOutput[] RayOutputs = RayPerceptionOut.RayOutputs; // Ottieni gli output del raggio

        int RayAmount = RayOutputs.Length - 1; // Numero di raggi
        float[] RayDistances = new float[RayAmount - 1]; // Distanze dei raggi
        
        // Distanze dei raggi 
        float[] RayDistancesLeft = new float[(RayAmount - 2) / 2]; // Sinistra
        float[] RayDistancesRight = new float[(RayAmount - 2) / 2]; // Destra
        float RayDistanceFront = RayOutputs[0].HitFraction; // Frontale
        float RayDistanceBack = RayOutputs[RayAmount - 1].HitFraction; // Posteriore

        bool hitStripeLeftFront = RayOutputs[2].HasHit && RayOutputs[2].HitGameObject != null && RayOutputs[2].HitGameObject.CompareTag("Stripe"); // Sensore anteriore sinistro 
        bool hitStripeLeftBack = RayOutputs[10].HasHit && RayOutputs[10].HitGameObject != null && RayOutputs[10].HitGameObject.CompareTag("Stripe"); // Sensore posteriore sinistro
        bool hitPlantLeft = RayOutputs[6].HasHit && RayOutputs[6].HitGameObject != null && RayOutputs[6].HitGameObject.CompareTag("Plants"); // Sensore sinistro

        bool hitStripeRightFront = RayOutputs[1].HasHit && RayOutputs[1].HitGameObject != null && RayOutputs[1].HitGameObject.CompareTag("Stripe"); // Sensore anteriore destro
        bool hitStripeRightBack = RayOutputs[9].HasHit && RayOutputs[9].HitGameObject != null && RayOutputs[9].HitGameObject.CompareTag("Stripe"); // Sensore posteriore destro
        bool hitPlantRight = RayOutputs[5].HasHit && RayOutputs[5].HitGameObject != null && RayOutputs[5].HitGameObject.CompareTag("Plants"); // Sensore destro

        bool hitRoadLeft = (((RayOutputs[4].HasHit && RayOutputs[4].HitGameObject != null && RayOutputs[4].HitGameObject.CompareTag("Road")) &&
                             (RayOutputs[6].HasHit && RayOutputs[6].HitGameObject != null && RayOutputs[6].HitGameObject.CompareTag("Road")) &&
                             (RayOutputs[8].HasHit && RayOutputs[8].HitGameObject != null && RayOutputs[8].HitGameObject.CompareTag("Road"))));

        bool hitRoadRight = (((RayOutputs[3].HasHit && RayOutputs[3].HitGameObject != null && RayOutputs[3].HitGameObject.CompareTag("Road")) &&
                              (RayOutputs[5].HasHit && RayOutputs[5].HitGameObject != null && RayOutputs[5].HitGameObject.CompareTag("Road")) &&
                              (RayOutputs[7].HasHit && RayOutputs[7].HitGameObject != null && RayOutputs[7].HitGameObject.CompareTag("Road"))));

        for(int i = 1; i < RayAmount-1; i++) {
            // Se è pari
            if(i % 2 == 0) {
                RayDistancesLeft[(i/2)-1] = RayOutputs[i].HitFraction; // Lato sinistro
            } else {
                RayDistancesRight[(i-1)/2] = RayOutputs[i].HitFraction; // Lato destro
            }
        }

        return (RayDistancesLeft, RayDistancesRight, RayDistanceFront, RayDistanceBack, hitStripeLeftFront, hitStripeLeftBack, hitStripeRightFront, hitStripeRightBack, hitPlantLeft, hitPlantRight, hitRoadLeft, hitRoadRight);
    }

    private void CruiseControl(float Speed) {
        RayPerceptionInput RayPerceptionIn = RayPerceptionSensorComponent.GetRayPerceptionInput(); // Ottieni l'input del raggio di percezione
        RayPerceptionOutput RayPerceptionOut = RayPerceptionSensor.Perceive(RayPerceptionIn);
        RayPerceptionOutput.RayOutput[] RayOutputs = RayPerceptionOut.RayOutputs; 

        // Controllo se uno dei sensori frontali rileva un pedone
        if ((RayOutputs[0].HasHit && RayOutputs[0].HitGameObject != null && RayOutputs[0].HitGameObject.CompareTag("Person") && RayOutputs[0].HitFraction < 0.7f) ||
            (RayOutputs[1].HasHit && RayOutputs[1].HitGameObject != null && RayOutputs[1].HitGameObject.CompareTag("Person") && RayOutputs[1].HitFraction < 0.7f) ||
            (RayOutputs[2].HasHit && RayOutputs[2].HitGameObject != null && RayOutputs[2].HitGameObject.CompareTag("Person") && RayOutputs[2].HitFraction < 0.7f)) {
            
            personDetectedFront = true;
        } else {
            personDetectedFront = false;
        }

        // Se un pedone è stato rilevato, fermare la macchina
        if(personDetectedFront) {
            carController.Move(0, 0f, 0f, 0f);
        } else {
            // Se la macchina è sotto la velocità desiderata, accelera
            if(carController.CurrentSpeed < Speed) {
                carController.Move(0, 0.5f, 0f, 0f);
            } 
            // Se la macchina è sopra la velocità desiderata, rallenta
            else if(carController.CurrentSpeed > Speed){
                carController.Move(0, -0.5f, 0f, 0f);
            }
        }
    }

    private void PositionCar(float offsetX) {
        float coveredX = Mathf.Abs(transform.position.x - detectedSpotLocation.x);
        float absoluteOffsetX = Mathf.Abs(offsetX);

        if(coveredX < absoluteOffsetX && offsetX < 0) {
            carController.Move(0f, -.3f, 0f, 0f); // Muovi l'auto verso il basso
        } else if(coveredX < absoluteOffsetX && offsetX > 0) {
            carController.Move(0f, .1f, 0f, 0f); // Muovi l'auto verso l'alto
        } else {
            isPositioning = false; // Fine posizionamento dell'auto
        }
    }

    public float CalculateReward() {
        float reward = 0f;

        float totDirectionChangeReward = 0f;
        float totAngleChangeReward = 0f; 
        float speed = 0f;
        string value = "";

        if(learningModel) {
            speed = 0.5f;
        } else {
            speed = 2f;
        }

        // Controlla se l'auto è nel target e se ci sono target disponibili
        if(inTarget && targets != null && targets.Count > 0) {

            // Trova il target più vicino all'agente
            Transform closestTarget = null;
            float closestDistance = Mathf.Infinity;
            foreach(Transform target in targets) {
                if(target != null) {
                    float distance = Vector3.Distance(transform.position, target.position);
                    if(distance < closestDistance) {
                        closestTarget = target;
                        closestDistance = distance;
                    }
                }
            }

            // Se è stato trovato un target valido, calcola la ricompensa
            if(closestTarget != null) {

                // Calcola l'angolo tra la direzione dell'auto e la direzione del target
                float angleToTarget = Vector3.Angle(transform.forward, closestTarget.forward);

                // Se l'auto sta guidando all'indietro rispetto al target, l'angolo sarà 180 gradi
                if(angleToTarget > 90f) {
                    angleToTarget = 180f - angleToTarget;
                }

                // Limita l'angolo tra 0 e 90 gradi
                angleToTarget = Mathf.Clamp(angleToTarget, 0f, 90f);

                // Calcola la ricompensa per l'angolo di rotazione rispetto al target
                float angleReward = (-(1f / 45f) * angleToTarget) + 1f;

                // Aggiunge la ricompensa per l'angolo alla ricompensa totale
                totAngleChangeReward = angleReward;

                // Aggiunge la ricompensa per l'angolo alla ricompensa totale
                reward += totAngleChangeReward;

                // Calcola la distanza dall'auto al target più vicino
                float distanceToTarget = closestDistance;

                // Calcola la ricompensa basata sulla distanza e sulla direzione
                if(lastPosition != Vector3.zero) {
                    
                    // Distanza sull'asse X e Z tra la posizione attuale dell'auto e la posizione del target
                    float distanceToTargetX = Mathf.Abs(transform.position.x - closestTarget.position.x);
                    float distanceToTargetZ = Mathf.Abs(transform.position.z - closestTarget.position.z);

                    // Distanza sull'asse X e Z tra l'ultima posizione dell'auto e la posizione del target
                    float lastDistanceToTargetX = Mathf.Abs(lastPosition.x - closestTarget.position.x);
                    float lastDistanceToTargetZ = Mathf.Abs(lastPosition.z - closestTarget.position.z);

                    // Variazione di direzione sull'asse X e Z
                    float directionChangeX = lastDistanceToTargetX - distanceToTargetX;
                    float directionChangeZ = lastDistanceToTargetZ - distanceToTargetZ;

                    // Calcola la ricompensa per la variazione di direzione totale
                    totDirectionChangeReward = (directionChangeX + directionChangeZ) * 10f;
                    
                    // Limita la ricompensa tra -0.5 e 0.5
                    totDirectionChangeReward = Mathf.Clamp(totDirectionChangeReward, -0.5f, 0.5f);

                    // Calcola la ricompensa per la distanza sull'asse X e Z rispetto al raggio (max 7f)
                    float distanceRewardX = (distanceToTargetX/7f);
                    float distanceRewardZ = (distanceToTargetZ/7f);

                    float totDistanceReward = (distanceRewardX + distanceRewardZ); // Ricompensa totale per la distanza

                    reward += totDirectionChangeReward + totDistanceReward; 
                }

                if(angleToTarget < 15f && distanceToTarget < 1f && Mathf.Abs(carController.CurrentSpeed) < speed) {
                    if(randomPositionIndex == 0) {
                        value = " a sinistra ";
                    } else {
                        value = " a destra ";
                    }

                    if(timeToParking < 20f) {
                        Debug.Log("Macchina parcheggiata correttamente" + value + "in meno di 20 secondi");
                        reward += 100f;
                    } else if(timeToParking < 50f) {
                        Debug.Log("Macchina parcheggiata correttamente" + value + "in meno di 50 secondi");
                        reward += 50f;
                    } else {
                        Debug.Log("Macchina parcheggiata correttamente" + value + "in più di 50 secondi");
                        reward += 25f;
                    }
                    
                    EndEpisode(); // Termina l'episodio
                }

                if(angleToTarget > 15f && inTarget && Mathf.Abs(carController.CurrentSpeed) < 0.5f) {
                    // Debug.Log("L'agente è nel target ma non è allineato correttamente e non si sta muovendo molto");
                    reward = 0f;
                }

                lastPosition = transform.position;
            }
        }

        return reward;
    }

    private void UpdateClosestTarget() {
        float closestDistance = Mathf.Infinity;
        foreach(Transform target in targets) {
            if(target != null) {
                float distance = Vector3.Distance(transform.position, target.position);
                if(distance < closestDistance) {
                    closestTarget = target;
                    closestDistance = distance;
                }
            }
        }
    }

    public override void OnEpisodeBegin() {
        carSpotsManager.DestroyTargets();
        carSpotsManager.HideRandomCarsAndShowTargets();

        Reset();

        UpdateTargets();
        UpdateClosestTarget();
    }


    public override void CollectObservations(VectorSensor sensor) {
        sensor.AddObservation(carController.CurrentSpeed);
        sensor.AddObservation(randomPositionIndex == 0 ? -1f : 1f); // Direzione del parcheggio (-1 sinistra, 1 destra)
    }

    public override void OnActionReceived(ActionBuffers actions) {
        float steering = actions.ContinuousActions[0];
        float accel = actions.ContinuousActions[1];
        float reverse = actions.ContinuousActions[2];

        accel = (accel + 1) / 2;
        reverse = (reverse + 1) / 2;

        accel = accel - reverse;
        
        if(!isLookingForSpot) {
            carController.Move(steering, accel, 0f, 0f);
        }
    
        steps++;

        float reward = CalculateReward();
        AddReward(reward);
    }

    public override void Heuristic(in ActionBuffers actionsOut) {
        ActionSegment<float> continuousActionsOut = actionsOut.ContinuousActions;

        float steering = Input.GetAxis("Horizontal");
        float accel = 0f;
        float reverse = 0f;

        if(Input.GetKey(KeyCode.UpArrow)) {
            accel = 1f;
        } else if(Input.GetKey(KeyCode.DownArrow)) {
            reverse = 2f;
        }

        continuousActionsOut[0] = steering;
        continuousActionsOut[1] = accel;
        continuousActionsOut[2] = reverse;
    }

    void OnTriggerEnter(Collider other) {
        if(other.gameObject.tag == "Finish") {
            inTarget = true;
        }
    }

    void OnTriggerExit(Collider other) {
        if(other.gameObject.tag == "Finish") {
            inTarget = false;
        }
    }

    void OnCollisionEnter(Collision collision) {
        if(collision.gameObject.tag != "Stripe") {
            print(collision.gameObject.tag);
        }

        if(collision.gameObject.tag == "Plants")  {// Penalizza l'agente se urta le piante
            AddReward(-30f);
            EndEpisode();
        } else if(collision.gameObject.tag == "Car") { // Penalizza l'agente se urta un'altra macchina
            AddReward(-10f);
            EndEpisode();
        } else if(collision.gameObject.tag == "Road") { // Penalizza l'agente se urta il terreno (è arrivato fuori strada senza trovare un posto auto o ha urtato il parciapiede)
            AddReward(-15f);
            EndEpisode();
        } else if(collision.gameObject.tag == "Stripe") { // Penalizza l'agente se urta la striscia
            AddReward(-3f);
        } else if(collision.gameObject.tag == "Vehicle") { // Penalizza l'agente se urta un veicolo
            AddReward(-15f);
            EndEpisode();
        } else if(collision.gameObject.tag == "Person") { // Penalizza l'agente se urta una persona
            AddReward(-50f);
            EndEpisode();
        }

        lastCollision = collision;
    }
}