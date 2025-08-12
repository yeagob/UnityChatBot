using System;
using System.Collections.Generic;
using UnityEngine;

namespace ChatSystem.Models.Tools.MCP
{
    [Serializable]
    public class ParameterDefinition
    {
        public string type = "object";
        public Dictionary<string, PropertyDefinition> properties;
        public List<string> required;
        
        public ParameterDefinition()
        {
            properties = new Dictionary<string, PropertyDefinition>();
            required = new List<string>();
        }
    }
}