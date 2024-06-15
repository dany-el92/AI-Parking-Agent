using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PedestrianSpawner : MonoBehaviour {
    public float WalkingDistance = 10f;
    public float SpawnTimer = 15f;
    public List<GameObject> PeoplePrefabs;

    private float timer = 0f; // Timer per il tempo di spawn
    public List<GameObject> People = new List<GameObject>(); // Lista di persone in movimento
    private Vector3 SpawnPosition; 
    private Quaternion SpawnRotation; 

    void Start() {
        SpawnPosition = gameObject.transform.position; 
        SpawnRotation = gameObject.transform.rotation; 

        SpawnPerson();
    }

    void Update() {
        List<GameObject> peopleToRemove = new List<GameObject>();

        timer += Time.deltaTime; // Aggiorna il timer

        // Se il timer supera il tempo di spawn massimo, genero una nuova persona
        if (timer >= SpawnTimer) {
            timer = 0f; 
            SpawnPerson(); 
        }

        foreach (GameObject person in People) {
            if (person != null) {
                float distanceWalked = Mathf.Abs(SpawnPosition.z - person.transform.position.z); // Calcolo la distanza percorsa dalla persona

                // Se la persona ha superato la distanza massima di camminata, viene rimossa
                if (distanceWalked > WalkingDistance) {
                    peopleToRemove.Add(person);
                } else {
                    // Altrimenti, la persona continua a muoversi
                    float speed = Random.Range(1f, 2.5f);
                    person.transform.Translate(Vector3.forward * speed * Time.deltaTime);
                }
            }
        }

        foreach (GameObject person in peopleToRemove) {
            DeletePerson(person);
        }
    }
    private void SpawnPerson() {
        // Scegli un prefab casuale dalla lista
        int randomIndex = Random.Range(0, PeoplePrefabs.Count);
        GameObject newPerson = Instantiate(PeoplePrefabs[randomIndex], SpawnPosition, SpawnRotation); 
        newPerson.tag = "Person"; // Assegna il tag "Person" alla persona appena generata

        newPerson.AddComponent<VehicleCollision>();

        People.Add(newPerson); 
    }

    private void DeletePerson(GameObject Person) {
        People.Remove(Person); 
        Destroy(Person); 
    }
}
