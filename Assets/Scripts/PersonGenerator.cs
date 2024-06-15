using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class PersonGenerator : MonoBehaviour {
    public List<GameObject> personPrefabs;
    public int numberOfPeople; // Numero massimo di persone nella scena
    public Vector3 areaMin; // L'angolo in basso a sinistra dell'area
    public Vector3 areaMax; // L'angolo in alto a destra dell'area
    public float spawnInterval; // L'intervallo di tempo tra la generazione di persone

    private int currentNumberOfPeople;

    void Start() {
        currentNumberOfPeople = 0;
        StartCoroutine(SpawnPeople());
    }

    IEnumerator SpawnPeople() {
        while (true) {
            if (currentNumberOfPeople < numberOfPeople) {
                SpawnPerson();
                currentNumberOfPeople++;
            }

            yield return new WaitForSeconds(spawnInterval);
        }
    }

    void SpawnPerson() {
        if (personPrefabs.Count == 0) {
            return;
        }

        GameObject personPrefab = personPrefabs[Random.Range(0, personPrefabs.Count)];

        // Calcola una posizione casuale all'interno del rettangolo
        Vector3 randomPosition = new Vector3(
            Random.Range(areaMin.x, areaMax.x),
            areaMin.y, // Imposta la y all'altezza specificata
            Random.Range(areaMin.z, areaMax.z)
        );

        GameObject person = Instantiate(personPrefab, randomPosition, Quaternion.identity);
        RandomWalk randomWalk = person.AddComponent<RandomWalk>();
        randomWalk.Initialize(this, areaMin, areaMax);
        // Debug.Log("Spawned person at position: " + randomPosition);
    }

    public void PersonDestroyed() {
        currentNumberOfPeople--;
    }
}

public class RandomWalk : MonoBehaviour {
    public float speed = 1.5f; // Imposta una velocità predefinita se non impostata
    private Vector3 areaMin;
    private Vector3 areaMax;
    private PersonGenerator personGenerator;
    private Vector3 direction;

    public void Initialize(PersonGenerator generator, Vector3 min, Vector3 max) {
        personGenerator = generator;
        areaMin = min;
        areaMax = max;
        ChangeDirection();
        StartCoroutine(ChangeDirectionRoutine());
    }

    void Update() {
        transform.Translate(direction * speed * Time.deltaTime, Space.World);

        // Se la persona esce dall'area viene distrutta
        if (transform.position.x < areaMin.x || transform.position.x > areaMax.x ||
            transform.position.z < areaMin.z || transform.position.z > areaMax.z) {
            DestroyPerson();
        }
    }

    void OnCollisionEnter(Collision collision) {
        // Se la persona entra in collisione con un oggetto che NON ha il tag "Road", distruggila
        if (collision.gameObject.tag != "Road") {
            // Debug.Log("Person collided with " + collision.gameObject.name);
            DestroyPerson();
        }
    }

    IEnumerator ChangeDirectionRoutine() {
        while (true) {
            yield return new WaitForSeconds(Random.Range(1f, 3f));
            ChangeDirection();
        }
    }

    void ChangeDirection() {
        direction = new Vector3(
            Random.Range(-1f, 1f),
            0,
            Random.Range(-1f, 1f)
        ).normalized;

        // Ruota la persona per affrontare la direzione in cui sta camminando
        if (direction != Vector3.zero) {
            Quaternion toRotation = Quaternion.LookRotation(direction, Vector3.up);
            transform.rotation = toRotation;
        }
    }

    void DestroyPerson() {
        personGenerator.PersonDestroyed();
        Destroy(gameObject);
    }
}
