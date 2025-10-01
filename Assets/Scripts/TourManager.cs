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

/// <summary>
/// Gerencia a lógica principal do tour virtual. Versão modificada para ser independente
/// da funcionalidade de fade, ideal para testes de lógica central.
/// </summary>
[RequireComponent(typeof(AudioSource))]
public class TourManager : MonoBehaviour
{
    [Header("Arquivo de Conteúdo")]
    public TextAsset tourDataJson;

    [Header("Referências da Cena")]
    public Renderer panoramaSphereRenderer;
    public Image fadeScreen; // MODIFICADO: Este campo PODE ser deixado vazio.

    [Header("Gerenciadores Externos")]
    public UILayoutManager uiLayoutManager;

    [Header("Configurações de Feedback")]
    public float feedbackDelay = 1.5f;

    [Header("Configurações de Transição")]
    public AudioClip locationVictorySound;
    public float waitOnBlackScreenDelay = 1.0f;
    public float fadeDuration = 0.8f;

    [Header("Configurações de Áudio")]
    [Range(0f, 1f)] public float backgroundMusicVolume = 0.5f;
    [Range(0f, 1f)] public float sfxVolume = 1.0f;
    public AudioClip correctAnswerSound;
    public AudioClip incorrectAnswerSound;

    // --- Variáveis Privadas de Estado ---
    private List<DadosLocal> locais = new List<DadosLocal>();
    private int currentLocalIndex = 0;
    private int currentDesafioIndex = 0;
    private bool isAnswering = false;
    private AudioSource audioSource;
    
    void Start()
    {
        audioSource = GetComponent<AudioSource>();

        // MODIFICADO: A validação agora ignora o 'fadeScreen'.
        if (tourDataJson == null || panoramaSphereRenderer == null || uiLayoutManager == null)
        {
            Debug.LogError("ERRO CRÍTICO: Uma ou mais referências essenciais (JSON, Renderer ou UI Manager) não foram atribuídas no Inspector!");
            enabled = false;
            return;
        }

        LoadTourDataFromJSON();

        if (locais.Count > 0)
        {
            StartCoroutine(TransitionToLocal(currentLocalIndex, true));
        }
        else
        {
            Debug.LogError("Nenhum local foi carregado do arquivo JSON. Verifique o arquivo e a sua sintaxe.");
        }
    }

    /// <summary>
    /// Lê o arquivo JSON, converte para objetos C# e popula a lista de locais.
    /// Esta é a versão completa e funcional.
    /// </summary>
    void LoadTourDataFromJSON()
    {
        // Garante que o arquivo de texto foi atribuído
        if (tourDataJson == null)
        {
            Debug.LogError("ERRO CRÍTICO: O arquivo 'tourDataJson' não foi atribuído no Inspector!");
            enabled = false;
            return;
        }

        // Tenta converter o texto do JSON para nossa estrutura de classes C#
        TourDataJson dataFromJson = JsonUtility.FromJson<TourDataJson>(tourDataJson.text);
        if (dataFromJson == null || dataFromJson.locais == null)
        {
            Debug.LogError("Falha ao parsear o JSON. Verifique a sintaxe do arquivo e a correspondência com as classes C#.");
            return;
        }

        // Itera sobre cada "local" encontrado no JSON
        foreach (var localJson in dataFromJson.locais)
        {
            DadosLocal novoLocal = new DadosLocal
            {
                locationName = localJson.locationName,
                backgroundMusic = Resources.Load<AudioClip>(localJson.backgroundMusicPath),
                desafios = new List<Desafio>()
            };

            // Itera sobre cada "desafio" dentro do local atual
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
                // Adiciona o novo desafio à lista de desafios do local atual
                novoLocal.desafios.Add(novoDesafio);

                if (novoDesafio.panoramaMaterial == null) 
                    Debug.LogWarning($"Asset de Material não encontrado em 'Resources/{desafioJson.panoramaMaterialPath}'");
            }

            if (novoLocal.backgroundMusic == null && !string.IsNullOrEmpty(localJson.backgroundMusicPath)) 
                Debug.LogWarning($"Asset de Áudio não encontrado em 'Resources/{localJson.backgroundMusicPath}'");

            // ESTA É A LINHA CRUCIAL: Adiciona o local totalmente montado à lista principal.
            locais.Add(novoLocal);
        }
        
