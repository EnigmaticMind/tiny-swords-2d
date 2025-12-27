using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

[ExecuteInEditMode]
public class LaneAutoPositioner : MonoBehaviour
{
  [Header("Fixed Position Slots")]
  [Tooltip("Y position for top slot (when 2 or 3 enemies)")]
  [SerializeField] private float topY = 1.5f;
  [Tooltip("Y position for middle slot (when 3 enemies)")]
  [SerializeField] private float middleY = 0f;
  [Tooltip("Y position for bottom slot (always used)")]
  [SerializeField] private float bottomY = -1.5f;
  
  [Header("Depth Staggering (X-axis)")]
  [Tooltip("X offset for middle enemy when there are 3 enemies (positive = right/back, negative = left)")]
  [SerializeField] private float middleXOffset = 0.3f;
  
  [Header("Settings")]
  [Tooltip("Offset from lane center")]
  [SerializeField] private Vector3 laneOffset = Vector3.zero;
  [Tooltip("Auto-update positions when children change")]
  [SerializeField] private bool autoUpdate = true;

  private int lastChildCount = -1;

  private void OnValidate()
  {
    if (autoUpdate)
    {
      RepositionChildren();
    }
  }

  private void Start()
  {
    // Reposition on start (both edit mode and play mode)
    if (autoUpdate)
    {
      RepositionChildren();
    }
  }

#if UNITY_EDITOR
  private void Update()
  {
    // In edit mode, check for child count changes
    if (!Application.isPlaying && autoUpdate)
    {
      if (transform.childCount != lastChildCount)
      {
        lastChildCount = transform.childCount;
        RepositionChildren();
      }
    }
  }
#endif

  /// <summary>
  /// Repositions all children using fixed positions.
  /// 1 enemy: bottom slot
  /// 2 enemies: top and bottom slots
  /// 3 enemies: top, middle (with X offset), and bottom slots
  /// </summary>
  public void RepositionChildren()
  {
    int childCount = transform.childCount;
    if (childCount == 0)
    {
      lastChildCount = 0;
      return;
    }

    // Define fixed positions for the 3 slots
    Vector3[] slotPositions = new Vector3[3];
    slotPositions[0] = new Vector3(0f, topY, 0f);      // Top left
    slotPositions[1] = new Vector3(middleXOffset, middleY, 0f); // Middle back (with X offset)
    slotPositions[2] = new Vector3(0f, bottomY, 0f);  // Bottom left

    // Position enemies based on count
    if (childCount == 1)
    {
      // 1 enemy: bottom slot only
      Transform child = transform.GetChild(0);
      if (child != null)
      {
        child.localPosition = slotPositions[2] + laneOffset;
      }
    }
    else if (childCount == 2)
    {
      // 2 enemies: top and bottom slots
      Transform child0 = transform.GetChild(0);
      Transform child1 = transform.GetChild(1);
      
      if (child0 != null)
      {
        child0.localPosition = slotPositions[0] + laneOffset; // Top
      }
      if (child1 != null)
      {
        child1.localPosition = slotPositions[2] + laneOffset; // Bottom
      }
    }
    else if (childCount >= 3)
    {
      // 3+ enemies: top, middle, bottom (use first 3)
      for (int i = 0; i < Mathf.Min(3, childCount); i++)
      {
        Transform child = transform.GetChild(i);
        if (child != null)
        {
          child.localPosition = slotPositions[i] + laneOffset;
        }
      }
    }

    lastChildCount = childCount;
  }

#if UNITY_EDITOR
    private void OnTransformChildrenChanged()
    {
        if (autoUpdate && !Application.isPlaying)
        {
            EditorApplication.delayCall += RepositionChildren;
        }
    }
#endif
}
