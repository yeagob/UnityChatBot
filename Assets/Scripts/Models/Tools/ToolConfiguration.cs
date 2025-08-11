using System;
using ChatSystem.Enums;

namespace ChatSystem.Models.Tools
{
    [Serializable]
    public class ToolConfiguration
    {
        public string toolId;
        public string toolName;
        public string description;
        public ToolType toolType;
        public ToolSchema inputSchema;
        public ToolAnnotations annotations;
        
        public ToolConfiguration()
        {
        }
        
        public ToolConfiguration(string id, string name, string desc, ToolType type)
        {
            toolId = id;
            toolName = name;
            description = desc;
            toolType = type;
        }
        
        public static ToolConfiguration FromScriptableObject(ToolConfig config)
        {
            return new ToolConfiguration
            {
                toolId = config.ToolId,
                toolName = config.ToolName,
                description = config.Description,
                toolType = config.ToolType,
                inputSchema = config.InputSchema,
                annotations = config.Annotations
            };
        }
    }
}
