using UnityEngine;

public class InitialRotation : MonoBehaviour
{
    [Tooltip("A rotação inicial desejada para este objeto no eixo Y.")]
    [SerializeField] private float initialYRotation = 0f;

    void Start()
    {
        // Pega a rotação atual do objeto
        Quaternion currentRotation = transform.rotation;
        
        // Converte para ângulos de Euler para modificar apenas o Y
        Vector3 eulerAngles = currentRotation.eulerAngles;
        
        // Aplica a rotação inicial desejada
        eulerAngles.y = initialYRotation;
        
        // Converte de volta para Quaternion e aplica ao objeto
        transform.rotation = Quaternion.Euler(eulerAngles);
    }
}