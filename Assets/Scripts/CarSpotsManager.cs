using System.Collections.Generic;
using UnityEngine;

public class CarSpotsManager : MonoBehaviour {
    public GameObject[] carSpots;
    public GameObject targetPrefab; 
    [SerializeField] private int numberOfCarsToHide = 0; // Numero di macchine da nascondere
    public GameObject currentEnvironment; // Riferimento all'ambiente corrente
    private List<Vector3> initialCarPositions; // Lista per memorizzare le posizioni originali delle macchine

    void Start() {
        // Inizializza la lista delle posizioni originali delle macchine
        initialCarPositions = new List<Vector3>();
        foreach (GameObject carSpot in carSpots) {
            initialCarPositions.Add(carSpot.transform.position);
        }
    }

    // Nasconde un numero casuale di macchine e mostra i target corrispondenti
    public void HideRandomCarsAndShowTargets() {
        ShowAllCars(); 

        // Utilizza la posizione dell'ambiente corrente per la generazione delle posizioni casuali
        Vector3 environmentPosition = currentEnvironment.transform.position;

        // Trova il genitore dei target nell'ambiente corrente
        GameObject targetParent = currentEnvironment.transform.Find("Targets").gameObject;

        // Creo una lista di indici per i posti auto
        List<int> indexes = new List<int>();
        for (int i = 0; i < carSpots.Length; i++) {
            indexes.Add(i);
        }

        int numToHide = Mathf.Min(numberOfCarsToHide, carSpots.Length);
        for (int i = 0; i < numToHide; i++) {
            int indexToHide = Random.Range(0, indexes.Count); 

            GameObject carSpotToHide = carSpots[indexes[indexToHide]];
            Vector3 spotPosition = carSpotToHide.transform.position; // Recupero la posizione della macchina da nascondere
            Quaternion spotRotation = carSpotToHide.transform.rotation; // Recupero la rotazione della macchina da nascondere
            carSpotToHide.SetActive(false); // Nascondo la macchina

            // Istanzio il target come figlio del genitore dei target nell'ambiente corrente
            GameObject target = Instantiate(targetPrefab, spotPosition, spotRotation, targetParent.transform); 
            target.tag = "Finish"; // Assegno il tag "Finish" al target

            // Fisso l'altezza del target
            Vector3 targetPosition = target.transform.position;
            targetPosition.y = 0.24f; 
            target.transform.position = targetPosition;

            indexes.RemoveAt(indexToHide); 
        }
    }

    public void ShowAllCars() {
        for (int i = 0; i < carSpots.Length; i++) {
            carSpots[i].transform.position = initialCarPositions[i]; // Ripristina la posizione iniziale
            carSpots[i].SetActive(true); // Attiva la macchina
        }
    }

    public void DestroyTargets() {
        GameObject[] targets = GameObject.FindGameObjectsWithTag("Finish"); // Recupera tutti i target

        // Distruggi solo i target associati all'ambiente corrente
        foreach (GameObject target in targets) {
            if (target.transform.parent == currentEnvironment.transform.Find("Targets")) {
                Destroy(target);
            }
        }
    }
}
