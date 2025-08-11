using UnityEngine;

[CreateAssetMenu(fileName = "PromptConfig", menuName = "LLM/Prompt Configuration")]
public class PromptConfig : ScriptableObject
{
    [SerializeField] private string promptId;
    [SerializeField, TextArea(5, 15)] private string promptText;
    [SerializeField, Range(0f, 2f)] private float temperature;
    [SerializeField] private int maxTokens;
    [SerializeField, Range(0f, 1f)] private float topP;
    [SerializeField, Range(0f, 1f)] private float topK;

    public string PromptId => promptId;
    public string PromptText => promptText;
    public float Temperature => temperature;
    public int MaxTokens => maxTokens;
    public float TopP => topP;
    public float TopK => topK;
}
