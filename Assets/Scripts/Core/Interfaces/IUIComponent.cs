public interface IUIComponent
{
    void Initialize();
    void Show();
    void Hide();
    void UpdateDisplay();
    bool IsActive { get; }
}