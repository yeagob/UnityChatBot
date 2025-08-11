using System.Collections.Generic;
using System.Threading.Tasks;
using ChatSystem.Models.Tools;
using ChatSystem.Enums;

namespace ChatSystem.Services.Tools.Interfaces
{
    public interface IToolSet
    {
        string ToolSetId { get; }
        ToolType ToolSetType { get; }
        List<ToolConfiguration> GetAvailableTools();
        Task<ToolResponse> ExecuteToolAsync(ToolCall toolCall);
        Task<bool> ValidateToolCallAsync(ToolCall toolCall);
        bool IsToolSupported(string toolName);
    }
}
