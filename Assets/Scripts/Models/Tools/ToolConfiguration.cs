using System;
using System.Collections.Generic;
using System.Text;
using ChatSystem.Configuration.ScriptableObjects;
using ChatSystem.Enums;
using ChatSystem.Models.Tools.MCP;

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
        
        public string ToOpenAIFormat()
        {
            StringBuilder sb = new StringBuilder();
            sb.Append("{");
            sb.Append("\"type\":\"function\",");
            sb.Append("\"function\":{");
            sb.Append($"\"name\":\"{toolName}\",");
            
            string description = GetToolDescription();
            sb.Append($"\"description\":\"{EscapeJsonString(description)}\",");
            
            sb.Append("\"parameters\":{");
            sb.Append("\"type\":\"object\"");
            
            if (inputSchema?.properties != null && inputSchema.properties.Count > 0)
            {
                sb.Append(",\"properties\":{");
                bool first = true;
                foreach (var prop in inputSchema.properties)
                {
                    if (!first) sb.Append(",");
                    sb.Append($"\"{prop.Key}\":{{");
                    sb.Append($"\"type\":\"{prop.Value.type}\"");
                    if (!string.IsNullOrEmpty(prop.Value.description))
                    {
                        sb.Append($",\"description\":\"{EscapeJsonString(prop.Value.description)}\"");
                    }
                    if (prop.Value.enumValues != null && prop.Value.enumValues.Count > 0)
                    {
                        sb.Append(",\"enum\":[");
                        for (int i = 0; i < prop.Value.enumValues.Count; i++)
                        {
                            if (i > 0) sb.Append(",");
                            sb.Append($"\"{EscapeJsonString(prop.Value.enumValues[i])}\"");
                        }
                        sb.Append("]");
                    }
                    sb.Append("}");
                    first = false;
                }
                sb.Append("}");
            }
            else
            {
                sb.Append(",\"properties\":{}");
            }
            
            if (inputSchema?.required != null && inputSchema.required.Count > 0)
            {
                sb.Append(",\"required\":[");
                for (int i = 0; i < inputSchema.required.Count; i++)
                {
                    if (i > 0) sb.Append(",");
                    sb.Append($"\"{inputSchema.required[i]}\"");
                }
                sb.Append("]");
            }
            
            sb.Append("}");
            sb.Append("}");
            sb.Append("}");
            
            return sb.ToString();
        }
        
        public string ToQWENFormat()
        {
            return ToOpenAIFormat();
        }
        
        private string GetToolDescription()
        {
            if (annotations != null && !string.IsNullOrEmpty(annotations.title))
            {
                return annotations.title;
            }
            
            return $"{toolName} function";
        }
        
        private Dictionary<string, ParameterSchema> ConvertToParameterSchemas(
            List<SerializableProperty> properties)
        {
            if (properties == null) return new Dictionary<string, ParameterSchema>();
            
            Dictionary<string, ParameterSchema> result = new Dictionary<string, ParameterSchema>();
            
            foreach (SerializableProperty prop in properties)
            {
                if (string.IsNullOrEmpty(prop.key)) continue;
                
                result[prop.key] = new ParameterSchema
                {
                    type = prop.value.type,
                    description = prop.value.description,
                    enumValues = prop.value.enumValues,
                    defaultValue = prop.value.defaultValue
                };
            }
            
            return result;
        }
        
        private string EscapeJsonString(string input)
        {
            if (string.IsNullOrEmpty(input)) return string.Empty;
            
            return input
                .Replace("\\", "\\\\")
                .Replace("\"", "\\\"")
                .Replace("\n", "\\n")
                .Replace("\r", "\\r")
                .Replace("\t", "\\t");
        }
    }
}