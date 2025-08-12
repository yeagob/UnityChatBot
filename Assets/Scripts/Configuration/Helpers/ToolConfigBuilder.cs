using System.Collections.Generic;
using ChatSystem.Models.Tools.MCP;

namespace ChatSystem.Configuration.Helpers
{
    public static class ToolConfigBuilder
    {
        public static PropertyDefinition CreateStringProperty(string description, List<string> enumValues = null)
        {
            return new PropertyDefinition
            {
                type = "string",
                description = description,
                enumValues = enumValues ?? new List<string>()
            };
        }
        
        public static PropertyDefinition CreateNumberProperty(string description)
        {
            return new PropertyDefinition
            {
                type = "number",
                description = description
            };
        }
        
        public static PropertyDefinition CreateArrayProperty(string itemType, string description)
        {
            return new PropertyDefinition
            {
                type = "array",
                description = description,
                items = new ItemDefinition
                {
                    type = itemType
                }
            };
        }
        
        public static FunctionDefinition CreateFunction(string name, string description, 
            Dictionary<string, PropertyDefinition> properties, List<string> required)
        {
            return new FunctionDefinition
            {
                name = name,
                description = description,
                parameters = new ParameterDefinition
                {
                    type = "object",
                    properties = properties,
                    required = required
                }
            };
        }
    }
}