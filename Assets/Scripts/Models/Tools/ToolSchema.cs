using System;
using System.Collections.Generic;

[Serializable]
public class ToolSchema
{
    public string type;
    public Dictionary<string, ParameterSchema> properties;
    public List<string> required;
}
