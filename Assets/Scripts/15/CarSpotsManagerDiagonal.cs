using System.Collections.Generic;
using UnityEngine;

public class CarSpotsManagerDiagonal : MonoBehaviour {
    public GameObject[] carSpots;
    public GameObject targetPrefab; 
    [SerializeField] private int numberOfCarsToHide = 0; // Numero di macchine da nascondere
    public GameObject currentEnvironment; // Riferimento all'ambiente corrente
    private int randomPositionIndex;
    public int RandomPositionIndex{ get { return randomPositionIndex; }} 
    private List<Vector3> initialCarPositions; // Lista per memorizzare le posizioni originali delle macchine

    void Start() {
        // Inizializza la lista delle posizioni originali delle macchine
        initialCarPositions = new List<Vector3>();
        foreach (GameObject carSpot in carSpots) {
            initialCarPositions.Add(carSpot.transform.position);
        }
    }

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
            int indexToHide = Random.Range(0, indexes.Count); // Scelgo un indice casuale

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

        randomPositionIndex = Random.Range(0, 2);
        
        for(int i = 0; i < carSpots.Length; i++) {
            carSpots[i].transform.position = initialCarPositions[i];
            carSpots[i].SetActive(true);

            if(randomPositionIndex == 0) { // Parcheggio a sinistra 
                carSpots[i].transform.rotation = Quaternion.Euler(0f, 15f, 0f);
            } else { // Parcheggio a destra
                carSpots[i].transform.rotation = Quaternion.Euler(0f, -15f, 0f);
            }
        }

        // Recupera gli oggetti con tag "Stripe" che sono figli del componente padre "Stripes"
        Transform[] allChildren = currentEnvironment.GetComponentsInChildren<Transform>();
        List<GameObject> stripeObjects = new List<GameObject>();
        foreach(Transform child in allChildren) {
            if(child.CompareTag("Stripe")) {
                stripeObjects.Add(child.gameObject);
            }
        }
        
        foreach(GameObject stripe in stripeObjects) {

            if(randomPositionIndex == 0) { // Parcheggio a sinistra 
                stripe.transform.rotation = Quaternion.Euler(0f, 15f, 0f);
            } else { // Parcheggio a destra
                stripe.transform.rotation = Quaternion.Euler(0f, -15f, 0f);
            }
        }
        
    }

    public void DestroyTargets() {
        GameObject[] targets = GameObject.FindGameObjectsWithTag("Finish"); // Recupera tutti i target

        // Distruggi solo i target associati all'ambiente corrente
        foreach(GameObject target in targets) {
            if (target.transform.parent == currentEnvironment.transform.Find("Targets")) {
                Destroy(target);
            }
        }
    }
}
