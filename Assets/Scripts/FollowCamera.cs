using UnityEngine;

public class FollowCamera : MonoBehaviour {
    public Transform target; // Il target che la telecamera seguirà
    private Vector3 offset; // Offset tra la telecamera e il target
    public float smoothSpeed = 0.125f; // Velocità di interpolazione per un movimento più fluido

    // Riferimento all'agente per il recupero dell'indice di posizione casuale
    public CarAgent carAgent; 
    public CarAgentDiagonal carAgentDiagonal;
    public CarAgentParallel carAgentParallel;

    private int randomPositionIndex;

    void FixedUpdate() {

        if (carAgent != null) { 
            randomPositionIndex = carAgent.RandomPositionIndex;
        } else if (carAgentDiagonal != null)  {
            randomPositionIndex = carAgentDiagonal.RandomPositionIndex;
        } else if (carAgentParallel != null) {
            randomPositionIndex = carAgentParallel.RandomPositionIndex;
        }

        if (randomPositionIndex == 0) { // Parcheggio a sinistra
            offset = new Vector3(-5f, 10f, -8f);
        } else { // Parcheggio a destra
            offset = new Vector3(5f, 10f, -8f);
        }

        if (target != null) {
            Vector3 desiredPosition = target.position + offset; // Posizione della camera
            Vector3 smoothedPosition = Vector3.Lerp(transform.position, desiredPosition, smoothSpeed); // Interpolazione per il movimento liscio
            transform.position = smoothedPosition;

            transform.LookAt(target); // Mantiene la camera puntata verso l'agente
        }
    }
}
