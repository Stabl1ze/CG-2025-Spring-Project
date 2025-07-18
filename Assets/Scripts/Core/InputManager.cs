using System.Collections.Generic;
using System.Linq;
using Unity.VisualScripting.Antlr3.Runtime.Tree;
using UnityEngine;
using UnityEngine.EventSystems;

public class InputManager : MonoBehaviour
{
    [Header("Selection Settings")]
    [SerializeField] private string selectableTag = "Selectable";
    [SerializeField] private float doubleClickTime = 0.3f;

    [Header("Box Selection Settings")]
    [SerializeField] private Texture boxSelectionTexture;
    [SerializeField] private Color boxSelectionColor = new(0.8f, 0.8f, 0.95f, 0.25f);
    [SerializeField] private Color boxSelectionBorderColor = new(0.8f, 0.8f, 0.95f);

    private Camera mainCamera;
    private float lastClickTime;
    private Vector3 boxSelectionStart;
    private bool isBoxSelecting;
    public static bool IsInBuildMode { get; set; }

    private void Awake()
    {
        mainCamera = Camera.main;

        if (EventSystem.current == null)
        {
            var eventSystem = new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
            Debug.Log("Auto Create EventSystem");
        }
    }

    private void Update()
    {
        if (GameManager.Instance.CurrentGameState != GameManager.GameState.Playing)
            return;

        if (IsInBuildMode) return;

        if (EventSystem.current.IsPointerOverGameObject())
            return;

        HandleSelectionInput();
        HandleCommandInput();
        HandleF2Input();
        HandleAttackInput();
        HandleBoxSelectionInput();
        HandleMarkTreeInput();
        HandleDeleteTreeInput();
        HandelEscInput();
    }

    private void OnGUI()
    {
        DrawBoxSelection();
    }

    private void HandleSelectionInput()
    {
        if (Input.GetMouseButtonDown(0))
        {
            // Cancel previous selection
            SelectionManager.Instance.DeselectAll();

            float timeSinceLastClick = Time.time - lastClickTime;
            lastClickTime = Time.time;

            Ray ray = mainCamera.ScreenPointToRay(Input.mousePosition);
            if (Physics.Raycast(ray, out RaycastHit hit, Mathf.Infinity))
            {
                if (hit.collider.CompareTag(selectableTag))
                {
                    ISelectable selectable = hit.collider.GetComponentInParent<ISelectable>();
                    if (selectable != null)
                    {
                        if (timeSinceLastClick <= doubleClickTime)
                        {
                            var unit = hit.collider.GetComponentInParent<UnitBase>();
                            if (unit != null)
                                SelectionManager.Instance.TypeSelect(unit);
                        }
                        else
                        {
                            selectable.OnSelect();
                            SelectionManager.Instance.AddToSelection(selectable);
                        }
                    }
                }
            }
        }
    }

    private void HandleBoxSelectionInput()
    {
        // Start box selection (Left Shift + Left Mouse)
        if (Input.GetMouseButtonDown(0) && Input.GetKey(KeyCode.LeftShift))
        {
            SelectionManager.Instance.DeselectAll();
            isBoxSelecting = true;
            boxSelectionStart = Input.mousePosition;
        }

        // End box selection
        if (Input.GetMouseButtonUp(0) && isBoxSelecting)
        {
            isBoxSelecting = false;
            SelectObjectsInBox(boxSelectionStart, Input.mousePosition);
        }

        // Cancel box selection
        if (Input.GetMouseButtonUp(1) && isBoxSelecting)
        {
            isBoxSelecting = false;
        }
    }

    private void SelectObjectsInBox(Vector3 start, Vector3 end)
    {
        // Convert screen positions to world space at ground level (y=0)
        Ray startRay = mainCamera.ScreenPointToRay(start);
        Ray endRay = mainCamera.ScreenPointToRay(end);

        Plane groundPlane = new(Vector3.up, Vector3.zero);
        groundPlane.Raycast(startRay, out float startDist);
        groundPlane.Raycast(endRay, out float endDist);

        Vector3 startWorldPos = startRay.GetPoint(startDist);
        Vector3 endWorldPos = endRay.GetPoint(endDist);

        // Create world space bounds
        Bounds selectionBounds = new();
        selectionBounds.SetMinMax(
            new Vector3(
                Mathf.Min(startWorldPos.x, endWorldPos.x),
                0,
                Mathf.Min(startWorldPos.z, endWorldPos.z)
            ),
            new Vector3(
                Mathf.Max(startWorldPos.x, endWorldPos.x),
                0,
                Mathf.Max(startWorldPos.z, endWorldPos.z)
            )
        );

        // Find all selectable objects
        HashSet<ISelectable> selectablesInBox = new HashSet<ISelectable>();
        var allSelectables = FindObjectsOfType<MonoBehaviour>().OfType<ISelectable>();

        foreach (ISelectable selectable in allSelectables)
        {
            if (selectable is MonoBehaviour)
            {
                Vector2 xzPos = selectable.GetXZ();

                // Check if XZ position is within selection bounds
                if (selectionBounds.Contains(new Vector3(xzPos.x, 0, xzPos.y)))
                {
                    selectablesInBox.Add(selectable);
                }
            }
        }

        SelectionManager.Instance.BoxSelect(selectablesInBox);
    }

