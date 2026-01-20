using TMPro;
using UnityEngine;

public class PlayerInteract : MonoBehaviour
{
    public Camera playerCamera;
    public float interactRange = 3f;
    public KeyCode interactKey = KeyCode.E;
    public GameObject interactionUI;
    public TextMeshProUGUI interactText;

    private IInteractable currentTarget;
        
    void FixedUpdate()
    {
        DetectInteractable();

        if (currentTarget != null && Input.GetKeyDown(interactKey))
        {
            currentTarget.Interact();
        }
    }

    void DetectInteractable()
    {
        Ray ray = new Ray(playerCamera.transform.position, playerCamera.transform.forward);
        if (Physics.Raycast(ray, out RaycastHit hit, interactRange))
        {
            currentTarget = hit.collider.GetComponent<IInteractable>();

            if (currentTarget != null)
            {
                interactionUI.SetActive(true);
                if (interactText != null)
                    interactText.text = currentTarget.GetPrompt();
                return;
            }
        }

        currentTarget = null;
        interactionUI.SetActive(false);
        if (interactText != null)
            interactText.text = "";
    }
}
