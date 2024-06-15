using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.MLAgents;
using Unity.MLAgents.Sensors;
using Unity.MLAgents.Actuators;
using System.Linq; 

public class VehicleSpawner : MonoBehaviour {
    public float DrivingDistance = 20f; // Distanza massima di guida in metri
    public float DrivingSpeed = 1.5f; // Velocità in metri al secondo
    public float DrivingSpeedVariance = 1.5f; // Varianza della velocità in metri al secondo 
    public float SpawnTimer = 2f; // Tempo di spawn in secondi (ogni quanto tempo viene generata un nuovo veicolo)
    public List<GameObject> VehiclePrefabs; // Lista dei prefabs dei veicoli
    private float timer = 0f; // Timer per il tempo di spawn
    public List<GameObject> Vehicles = new List<GameObject>(); // Lista dei veicoli in movimento
    private Vector3 SpawnPosition; 
    private Quaternion SpawnRotation; 

    void Start() {
        SpawnPosition = gameObject.transform.position; 
        SpawnRotation = gameObject.transform.rotation; 

        SpawnVehicle();
    }

    void Update() {
        timer += Time.deltaTime; // Aggiorna il timer

        // Se il timer supera il tempo di spawn massimo, genero un nuovo veicolo
        if (timer >= SpawnTimer) {
            timer = 0f; 
            SpawnVehicle(); 
        }

        foreach (GameObject vehicle in Vehicles) {
            if (vehicle != null) {
                float DistanceDriven = Mathf.Abs(SpawnPosition.x - vehicle.transform.position.x); // Calcolo la distanza percorsa dal veicolo

                // Se il veicolo ha superato la distanza massima di guida, viene rimosso
                if (DistanceDriven > DrivingDistance) {
                    DeleteVehicle(vehicle);
                    break; 
                } else {
                    // Altrimenti, il veicolo continua a muoversi
                    float speed = DrivingSpeed + Random.Range(-DrivingSpeedVariance, DrivingSpeedVariance);

                    // Ottieni il componente RayPerceptionSensorComponent3D
                    var raySensor = vehicle.GetComponent<RayPerceptionSensorComponent3D>();
                    if (raySensor != null) {
                        RayPerceptionInput rayPerceptionIn = raySensor.GetRayPerceptionInput();
                        RayPerceptionOutput rayPerceptionOut = RayPerceptionSensor.Perceive(rayPerceptionIn);
                        RayPerceptionOutput.RayOutput[] rayOutputs = rayPerceptionOut.RayOutputs;

                        bool hitPerson = false;
                        foreach (var rayOutput in rayOutputs) {
                            // Debug.Log(rayOutput.HitFraction);
                            if (rayOutput.HasHit && rayOutput.HitGameObject != null && rayOutput.HitGameObject.CompareTag("Person") && rayOutput.HitFraction < 0.4f) {
                                hitPerson = true;
                                break;
                            }
                        }

                        if (!hitPerson) {
                            vehicle.transform.Translate(Vector3.forward * speed * Time.deltaTime); // Muovi il veicolo in avanti
                        }
                    } else {
                        vehicle.transform.Translate(Vector3.forward * speed * Time.deltaTime); // Muovi il veicolo in avanti
                    }

                }
            }
        }
    }

    private void SpawnVehicle() {
        // Scegli un prefab casuale dalla lista
        int randomIndex = Random.Range(0, VehiclePrefabs.Count);
        GameObject newVehicle = Instantiate(VehiclePrefabs[randomIndex], SpawnPosition, SpawnRotation); 
        newVehicle.tag = "Vehicle"; // Assegna il tag "Vehicle" al veicolo appena generato

        // Aggiungi lo script di collisione al veicolo
        newVehicle.AddComponent<VehicleCollision>();

        Vehicles.Add(newVehicle); 
    }

    private void DeleteVehicle(GameObject Vehicle) {
        Vehicles.Remove(Vehicle); 
        Destroy(Vehicle); 
    }
}