    #region Box Selection Utility Methods
    private Rect GetScreenRect(Vector3 screenPosition1, Vector3 screenPosition2)
    {
        // Convert from screen coordinates to GUI coordinates
        screenPosition1.y = Screen.height - screenPosition1.y;
        screenPosition2.y = Screen.height - screenPosition2.y;

        Vector3 topLeft = Vector3.Min(screenPosition1, screenPosition2);
        Vector3 bottomRight = Vector3.Max(screenPosition1, screenPosition2);

        return Rect.MinMaxRect(topLeft.x, topLeft.y, bottomRight.x, bottomRight.y);
    }

    private void DrawBoxSelection()
    {
        if (isBoxSelecting)
        {
            Rect rect = GetScreenRect(boxSelectionStart, Input.mousePosition);

            // Draw selection box background
            if (boxSelectionTexture != null)
            {
                GUI.DrawTexture(rect, boxSelectionTexture);
            }
            else
            {
                GUI.color = boxSelectionColor;
                GUI.DrawTexture(rect, Texture2D.whiteTexture);
            }

            // Draw selection box border
            GUI.color = boxSelectionBorderColor;
            GUI.DrawTexture(new Rect(rect.x, rect.y, rect.width, 1), Texture2D.whiteTexture); // Top
            GUI.DrawTexture(new Rect(rect.x, rect.y, 1, rect.height), Texture2D.whiteTexture); // Left
            GUI.DrawTexture(new Rect(rect.x + rect.width, rect.y, 1, rect.height), Texture2D.whiteTexture); // Right
            GUI.DrawTexture(new Rect(rect.x, rect.y + rect.height, rect.width, 1), Texture2D.whiteTexture); // Bottom

            GUI.color = Color.white;
        }
    }

    private Bounds GetViewportBounds(Camera camera, Vector3 screenPos1, Vector3 screenPos2)
    {
        Vector3 v1 = camera.ScreenToViewportPoint(screenPos1);
        Vector3 v2 = camera.ScreenToViewportPoint(screenPos2);
        Vector3 min = Vector3.Min(v1, v2);
        Vector3 max = Vector3.Max(v1, v2);
        min.z = camera.nearClipPlane;
        max.z = camera.farClipPlane;

        Bounds bounds = new Bounds();
        bounds.SetMinMax(min, max);
        return bounds;
    }
    #endregion

    private void HandleCommandInput()
    {
        if (Input.GetMouseButtonDown(1))
        {
            if (SelectionManager.Instance.HasSelection())
            {
                Ray ray = mainCamera.ScreenPointToRay(Input.mousePosition);
                if (Physics.Raycast(ray, out RaycastHit hit, Mathf.Infinity))
                {
                    SelectionManager.Instance.IssueCommand(hit.point, hit.collider.gameObject);
                }
            }
            return;
        }
    }

    private void HandleF2Input()
    {
        if (Input.GetKeyDown(KeyCode.F2))
        {
            SelectionManager.Instance.DeselectAll();
            var fightingUnits = UnitManager.Instance.GetFightingUnits();
            foreach (var unit in fightingUnits)
            {
                if (unit is ISelectable selectable)
                {
                    selectable.OnSelect();
                    SelectionManager.Instance.AddToSelection(selectable);
                }
            }
        }
    }

    private void HandleAttackInput()
    {
        if (Input.GetKeyDown(KeyCode.Z))
        {
            if (SelectionManager.Instance.HasSelection())
            {
                Ray ray = mainCamera.ScreenPointToRay(Input.mousePosition);
                if (Physics.Raycast(ray, out RaycastHit hit, Mathf.Infinity))
                {
                    // Use null for move-attack
                    SelectionManager.Instance.IssueCommand(hit.point, null);
                }
            }
        }
    }

    private void HandleMarkTreeInput()
    {
        if (Input.GetKey(KeyCode.F) || Input.GetKey(KeyCode.G))
        {
            bool mark = Input.GetKey(KeyCode.F); 
            Ray ray = mainCamera.ScreenPointToRay(Input.mousePosition);
            if (Physics.Raycast(ray, out RaycastHit hit, Mathf.Infinity))
            {
                if (hit.collider.CompareTag(selectableTag))
                {
                    var treeNode = hit.collider.GetComponentInParent<TreeNode>();
                    if (treeNode != null) 
                    {
                        TreeManager.Instance.ToggleMarkTree(treeNode, treeNode.transform.position, mark);
                        return;
                    }
                }
            }
        }
    }

    // Test method
    private void HandleDeleteTreeInput()
    {
        if (Input.GetKey(KeyCode.H))
        {
            Ray ray = mainCamera.ScreenPointToRay(Input.mousePosition);
            if (Physics.Raycast(ray, out RaycastHit hit, Mathf.Infinity))
            {
                if (hit.collider.CompareTag(selectableTag))
                {
                    var tree = hit.collider.GetComponentInParent<TreeNode>();
                    if (tree != null)
                    {
                        Destroy(tree.gameObject);
                    }
                    var unit = hit.collider.GetComponentInParent<UnitBase>();
                    if (unit != null)
                    {
                        Destroy(unit.gameObject);
                    }
                }
            }
        }
    }

    private void HandelEscInput()
    {
        if(Input.GetKeyDown(KeyCode.Escape))
        {
            if (!UIManager.Instance.escUI.IsActive)
                UIManager.Instance.ShowEscPanel();
            else
                UIManager.Instance.HideEscPanel();
        }
    }
}