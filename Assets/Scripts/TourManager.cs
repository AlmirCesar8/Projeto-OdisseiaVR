using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;
using TMPro;

// --- ESTRUTURAS DE DADOS PARA O RUNTIME (O QUE O JOGO USA) ---
public class Desafio
{
    public Material panoramaMaterial;
    public float initialYRotation;
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
    public float initialYRotation;
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
    public Image fadeScreen; 

    [Header("Configurações de Feedback")]
    public Color correctColor = new Color(0.1f, 0.7f, 0.2f);
    public Color incorrectColor = new Color(0.8f, 0.2f, 0.1f);
    public Color normalColor = Color.white;
    public float feedbackDelay = 1.5f;

    [Header("Configurações de Transição de Local")]
    public AudioClip locationVictorySound;
    public float waitOnBlackScreenDelay = 1.0f; 
    public float fadeDuration = 0.8f;          

    [Header("Configurações de Áudio")]
    [Range(0f, 1f)] public float backgroundMusicVolume = 0.5f; 
    [Range(0f, 1f)] public float sfxVolume = 1.0f;             
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
        
        if (fadeScreen == null) {
             Debug.LogError("ERRO CRÍTICO: A 'Fade Screen' não foi atribuída no Inspector!");
             enabled = false;
             return;
        }
        
        // Garante que a tela comece transparente, independentemente do seu estado no Editor
        Color tempColor = fadeScreen.color;
        tempColor.a = 0;
        fadeScreen.color = tempColor;

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
            StartCoroutine(TransitionToLocal(currentLocalIndex, true));
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
            DadosLocal novoLocal = new DadosLocal
            {
                locationName = localJson.locationName,
                backgroundMusic = Resources.Load<AudioClip>(localJson.backgroundMusicPath),
                desafios = new List<Desafio>()
            };

            foreach(var desafioJson in localJson.desafios)
            {
                Desafio novoDesafio = new Desafio
                {
                    panoramaMaterial = Resources.Load<Material>(desafioJson.panoramaMaterialPath),
                    initialYRotation = desafioJson.initialYRotation,
                    questionText = desafioJson.questionText,
                    answers = desafioJson.answers,
                    correctAnswerIndex = desafioJson.correctAnswerIndex
                };
                novoLocal.desafios.Add(novoDesafio);

                if (novoDesafio.panoramaMaterial == null) 
                    Debug.LogWarning($"Asset não encontrado em 'Resources/{desafioJson.panoramaMaterialPath}' para o local '{novoLocal.locationName}'");
            }

            if (novoLocal.backgroundMusic == null && !string.IsNullOrEmpty(localJson.backgroundMusicPath)) 
                Debug.LogWarning($"Asset não encontrado em 'Resources/{localJson.backgroundMusicPath}' para o local '{novoLocal.locationName}'");

            locais.Add(novoLocal);
        }
    }

    void CarregarDadosDoLocal(int localIndex)
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
            audioSource.volume = backgroundMusicVolume;
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
        isAnswering = true; 

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
    
    // --- REATORAÇÃO (Refactoring) ---
    // NOVO: Corrotina dedicada a escurecer a tela (Fade Out) de forma determinística.
    private IEnumerator FadeOut() {
        Color currentColor = fadeScreen.color;
        float startAlpha = 0f; // MODIFICADO: Força o início a ser transparente
        float targetAlpha = 1f;
        float timer = 0f;

        while (timer < fadeDuration) {
            timer += Time.deltaTime;
            float progress = Mathf.Clamp01(timer / fadeDuration);
            currentColor.a = Mathf.Lerp(startAlpha, targetAlpha, progress);
            fadeScreen.color = currentColor;
            yield return null; 
        }

        currentColor.a = targetAlpha;
        fadeScreen.color = currentColor;
    }

    // NOVO: Corrotina dedicada a clarear a tela (Fade In) de forma determinística.
    private IEnumerator FadeIn() {
        Color currentColor = fadeScreen.color;
        float startAlpha = 1f; // MODIFICADO: Força o início a ser preto
        float targetAlpha = 0f;
        float timer = 0f;

        while (timer < fadeDuration) {
            timer += Time.deltaTime;
            float progress = Mathf.Clamp01(timer / fadeDuration);
            currentColor.a = Mathf.Lerp(startAlpha, targetAlpha, progress);
            fadeScreen.color = currentColor;
            yield return null;
        }

        currentColor.a = targetAlpha;
        fadeScreen.color = currentColor;
    }

    // MODIFICADO: A rotina de transição agora usa as novas funções de FadeIn e FadeOut.
    private IEnumerator TransitionToLocal(int localIndex, bool isFirstLoad = false) {
        if (!isFirstLoad) {
            yield return StartCoroutine(FadeOut()); // Usa a nova função
            audioSource.Stop(); 
            if (locationVictorySound != null) audioSource.PlayOneShot(locationVictorySound, sfxVolume);
            yield return new WaitForSeconds(waitOnBlackScreenDelay);
        }

        CarregarDadosDoLocal(localIndex);

        // Para o primeiro carregamento, a tela já está transparente, então não precisa de fade in.
        // Para os demais, a tela estará preta, então clareamos.
        if (!isFirstLoad) {
            yield return StartCoroutine(FadeIn()); // Usa a nova função
        }
    }

    private IEnumerator HandleCorrectAnswer(Button correctButton)
    {
        correctButton.GetComponent<Image>().color = correctColor;
        if(correctAnswerSound != null) audioSource.PlayOneShot(correctAnswerSound, sfxVolume); 
        
        yield return new WaitForSeconds(feedbackDelay);

        currentDesafioIndex++;
        if (currentDesafioIndex >= locais[currentLocalIndex].desafios.Count)
        {
            int proximoLocalIndex = (currentLocalIndex + 1) % locais.Count;
            yield return StartCoroutine(TransitionToLocal(proximoLocalIndex)); 
        }
        else
        {
            ApresentarDesafio();
        }
        isAnswering = false;
    }

    private IEnumerator HandleIncorrectAnswer(Button incorrectButton)
    {
        incorrectButton.GetComponent<Image>().color = incorrectColor;
        if(incorrectAnswerSound != null) audioSource.PlayOneShot(incorrectAnswerSound, sfxVolume);
        yield return new WaitForSeconds(feedbackDelay);
        
        for (int i = 0; i < locais[currentLocalIndex].desafios[currentDesafioIndex].answers.Count; i++)
        {
             answerButtons[i].GetComponent<Image>().color = normalColor;
             answerButtons[i].interactable = true;
        }
        isAnswering = false;
    }
}