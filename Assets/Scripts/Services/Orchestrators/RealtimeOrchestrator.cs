        private List<ToolConfiguration> GetCurrentAgentToolConfigurations()
        {
            List<ToolConfiguration> toolConfigurations = new List<ToolConfiguration>();
            
            if (currentAgentConfig == null || currentAgentConfig.availableTools == null)
            {
                LoggingService.LogWarning("No agent config or tools available");
                return toolConfigurations;
            }

            foreach (var toolConfig in currentAgentConfig.availableTools)
            {
                if (toolConfig != null && toolConfig.enabled)
                {
                    ToolConfiguration toolConfiguration = new ToolConfiguration(toolConfig);
                    toolConfigurations.Add(toolConfiguration);
                }
            }
            
            LoggingService.LogInfo($"Generated {toolConfigurations.Count} tool configurations from agent");
            return toolConfigurations;
        }