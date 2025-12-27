using UnityEngine;
using UnityEngine.EventSystems;

namespace TinySwords2D.UI
{
  public class BackdropClickHandler : MonoBehaviour, IPointerClickHandler
  {
    private PauseMenuController pauseController;
    private ConfirmationModal modal;

    public void Initialize(PauseMenuController controller)
    {
      pauseController = controller;
    }

    public void Initialize(ConfirmationModal modal)
    {
      this.modal = modal;
    }

    public void OnPointerClick(PointerEventData eventData)
    {
      // Check if the click actually hit this backdrop object, not a child panel
      // This prevents clicks on the panel from closing the menu
      if (eventData.pointerCurrentRaycast.gameObject != gameObject)
      {
        // Click was on a child object (like the panel), ignore it
        // Mark the event as used to prevent further processing
        eventData.Use();
        return;
      }

      if (modal != null)
      {
        modal.Hide();
      }
      else if (pauseController != null)
      {
        pauseController.OnBackdropClicked();
      }
    }
  }
}
