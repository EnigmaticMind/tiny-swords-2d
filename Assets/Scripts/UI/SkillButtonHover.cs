using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

[RequireComponent(typeof(Image))]
public class UIHoverEffect : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
  [SerializeField] private float hoverBrightness = 1.1f;
  private Image image;
  private Color originalColor;

  void Awake()
  {
    image = GetComponent<Image>();
    originalColor = image.color;
    Debug.Log($"UIHoverEffect: Initialized on {gameObject.name} (brightness {hoverBrightness})");
  }

  public void OnPointerEnter(PointerEventData eventData)
  {
    if (image == null)
    {
      Debug.LogWarning($"UIHoverEffect: Image missing on {gameObject.name}");
      return;
    }

    Color bright = originalColor * hoverBrightness;
    bright.a = originalColor.a;
    image.color = bright;

    Debug.Log($"UIHoverEffect: Hover enter on {gameObject.name}. Applied brightness {hoverBrightness}");
  }

  public void OnPointerExit(PointerEventData eventData)
  {
    if (image == null)
      return;

    image.color = originalColor;
    Debug.Log($"UIHoverEffect: Hover exit on {gameObject.name}. Restored original color.");
  }
}