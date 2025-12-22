using UnityEngine;

public class InstructionsUI : MonoBehaviour
{
    [SerializeField] private GameObject instructionsPanel;

    private void Awake()
    {
        if (instructionsPanel != null)
            instructionsPanel.SetActive(false);
    }

    public void OpenInstructions()
    {
        if (instructionsPanel != null)
            instructionsPanel.SetActive(true);
    }

    public void CloseInstructions()
    {
        if (instructionsPanel != null)
            instructionsPanel.SetActive(false);
    }
}
