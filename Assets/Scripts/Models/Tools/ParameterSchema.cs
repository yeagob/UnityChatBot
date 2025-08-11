using System;
using System.Collections.Generic;

[Serializable]
public class ParameterSchema
{
    public string type;
    public string description;
    public List<string> @enum;
    public object @default;
}
