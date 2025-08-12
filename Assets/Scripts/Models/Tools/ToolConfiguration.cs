using System;
using System.Collections.Generic;
using ChatSystem.Configuration.ScriptableObjects;
using ChatSystem.Enums;

namespace ChatSystem.Models.Tools
{
    [Serializable]
    public class ToolConfiguration
    {
        public string toolId;
        public string toolName;
        public ToolType toolType;
        public ToolSchema inputSchema;
        public ToolAnnotations annotations;
        public bool enabled;
        public bool requiresAuthentication;
        public int timeoutMs;
        public int maxRetries;
        public bool hasRateLimit;
        public int requestsPerMinute;
        
        public ToolConfiguration()
        {
            enabled = true;
            timeoutMs = 5000;
            maxRetries = 3;
            requestsPerMinute = 60;
        }
        
        public ToolConfiguration(ToolConfig config)
        {
            if (config == null) return;
            
            toolId = config.toolId;
            toolName = config.toolName;
            toolType = config.toolType;
            annotations = config.annotations;
            enabled = config.enabled;
            requiresAuthentication = config.requiresAuthentication;
            timeoutMs = config.timeoutMs;
            maxRetries = config.maxRetries;
            hasRateLimit = config.hasRateLimit;
            requestsPerMinute = config.requestsPerMinute;
            
            if (config.function != null && config.function.parameters != null)
            {
                inputSchema = new ToolSchema
                {
                    type = config.function.parameters.type,
                    properties = ConvertToParameterSchemas(config.function.parameters.properties),
                    required = config.function.parameters.required
                };
            }
        }
        
        private Dictionary<string, ParameterSchema> ConvertToParameterSchemas(
            Dictionary<string, MCP.PropertyDefinition> properties)
        {
            if (properties == null) return null;
            
            Dictionary<string, ParameterSchema> result = new Dictionary<string, ParameterSchema>();
            
            foreach (var kvp in properties)
            {
                result[kvp.Key] = new ParameterSchema
                {
                    type = kvp.Value.type,
                    description = kvp.Value.description,
                    enumValues = kvp.Value.enumValues,
                    defaultValue = kvp.Value.defaultValue
                };
            }
            
            return result;
        }
    }
}