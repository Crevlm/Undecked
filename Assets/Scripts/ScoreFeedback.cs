using TMPro;
using UnityEngine;

public class ScoreFeedback : MonoBehaviour
{
    [SerializeField] private float lifetime = 2f;

    private TextMeshProUGUI textMesh;
    private float timer;
    private Color originalColor;

    void Awake()
    {
        textMesh = GetComponent<TextMeshProUGUI>();
        if (textMesh != null)
        {
            originalColor = textMesh.color;
        }
    }

    void Update()
    {
        timer += Time.deltaTime;

        // Fade out
        if (textMesh != null)
        {
            float alpha = 1f - (timer / lifetime);
            textMesh.color = new Color(originalColor.r, originalColor.g, originalColor.b, alpha);
        }

        // Destroy
        if (timer >= lifetime)
        {
            Destroy(gameObject);
        }
    }

    public void SetText(string text)
    {
        if (textMesh != null)
        {
            textMesh.text = text;
        }
    }
}