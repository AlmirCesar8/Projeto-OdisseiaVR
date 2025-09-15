using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using System.Collections;

public class GazeTeleport : MonoBehaviour
{
    public Transform teleportTarget;
    public GameObject xrRig;
    public float gazeTime = 2.0f;  // tempo de espera com o olhar
    private Coroutine gazeCoroutine;

    public void OnHoverEnter()
    {
        gazeCoroutine = StartCoroutine(StartGaze());
    }

    public void OnHoverExit()
    {
        if (gazeCoroutine != null)
        {
            StopCoroutine(gazeCoroutine);
        }
    }

    private IEnumerator StartGaze()
    {
        yield return new WaitForSeconds(gazeTime);
        if (xrRig != null && teleportTarget != null)
        {
            xrRig.transform.position = teleportTarget.position;
        }
    }
}
