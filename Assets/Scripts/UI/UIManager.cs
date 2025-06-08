using UnityEngine;
using System.Collections.Generic;

public class UIManager : MonoBehaviour
{
    public static UIManager Instance { get; private set; }

    [Header("UI Components")]
    [SerializeField] public ResourceUI resourceUI;
    [SerializeField] public UnitUI unitUI;
    [SerializeField] public BuildingUI buildingUI;
    [SerializeField] public ResourceNodeUI resourceNodeUI;
    [SerializeField] public ConstructionUI constructionUI;
    [SerializeField] public VictoryUI victoryUI;
    [SerializeField] public EscUI escUI;

    private readonly List<IUIComponent> allUIComponents = new();

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        InitializeUIComponents();
    }

    private void InitializeUIComponents()
    {
        allUIComponents.Clear();

        // Add all UI components
        if (resourceUI != null) allUIComponents.Add(resourceUI);
        if (unitUI != null) allUIComponents.Add(unitUI);
        if (buildingUI != null) allUIComponents.Add(buildingUI);
        if (resourceNodeUI != null) allUIComponents.Add(resourceNodeUI);
        if (escUI != null) allUIComponents.Add(escUI);
        if (victoryUI != null) allUIComponents.Add(victoryUI);


        // Initialize each component
        foreach (var component in allUIComponents)
        {
            component.Initialize();
        }
    }

    #region Public UI Interface Methods
    public void UpdateResourceDisplay(Dictionary<ResourceManager.ResourceType, ResourceManager.ResourceData> resources)
    {
        foreach (var resource in resources)
        {
            resourceUI?.UpdateResourceDisplay(resource.Key, resource.Value.amount, resource.Value.maxCapacity);
        }
    }

    public void ShowResourceNodePanel(ResourceNode resourceNode) => resourceNodeUI?.ShowResourceNodePanel(resourceNode);
    public void UpdateResourceNodeDisplay(ResourceNode resourceNode) => resourceNodeUI?.UpdateResourceNodeDisplay(resourceNode);
    public void HideResourceNodePanel() => resourceNodeUI?.Hide();

    public void ShowUnitPanel(UnitBase unit) => unitUI?.ShowUnitPanel(unit);
    public void UpdateUnitHP(UnitBase unit) => unitUI?.UpdateUnitHP(unit);
    public void HideUnitPanel() => unitUI?.Hide();

    public void ShowBuildingPanel(BuildingBase building) => buildingUI?.ShowBuildingPanel(building);
    public void UpdateBuildingHP(BuildingBase building) => buildingUI?.UpdateBuildingHP(building);
    public void HideBuildingPanel() => buildingUI?.Hide();

    public void ShowConstructionPanel() => constructionUI?.ShowConstructionPanel();
    public void HideConstructionPanel() => constructionUI?.Hide();

    public void ShowEscPanel() => escUI?.Show();
    public void HideEscPanel() => escUI?.Hide();

    public void ShowVictoryPanel() => victoryUI?.Show();
    public void HideVictoryPanel() => victoryUI?.Hide();
    public void SetCondItionActive(bool enemy, bool beacon) => victoryUI?.SetConditionActive(enemy, beacon);
    public void UpdateVictoryPanel(int enemy, bool beacon) => victoryUI?.UpdateVictoryDisplay(enemy, beacon);
    #endregion

    #region Minimap Implementation
    #endregion

    #region Extension Methods
    public void HideAllPanels()
    {
        foreach (var component in allUIComponents)
        {
            component.Hide();
        }
    }

    public T GetUIComponent<T>() where T : class, IUIComponent
    {
        foreach (var component in allUIComponents)
        {
            if (component is T typedComponent)
                return typedComponent;
        }
        return null;
    }
    #endregion

    #region Debug Mode Test
    private void OnGUI()
    {
        if (GameManager.Instance?.GetDebugStatus() != true) return;

        GUILayout.BeginArea(new Rect(Screen.width - 210, 10, 200, 200));
        GUILayout.Label("Debug Tools");

        if (GUILayout.Button("Add 100 LineR"))
        {
            ResourceManager.Instance?.AddResources(ResourceManager.ResourceType.LineR, 100);
        }

        if (GUILayout.Button("Remove 150 LineR"))
        {
            ResourceManager.Instance?.SpendResources(ResourceManager.ResourceType.LineR, 150);
        }

        if (GUILayout.Button("Add 100 CubeR"))
        {
            ResourceManager.Instance?.AddResources(ResourceManager.ResourceType.CubeR, 100);
        }

        if (GUILayout.Button("Test Win Condition 1"))
        {
            Beacon beacon = new();
            GameManager.Instance.CheckBeaconBuilt(beacon);
        }

        if (GUILayout.Button("Test Win Condition 2"))
        {
            GameManager.Instance.CheckEnemiesDefeated();
        }
        
        if (GUILayout.Button("Test Lost Condition"))
        {
            GameManager.Instance.CheckTimeLimit(6, 5);
        }
        GUILayout.EndArea();
    }
    #endregion
}