        // Log final para verificar o resultado
        Debug.Log($"Dados carregados! {locais.Count} locais encontrados.");
    }

    void CarregarDadosDoLocal(int localIndex)
    {
        // (O conteúdo deste método não muda)
        currentLocalIndex = localIndex;
        currentDesafioIndex = 0;
        if (locais[currentLocalIndex].backgroundMusic != null)
        {
            audioSource.clip = locais[currentLocalIndex].backgroundMusic;
            audioSource.volume = backgroundMusicVolume;
            audioSource.loop = true;
            audioSource.Play();
        }
        else { audioSource.Stop(); }
        ApresentarDesafio();
    }
    
    void ApresentarDesafio()
    {
        // (O conteúdo deste método não muda)
        isAnswering = false;
        Desafio desafioAtual = locais[currentLocalIndex].desafios[currentDesafioIndex];
        panoramaSphereRenderer.transform.rotation = Quaternion.Euler(0, desafioAtual.initialYRotation, 0);
        panoramaSphereRenderer.material = desafioAtual.panoramaMaterial;
        uiLayoutManager.CreateQuizUI(desafioAtual, CheckAnswer);
    }
    
    public void CheckAnswer(int selectedIndex)
    {
        // (O conteúdo deste método não muda)
        if (isAnswering) return;
        isAnswering = true;
        Desafio desafioAtual = locais[currentLocalIndex].desafios[currentDesafioIndex];
        if (selectedIndex == desafioAtual.correctAnswerIndex) { StartCoroutine(HandleCorrectAnswer()); }
        else { StartCoroutine(HandleIncorrectAnswer()); }
    }

    private IEnumerator HandleCorrectAnswer()
    {
        // (O conteúdo deste método não muda)
        if (correctAnswerSound != null) audioSource.PlayOneShot(correctAnswerSound, sfxVolume);
        yield return new WaitForSeconds(feedbackDelay);
        currentDesafioIndex++;
        if (currentDesafioIndex >= locais[currentLocalIndex].desafios.Count)
        {
            int proximoLocalIndex = (currentLocalIndex + 1) % locais.Count;
            yield return StartCoroutine(TransitionToLocal(proximoLocalIndex));
        }
        else { ApresentarDesafio(); }
    }

    private IEnumerator HandleIncorrectAnswer()
    {
        // (O conteúdo deste método não muda)
        if (incorrectAnswerSound != null) audioSource.PlayOneShot(incorrectAnswerSound, sfxVolume);
        yield return new WaitForSeconds(feedbackDelay);
        ApresentarDesafio();
    }
    
    private IEnumerator TransitionToLocal(int localIndex, bool isFirstLoad = false)
    {
        if (!isFirstLoad)
        {
            // MODIFICADO: A chamada para FadeOut foi desativada com '//'.
            // yield return StartCoroutine(FadeOut());
            audioSource.Stop();
            if (locationVictorySound != null) audioSource.PlayOneShot(locationVictorySound, sfxVolume);
            yield return new WaitForSeconds(waitOnBlackScreenDelay);
        }

        CarregarDadosDoLocal(localIndex);

        if (!isFirstLoad)
        {
            // MODIFICADO: A chamada para FadeIn foi desativada com '//'.
            // yield return StartCoroutine(FadeIn());
        }
    }
    
    // As corrotinas de Fade ainda existem mas não são chamadas.
    private IEnumerator Fade(float targetAlpha) { /* ... */ yield return null; }
    private IEnumerator FadeOut() { yield return Fade(1f); }
    private IEnumerator FadeIn() { yield return Fade(0f); }
}

// NOTE: As definições das classes de dados (Desafio, DadosLocal, DesafioJson, etc.)
// foram omitidas aqui por brevidade, mas devem ser mantidas no seu arquivo como estão.