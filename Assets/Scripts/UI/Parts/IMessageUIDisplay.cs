
using TMPro;

public interface IMessageUIDisplay
{
    string messageText { get; set; }
    void Init(string text);
    void UpdateTextDisplay();
}