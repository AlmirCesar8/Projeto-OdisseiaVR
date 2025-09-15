using UnityEngine;

public class TeleportToTarget : MonoBehaviour
{
    public Transform teleportTarget;
    public GameObject xrRig;
    public ScreenFader screenFader;
    [TextArea]
    public string mensagemDoTeleport; // ⬅️ Mensagem personalizada

    public void Teleport()
    {
        if (teleportTarget != null && xrRig != null && screenFader != null)
        {
            screenFader.FadeAndTeleportWithRotation(teleportTarget, xrRig, mensagemDoTeleport);
        }
        else
        {
            Debug.LogWarning("Teleport falhou: faltando referência.");
        }
    }
}
