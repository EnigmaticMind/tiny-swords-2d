using UnityEngine;
using TMPro;
using UnityEngine.UI;
using TinySwords2D.Gameplay;

namespace TinySwords2D.UI
{
  /// <summary>
  /// Displays incoming damage indicator above a character
  /// </summary>
  public class DamageIndicator : MonoBehaviour
  {
    [Header("UI References")]
    [SerializeField] private GameObject indicatorPanel;
    [SerializeField] private TextMeshProUGUI damageText;
    [SerializeField] private Image damageIcon;

    [Header("Settings")]
    [SerializeField] private float offsetY = 50f; // Height above character
    [SerializeField] private bool showOnlyWhenDamage = true;

    private CharacterInstance characterInstance;
    private int totalIncomingDamage = 0;

    private void Awake()
    {
      characterInstance = GetComponentInParent<CharacterInstance>();

      // Auto-find components if not assigned
      if (indicatorPanel == null)
      {
        indicatorPanel = transform.Find("DamageIndicator")?.gameObject;
      }

      if (damageText == null && indicatorPanel != null)
      {
        damageText = indicatorPanel.GetComponentInChildren<TextMeshProUGUI>();
      }

      // Hide by default
      if (indicatorPanel != null)
      {
        indicatorPanel.SetActive(false);
      }
    }

    /// <summary>
    /// Updates the incoming damage value
    /// </summary>
    public void SetIncomingDamage(int damage)
    {
      totalIncomingDamage = damage;
      UpdateDisplay();
    }

    /// <summary>
    /// Adds to incoming damage (for multiple enemies targeting same player)
    /// </summary>
    public void AddIncomingDamage(int damage)
    {
      totalIncomingDamage += damage;
      UpdateDisplay();
    }

    /// <summary>
    /// Clears incoming damage (call at start of player turn)
    /// </summary>
    public void ClearIncomingDamage()
    {
      totalIncomingDamage = 0;
      UpdateDisplay();
    }

    private void UpdateDisplay()
    {
      if (indicatorPanel == null) return;

      bool shouldShow = totalIncomingDamage > 0 || !showOnlyWhenDamage;
      indicatorPanel.SetActive(shouldShow);

      if (damageText != null && totalIncomingDamage > 0)
      {
        damageText.text = totalIncomingDamage.ToString();
      }
    }

    /// <summary>
    /// Positions the indicator above the character
    /// </summary>
    private void Update()
    {
      if (indicatorPanel != null && characterInstance != null)
      {
        // Position above character in world space
        Vector3 worldPos = characterInstance.transform.position;
        worldPos.y += offsetY;

        // Convert to screen space and position UI
        Camera mainCam = Camera.main;
        if (mainCam != null)
        {
          Vector3 screenPos = mainCam.WorldToScreenPoint(worldPos);
          RectTransform rectTransform = indicatorPanel.GetComponent<RectTransform>();
          if (rectTransform != null)
          {
            // Convert screen position to canvas position
            Canvas canvas = GetComponentInParent<Canvas>();
            if (canvas != null)
            {
              RectTransformUtility.ScreenPointToLocalPointInRectangle(
                canvas.transform as RectTransform,
                screenPos,
                canvas.worldCamera,
                out Vector2 localPoint
              );
              rectTransform.anchoredPosition = localPoint;
            }
          }
        }
      }
    }
  }
}
