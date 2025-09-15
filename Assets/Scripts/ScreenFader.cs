using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

public class ScreenFader : MonoBehaviour
{
    public Image faderImage;
    public float fadeDuration = 0.5f;

    public TMP_Text teleportTMPMessage; // ← usa TMP
    public float messageDuration = 2f;

    private void Awake()
    {
        if (faderImage != null)
            faderImage.gameObject.SetActive(true);

        if (teleportTMPMessage != null)
            teleportTMPMessage.gameObject.SetActive(false); // esconde inicialmente
    }

public void FadeAndTeleportWithRotation(Transform target, GameObject xrRig, string mensagem)
{
    StartCoroutine(FadeRoutineWithRotation(target, xrRig, mensagem));
}

private IEnumerator FadeRoutineWithRotation(Transform target, GameObject xrRig, string mensagem)
{
    yield return StartCoroutine(Fade(1f));

    if (target != null && xrRig != null)
    {
        xrRig.transform.position = target.position;
        xrRig.transform.rotation = target.rotation;
    }

    yield return StartCoroutine(Fade(0f));

    ShowTeleportMessage(mensagem); // ⬅️ Mensagem personalizada aqui
}


    private IEnumerator Fade(float targetAlpha)
    {
        float startAlpha = faderImage.color.a;
        float time = 0f;

        while (time < fadeDuration)
        {
            time += Time.deltaTime;
            float alpha = Mathf.Lerp(startAlpha, targetAlpha, time / fadeDuration);
            faderImage.color = new Color(0f, 0f, 0f, alpha);
            yield return null;
        }

        faderImage.color = new Color(0f, 0f, 0f, targetAlpha);
    }

    // TMP → Exibir mensagem temporária
    private void ShowTeleportMessage(string message)
    {
        if (teleportTMPMessage != null)
        {
            teleportTMPMessage.text = message;
            teleportTMPMessage.gameObject.SetActive(true);
            StartCoroutine(HideMessageAfterDelay());
        }
    }

    private IEnumerator HideMessageAfterDelay()
    {
        yield return new WaitForSeconds(messageDuration);
        teleportTMPMessage.gameObject.SetActive(false);
    }
}
