using UnityEngine;

[CreateAssetMenu(fileName = "ToolConfig", menuName = "LLM/Tool Configuration")]
public class ToolConfig : ScriptableObject
{
    [SerializeField] private string toolId;
    [SerializeField] private string toolName;
    [SerializeField] private string description;
    [SerializeField] private ToolType toolType;
    [SerializeField] private ToolSchema inputSchema;
    [SerializeField] private ToolAnnotations annotations;

    public string ToolId => toolId;
    public string ToolName => toolName;
    public string Description => description;
    public ToolType ToolType => toolType;
    public ToolSchema InputSchema => inputSchema;
    public ToolAnnotations Annotations => annotations;
}
