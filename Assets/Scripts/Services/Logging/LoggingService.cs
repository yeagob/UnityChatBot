using UnityEngine;
using ChatSystem.Enums;
using ChatSystem.Services.Logging.Interfaces;
using System;
using System.Collections.Generic;

namespace ChatSystem.Services.Logging
{
    public static class LoggingService
    {
        private static LogLevel currentLogLevel = LogLevel.Info;
        private static bool isInitialized = false;
        
        public static void Initialize(LogLevel logLevel = LogLevel.Info)
        {
            currentLogLevel = logLevel;
            isInitialized = true;
            LogInfo("LoggingService initialized");
        }
        
        public static void LogDebug(string message)
        {
            if (IsLogLevelEnabled(LogLevel.Debug))
            {
                Debug.Log($"[DEBUG] {GetTimestamp()} {message}");
            }
        }
        
        public static void LogInfo(string message)
        {
            if (IsLogLevelEnabled(LogLevel.Info))
            {
                Debug.Log($"[INFO] {GetTimestamp()} {message}");
            }
        }
        
        public static void LogWarning(string message)
        {
            if (IsLogLevelEnabled(LogLevel.Warning))
            {
                Debug.LogWarning($"[WARNING] {GetTimestamp()} {message}");
            }
        }
        
        public static void Error(string message)
        {
            if (IsLogLevelEnabled(LogLevel.Error))
            {
                Debug.LogError($"[ERROR] {GetTimestamp()} {message}");
            }
        }
        
        public static void LogCritical(string message)
        {
            if (IsLogLevelEnabled(LogLevel.Critical))
            {
                Debug.LogError($"[CRITICAL] {GetTimestamp()} {message}");
            }
        }
        
        public static void LogAgentExecution(string agentId, string message)
        {
            LogInfo($"[AGENT:{agentId}] {message}");
        }
        
        public static void LogToolCall(string toolName, Dictionary<string, object> arguments)
        {
            string argumentsString = FormatArguments(arguments);
            LogInfo($"[TOOL_CALL:{toolName}] Arguments: {argumentsString}");
        }
        
        public static void LogToolResponse(string toolName, string response)
        {
            LogInfo($"[TOOL_RESPONSE:{toolName}] Response: {response}");
        }
        
        public static void LogMessageReceived(string conversationId, string role)
        {
            LogInfo($"[MESSAGE:{conversationId}] Role: {role}");
        }
        
        public static void LogPromptConstruction(string agentId, int tokenCount)
        {
            LogInfo($"[PROMPT:{agentId}] Token count: {tokenCount}");
        }
        
        public static void SetLogLevel(LogLevel level)
        {
            currentLogLevel = level;
            LogInfo($"Log level changed to: {level}");
        }
        
        public static bool IsLogLevelEnabled(LogLevel level)
        {
            if (!isInitialized) Initialize();
            return level >= currentLogLevel;
        }
        
        private static string GetTimestamp()
        {
            return DateTime.Now.ToString("HH:mm:ss.fff");
        }
        
        private static string FormatArguments(Dictionary<string, object> arguments)
        {
            if (arguments == null || arguments.Count == 0)
                return "(none)";
                
            List<string> pairs = new List<string>();
            foreach (KeyValuePair<string, object> kvp in arguments)
            {
                pairs.Add($"{kvp.Key}={kvp.Value}");
            }
            return string.Join(", ", pairs);
        }
    }
}
