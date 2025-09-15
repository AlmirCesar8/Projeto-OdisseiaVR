using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;
using TMPro;

// --- ESTRUTURAS DE DADOS PARA O RUNTIME (O QUE O JOGO USA) ---
public class Desafio
{
    public Material panoramaMaterial;
    public float initialYRotation; // Novo campo para rotação
    public string questionText;
    public List<string> answers;
    public int correctAnswerIndex;
}

public class DadosLocal
{
    public string locationName;
    public AudioClip backgroundMusic;
    public List<Desafio> desafios;
}

// --- ESTRUTURAS DE DADOS PARA O JSON (O QUE O ARQUIVO CONTÉM) ---
[System.Serializable]
public class DesafioJson
{
    public string panoramaMaterialPath;
    public float initialYRotation; // Novo campo para rotação
    public string questionText;
    public List<string> answers;
    public int correctAnswerIndex;
}

[System.Serializable]
public class DadosLocalJson
{
    public string locationName;
    public string backgroundMusicPath;
    public List<DesafioJson> desafios;
}

[System.Serializable]
public class TourDataJson
{
    public List<DadosLocalJson> locais;
}

[RequireComponent(typeof(AudioSource))]
public class TourManager : MonoBehaviour
{
    [Header("Arquivo de Conteúdo")]
    public TextAsset tourDataJson;

    [Header("Referências da Cena")]
    public Renderer panoramaSphereRenderer;
    public TextMeshProUGUI questionTextUI;
    public List<Button> answerButtons;

    [Header("Configurações de Feedback")]
    public Color correctColor = new Color(0.1f, 0.7f, 0.2f);
    public Color incorrectColor = new Color(0.8f, 0.2f, 0.1f);
    public Color normalColor = Color.white;
    public float feedbackDelay = 1.5f;

    [Header("Configurações de Transição de Local")]
    public AudioClip locationVictorySound; // Novo som de vitória
    public float locationTransitionDelay = 2.0f; // Novo delay

    [Header("Configurações de Áudio")]
    public AudioClip correctAnswerSound;
    public AudioClip incorrectAnswerSound;
    
    private List<DadosLocal> locais = new List<DadosLocal>();
    private int currentLocalIndex = 0;
    private int currentDesafioIndex = 0;
    private bool isAnswering = false;
    private AudioSource audioSource;

    void Start()
    {
        audioSource = GetComponent<AudioSource>();
        LoadTourDataFromJSON();

        for (int i = 0; i < answerButtons.Count; i++)
        {
            int index = i;
            if (answerButtons[i] != null) {
                answerButtons[i].onClick.AddListener(() => CheckAnswer(index));
            }
        }

        if (locais.Count > 0)
        {
            CarregarLocal(currentLocalIndex);
        }
    }

    void LoadTourDataFromJSON()
    {
        if (tourDataJson == null)
        {
            Debug.LogError("ERRO CRÍTICO: O arquivo 'tourDataJson' não foi atribuído no Inspector!");
            enabled = false;
            return;
        }

        TourDataJson dataFromJson = JsonUtility.FromJson<TourDataJson>(tourDataJson.text);

        foreach (var localJson in dataFromJson.locais)
        {
            DadosLocal novoLocal = new DadosLocal();
            novoLocal.locationName = localJson.locationName;
            novoLocal.backgroundMusic = Resources.Load<AudioClip>(localJson.backgroundMusicPath);
            novoLocal.desafios = new List<Desafio>();

            foreach(var desafioJson in localJson.desafios)
            {
                Desafio novoDesafio = new Desafio();
                novoDesafio.panoramaMaterial = Resources.Load<Material>(desafioJson.panoramaMaterialPath);
                novoDesafio.initialYRotation = desafioJson.initialYRotation; // Carrega a rotação
                novoDesafio.questionText = desafioJson.questionText;
                novoDesafio.answers = desafioJson.answers;
                novoDesafio.correctAnswerIndex = desafioJson.correctAnswerIndex;
                novoLocal.desafios.Add(novoDesafio);

                if (novoDesafio.panoramaMaterial == null) 
                    Debug.LogWarning($"Asset não encontrado em 'Resources/{desafioJson.panoramaMaterialPath}' para o local '{novoLocal.locationName}'");
            }

            if (novoLocal.backgroundMusic == null && !string.IsNullOrEmpty(localJson.backgroundMusicPath)) 
                Debug.LogWarning($"Asset não encontrado em 'Resources/{localJson.backgroundMusicPath}' para o local '{novoLocal.locationName}'");

            locais.Add(novoLocal);
        }
    }

