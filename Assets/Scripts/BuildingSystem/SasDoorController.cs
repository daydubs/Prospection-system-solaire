using System.Collections;
using UnityEngine;
using InventoryFramework;

public class SasDoorController : MonoBehaviour, IPromptInteractable
{
    [Header("Door Bones")]
    [SerializeField] private Transform doorLeftBone;
    [SerializeField] private Transform doorRightBone;

    [Header("Collision")]
    [Tooltip("Optional solid collider that blocks passage while door is closed.")]
    [SerializeField] private Collider doorBlockerCollider;

    [Header("Motion")]
    [SerializeField] private float moveDuration = 1.2f;
    [SerializeField] private float slideDistance = 0.0053f;

    [Header("Proximity Trigger")]
    [SerializeField] private bool autoClose = true;
    [SerializeField] private float autoCloseDelay = 2.0f;

    [Header("Interaction Prompt")]
    [SerializeField] private string openPrompt = "[E] Ouvrir le sas";
    [SerializeField] private string closePrompt = "[E] Fermer le sas";

    public bool IsOpen { get; private set; } = false;

    private Vector3 initialPosL;
    private Vector3 initialPosR;
    private Coroutine currentRoutine;
    private int triggerOccupants = 0;

    private void Awake()
    {
        if (doorLeftBone != null)
            initialPosL = doorLeftBone.localPosition;
        if (doorRightBone != null)
            initialPosR = doorRightBone.localPosition;
    }

    public void SetBones(Transform left, Transform right)
    {
        doorLeftBone = left;
        doorRightBone = right;
        if (doorLeftBone != null) initialPosL = doorLeftBone.localPosition;
        if (doorRightBone != null) initialPosR = doorRightBone.localPosition;
    }

    public void SetBlockerCollider(Collider col)
    {
        doorBlockerCollider = col;
    }

    public void Interact()
    {
        ToggleDoor();
    }

    public string GetInteractionPrompt()
    {
        return IsOpen ? closePrompt : openPrompt;
    }

    public void ToggleDoor()
    {
        if (IsOpen) CloseDoor();
        else OpenDoor();
    }

    public void OpenDoor()
    {
        if (IsOpen) return;
        IsOpen = true;

        if (doorBlockerCollider != null)
            doorBlockerCollider.enabled = false;

        if (currentRoutine != null) StopCoroutine(currentRoutine);
        currentRoutine = StartCoroutine(AnimateDoor(true));
    }

    public void CloseDoor()
    {
        if (!IsOpen) return;
        IsOpen = false;

        if (currentRoutine != null) StopCoroutine(currentRoutine);
        currentRoutine = StartCoroutine(AnimateDoor(false));
    }

    private IEnumerator AnimateDoor(bool opening)
    {
        if (doorLeftBone == null || doorRightBone == null)
            yield break;

        Vector3 targetL = initialPosL + (opening ? new Vector3(slideDistance, 0, 0) : Vector3.zero);
        Vector3 targetR = initialPosR + (opening ? new Vector3(-slideDistance, 0, 0) : Vector3.zero);

        Vector3 startL = doorLeftBone.localPosition;
        Vector3 startR = doorRightBone.localPosition;

        float elapsed = 0f;
        while (elapsed < moveDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.SmoothStep(0f, 1f, elapsed / moveDuration);
            doorLeftBone.localPosition = Vector3.Lerp(startL, targetL, t);
            doorRightBone.localPosition = Vector3.Lerp(startR, targetR, t);
            yield return null;
        }

        doorLeftBone.localPosition = targetL;
        doorRightBone.localPosition = targetR;

        if (!opening && doorBlockerCollider != null)
            doorBlockerCollider.enabled = true;

        currentRoutine = null;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player") || other.GetComponentInParent<Player>() != null)
        {
            triggerOccupants++;
            OpenDoor();
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player") || other.GetComponentInParent<Player>() != null)
        {
            triggerOccupants = Mathf.Max(0, triggerOccupants - 1);
            if (autoClose && triggerOccupants == 0)
            {
                StartCoroutine(AutoCloseDelayRoutine());
            }
        }
    }

    private IEnumerator AutoCloseDelayRoutine()
    {
        yield return new WaitForSeconds(autoCloseDelay);
        if (triggerOccupants == 0 && IsOpen)
        {
            CloseDoor();
        }
    }
}
