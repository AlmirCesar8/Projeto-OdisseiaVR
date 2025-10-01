using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

public class UILayoutManager : MonoBehaviour
{
    [Header("Prefabs")]
    [SerializeField] private GameObject questionTextPrefab; // Prefab do seu texto de pergunta
    [SerializeField] private GameObject answerButtonPrefab; // Prefab do seu botão de resposta

    [Header("Referências")]
    [SerializeField] private Transform playerCameraTransform; // Arraste a Main Camera do XR Origin aqui

    [Header("Parâmetros de Layout")]
    [Tooltip("Distância que a UI ficará do jogador em metros.")]
    [SerializeField] private float distanceFromPlayer = 2f;
    [Tooltip("Largura total do painel de UI.")]
    [SerializeField] private float panelWidth = 400f;
    [Tooltip("Altura do campo de texto da pergunta.")]
    [SerializeField] private float questionTextHeight = 100f;
    [Tooltip("Altura de cada botão de resposta.")]
    [SerializeField] private float buttonHeight = 50f;
    [Tooltip("Espaço vertical entre cada elemento da UI.")]
    [SerializeField] private float verticalSpacing = 10f;
    [Tooltip("Margem no topo antes do primeiro elemento.")]
    [SerializeField] private float topMargin = 20f;

    private List<GameObject> currentUIElements = new List<GameObject>();

    // Esta será a função pública que o TourManager irá chamar
    public void CreateQuizUI(Desafio desafio, System.Action<int> onAnswerSelectedCallback)
    {
        Debug.Log($"---> UILAYOUTMANAGER: Recebido pedido para criar UI para a pergunta: '{desafio.questionText}'"); // ADICIONE ESTA LINHA
        
        ClearUI();

        // 1. Posiciona o Canvas inteiro à frente do jogador
        transform.position = playerCameraTransform.position + playerCameraTransform.forward * distanceFromPlayer;
        transform.rotation = Quaternion.LookRotation(transform.position - playerCameraTransform.position);

        // --- Posição Inicial para o Layout ---
        // Começamos no topo do painel (Y=0, pois o pivô do pai está no centro) e descemos.
        float currentYPosition = 0;

        // 2. Cria e posiciona a Pergunta
        GameObject questionObject = Instantiate(questionTextPrefab, transform);
        currentUIElements.Add(questionObject);

        RectTransform questionRect = questionObject.GetComponent<RectTransform>();
        // Definindo o pivô para o topo simplifica o cálculo da posição
        questionRect.pivot = new Vector2(0.5f, 1f); 
        questionRect.sizeDelta = new Vector2(panelWidth, questionTextHeight);
        currentYPosition -= topMargin;
        questionRect.localPosition = new Vector3(0, currentYPosition, 0);

        questionObject.GetComponent<TextMeshProUGUI>().text = desafio.questionText;

        currentYPosition -= questionTextHeight; // Move para baixo para o próximo elemento

        // 3. Cria e posiciona os Botões em um loop
        for (int i = 0; i < desafio.answers.Count; i++)
        {
            GameObject buttonObject = Instantiate(answerButtonPrefab, transform);
            currentUIElements.Add(buttonObject);

            RectTransform buttonRect = buttonObject.GetComponent<RectTransform>();
            buttonRect.pivot = new Vector2(0.5f, 1f);
            buttonRect.sizeDelta = new Vector2(panelWidth, buttonHeight);

            currentYPosition -= verticalSpacing;
            buttonRect.localPosition = new Vector3(0, currentYPosition, 0);

            // Configura o texto e o clique do botão
            buttonObject.GetComponentInChildren<TextMeshProUGUI>().text = desafio.answers[i];
            int answerIndex = i;
            buttonObject.GetComponent<Button>().onClick.AddListener(() => onAnswerSelectedCallback(answerIndex));
            
            currentYPosition -= buttonHeight;
        }
    }

    public void ClearUI()
    {
        foreach (GameObject element in currentUIElements)
        {
            Destroy(element);
        }
        currentUIElements.Clear();
    }
}