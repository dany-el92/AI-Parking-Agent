using UnityEngine;

public class VehicleCollision : MonoBehaviour {
    [HideInInspector] public VehicleSpawner vehicleSpawner; 

    void Start() {
        vehicleSpawner = FindObjectOfType<VehicleSpawner>(); 
    }
    
    void OnCollisionEnter(Collision collision) {
        if (collision.gameObject.CompareTag("Person")) {
            Destroy(collision.gameObject); // Distrugge il pedone
            vehicleSpawner.Vehicles.Remove(gameObject); // Rimuove il veicolo dalla lista
            Destroy(gameObject); // Distrugge il veicolo
        }
    }
}