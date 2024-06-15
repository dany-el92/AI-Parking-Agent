using System;
using UnityEngine;

public class CarController : MonoBehaviour {
    [SerializeField] private WheelCollider[] m_WheelColliders = new WheelCollider[4]; // Collider delle ruote dell'auto
    [SerializeField] private GameObject[] m_WheelMeshes = new GameObject[4]; // Mesh delle ruote dell'auto
    [SerializeField] private float m_MaximumSteerAngle; // Angolo massimo di sterzata
    [Range(0, 1)] [SerializeField] private float m_TractionControl; // 0 è nessun controllo di trazione, 1 è un'interferenza completa
    [SerializeField] private float m_FullTorqueOverAllWheels; // Coppia massima su tutte le ruote
    [SerializeField] private float m_ReverseTorque; // Coppia in retromarcia
    [SerializeField] private float m_BrakeTorque; // Coppia di frenata
    [SerializeField] private float m_engineBrakeTorque = 100; // Coppia di freno motore
    [SerializeField] private float m_TopSpeed = 200; 
    private float m_MaxHandbrakeTorque;
    private Quaternion[] m_WheelMeshLocalRotations;
    private float m_SteerAngle;
    private float m_CurrentTorque;
    private Rigidbody m_Rigidbody;
    private float _currentSpeed;

    public float CurrentSpeed{ get { return _currentSpeed; }} 

    private void Start() {
        m_WheelMeshLocalRotations = new Quaternion[4];

        // Memorizza la posizione e la rotazione iniziale delle mesh delle ruote
        for (int i = 0; i < 4; i++) {
            m_WheelMeshLocalRotations[i] = m_WheelMeshes[i].transform.localRotation;
        }

        m_MaxHandbrakeTorque = float.MaxValue;

        m_Rigidbody = GetComponent<Rigidbody>();
        m_CurrentTorque = m_FullTorqueOverAllWheels - (m_TractionControl*m_FullTorqueOverAllWheels); 
    }

    private void FixedUpdate() {
        CalculateSpeed();
    }

    private void CalculateSpeed() {
        
        Vector3 CarVector = m_Rigidbody.transform.forward;
        Vector3 SpeedVector = m_Rigidbody.velocity;

        // Se l'angolo tra questi vettori è 0, l'auto si sta muovendo nella stessa direzione del vettore in avanti
        // Altrimenti è 180 e si sta muovendo all'indietro.
        float angle = Vector3.Angle(CarVector, SpeedVector);
        angle = Mathf.Round(angle);

        _currentSpeed = m_Rigidbody.velocity.magnitude * 2.23693629f;
        // _currentSpeed = m_Rigidbody.velocity.magnitude * 1.8f;
        
        if(angle > 45) {
            _currentSpeed *= -1;
        }            
    }

    public void Move(float steering, float accel, float footbrake, float handbrake) {
        // Aggiorna la posizione e la rotazione delle mesh delle ruote
        for (int i = 0; i < 4; i++) {
            Quaternion quat;
            Vector3 position;
            m_WheelColliders[i].GetWorldPose(out position, out quat);
            m_WheelMeshes[i].transform.position = position;
            m_WheelMeshes[i].transform.rotation = quat;
        }

        steering = Mathf.Clamp(steering, -1, 1); // Limita il valore di sterzata tra -1 e 1
        handbrake = Mathf.Clamp(handbrake, 0, 1); 

        // Imposta lo sterzo sulle ruote anteriori
        m_SteerAngle = steering*m_MaximumSteerAngle;
        m_WheelColliders[0].steerAngle = m_SteerAngle;
        m_WheelColliders[1].steerAngle = m_SteerAngle;

        ApplyDrive(accel, footbrake);
        CarSpeed();

        if (handbrake > 0f) {
            var hbTorque = handbrake*m_MaxHandbrakeTorque;
            m_WheelColliders[2].brakeTorque = hbTorque;
            m_WheelColliders[3].brakeTorque = hbTorque;
        }
    }

    private void ApplyDrive(float accel, float footbrake) {
        float thrustTorque; 

        thrustTorque = accel * (m_CurrentTorque / 2f);
        m_WheelColliders[2].motorTorque = m_WheelColliders[3].motorTorque = thrustTorque; // Applica la forza di accelerazione alle ruote posteriori

        // Frena se il pedale del freno è premuto
        for (int i = 0; i < 4; i++) {
            if (footbrake > 0) {
                // Applica la forza di frenata alle ruote
                m_WheelColliders[i].brakeTorque = m_BrakeTorque * footbrake;
            } else if (accel == 0 && Mathf.Abs(CurrentSpeed) > 2) {
                // Applica la forza di freno motore quando l'accelerazione è 0 e la velocità attuale è maggiore di 2
                m_WheelColliders[i].brakeTorque = m_engineBrakeTorque;
            } else if (accel != 0) {
                m_WheelColliders[i].brakeTorque = 0f;
                m_WheelColliders[i].motorTorque = 0f;

                if (CurrentSpeed < -1 && accel > 0 || CurrentSpeed > 1 && accel < 0) {
                    // Applica la forza di frenata quando la velocità attuale è negativa e l'accelerazione è positiva, o viceversa
                    m_WheelColliders[i].brakeTorque = m_BrakeTorque;
                } else if (CurrentSpeed < 0 && accel < 0) {
                    // Applica la forza di inversione quando la velocità attuale è negativa e l'accelerazione è negativa
                    m_WheelColliders[i].motorTorque = m_ReverseTorque * accel;
                } else {
                    // Applica la forza di accelerazione alle ruote
                    m_WheelColliders[i].motorTorque = m_CurrentTorque * accel;
                }
            }
        }
    }

    private void CarSpeed() {
        float speed = m_Rigidbody.velocity.magnitude;
        
        speed *= 2.23693629f;
        if (speed > m_TopSpeed)
            m_Rigidbody.velocity = (m_TopSpeed/2.23693629f) * m_Rigidbody.velocity.normalized;
    }                 
}