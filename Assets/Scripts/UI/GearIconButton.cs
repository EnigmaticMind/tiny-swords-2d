using UnityEngine;
using UnityEngine.EventSystems;

namespace TinySwords2D.UI
{
  /// <summary>
  /// Button that opens the pause menu. Can be placed anywhere.
  /// </summary>
  public class GearIconButton : MonoBehaviour, IPointerClickHandler
  {
    public void OnPointerClick(PointerEventData eventData)
    {
      if (MenuManager.Instance != null)
      {
        MenuManager.Instance.TogglePauseMenu();
      }
      else
      {
        Debug.LogWarning("GearIconButton: MenuManager.Instance is null! Make sure MenuManager exists in the scene.");
      }
    }
  }
}
