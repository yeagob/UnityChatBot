using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "AgentConfig", menuName = "LLM/Agent Configuration")]
public class AgentConfig : ScriptableObject
{
    [SerializeField] private string agentId;
    [SerializeField] private string agentName;
    [SerializeField] private string token;
    [SerializeField] private string model;
    [SerializeField] private string serviceProvider;
    [SerializeField] private string serviceUrl;
    [SerializeField] private decimal inputTokenCost;
    [SerializeField] private decimal outputTokenCost;
    [SerializeField] private List<ToolConfig> toolConfigs;

    public string AgentId => agentId;
    public string AgentName => agentName;
    public string Token => token;
    public string Model => model;
    public string ServiceProvider => serviceProvider;
    public string ServiceUrl => serviceUrl;
    public decimal InputTokenCost => inputTokenCost;
    public decimal OutputTokenCost => outputTokenCost;
    public List<ToolConfig> ToolConfigs => toolConfigs;
}
