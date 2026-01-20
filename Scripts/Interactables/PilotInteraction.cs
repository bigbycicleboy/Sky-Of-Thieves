using UnityEngine;

public class PilotShipInteraction : MonoBehaviour, IInteractable
{
    public string promptMessage = "Pilot Ship";
    public ShipController ship;
    public PlayerController PlayerController;
    public GameObject Camera;

    public string GetPrompt()
    {
        return promptMessage;
    }

    public void Interact()
    {
        ship.enabled = true;
        Camera.GetComponent<CameraShipMove>().enabled = true;
        
        //PlayerController.movementEnabled = false;
        Camera.GetComponent<CameraController>().enabled = false;
    }
}