    void CarregarLocal(int localIndex)
    {
        if(locais.Count == 0 || localIndex >= locais.Count) {
             Debug.LogError("Tentativa de carregar um local inválido.");
             return;
        }
        currentLocalIndex = localIndex;
        currentDesafioIndex = 0;

        if (locais[currentLocalIndex].backgroundMusic != null)
        {
            audioSource.clip = locais[currentLocalIndex].backgroundMusic;
            audioSource.loop = true;
            audioSource.Play();
        } else {
            audioSource.Stop();
        }
        ApresentarDesafio();
    }

    void ApresentarDesafio()
    {
        Desafio desafioAtual = locais[currentLocalIndex].desafios[currentDesafioIndex];

        // Aplica a rotação inicial e o material
        panoramaSphereRenderer.transform.rotation = Quaternion.Euler(0, desafioAtual.initialYRotation, 0);
        panoramaSphereRenderer.material = desafioAtual.panoramaMaterial;
        
        questionTextUI.text = desafioAtual.questionText;

        for (int i = 0; i < answerButtons.Count; i++)
        {
            if (i < desafioAtual.answers.Count)
            {
                answerButtons[i].gameObject.SetActive(true);
                answerButtons[i].GetComponent<Image>().color = normalColor;
                answerButtons[i].interactable = true;
                answerButtons[i].GetComponentInChildren<TextMeshProUGUI>().text = desafioAtual.answers[i];
            }
            else
            {
                answerButtons[i].gameObject.SetActive(false);
            }
        }
    }

    public void CheckAnswer(int selectedIndex)
    {
        if (isAnswering) return;
        Desafio desafioAtual = locais[currentLocalIndex].desafios[currentDesafioIndex];
        for(int i = 0; i < desafioAtual.answers.Count; i++) {
             answerButtons[i].interactable = false;
        }

        if (selectedIndex == desafioAtual.correctAnswerIndex)
        {
            StartCoroutine(HandleCorrectAnswer(answerButtons[selectedIndex]));
        }
        else
        {
            StartCoroutine(HandleIncorrectAnswer(answerButtons[selectedIndex]));
        }
    }

    private IEnumerator HandleCorrectAnswer(Button correctButton)
    {
        isAnswering = true;
        correctButton.GetComponent<Image>().color = correctColor;
        if(correctAnswerSound != null) audioSource.PlayOneShot(correctAnswerSound);
        
        yield return new WaitForSeconds(feedbackDelay);

        currentDesafioIndex++;
        if (currentDesafioIndex >= locais[currentLocalIndex].desafios.Count)
        {
            // Lógica de vitória do local
            if (locationVictorySound != null) audioSource.PlayOneShot(locationVictorySound);
            yield return new WaitForSeconds(locationTransitionDelay); // Espera o novo delay

            int proximoLocalIndex = (currentLocalIndex + 1) % locais.Count;
            CarregarLocal(proximoLocalIndex);
        }
        else
        {
            ApresentarDesafio();
        }
        isAnswering = false;
    }

    private IEnumerator HandleIncorrectAnswer(Button incorrectButton)
    {
        isAnswering = true;
        incorrectButton.GetComponent<Image>().color = incorrectColor;
        if(incorrectAnswerSound != null) audioSource.PlayOneShot(incorrectAnswerSound);
        yield return new WaitForSeconds(feedbackDelay);
        
        for (int i = 0; i < locais[currentLocalIndex].desafios[currentDesafioIndex].answers.Count; i++)
        {
             answerButtons[i].GetComponent<Image>().color = normalColor;
             answerButtons[i].interactable = true;
        }
        isAnswering = false;
    }
}

