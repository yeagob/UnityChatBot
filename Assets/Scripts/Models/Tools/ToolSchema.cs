using System;
using System.Collections.Generic;
using ChatSystem.Models.Tools;

[Serializable]
public class ToolSchema
{
    public string type;
    public Dictionary<string, ParameterSchema> properties;
    public List<string> required;
}
