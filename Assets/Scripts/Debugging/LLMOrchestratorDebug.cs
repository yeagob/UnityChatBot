using UnityEngine;
using ChatSystem.Services.Orchestrators.Interfaces;
using System.Collections.Generic;

namespace ChatSystem.Debugging
{
    public class LLMOrchestratorDebug : MonoBehaviour
    {
        private ILLMOrchestrator orchestrator;
        
        public void SetOrchestrator(ILLMOrchestrator orchestrator)
        {
            this.orchestrator = orchestrator;
        }
        
        [ContextMenu("Show Active Agents")]
        public void ShowActiveAgents()
        {
            if (orchestrator == null)
            {
                Debug.LogWarning("LLMOrchestrator not set");
                return;
            }
            
            List<string> activeAgents = orchestrator.GetActiveAgents();
            
            if (activeAgents.Count == 0)
            {
                Debug.Log("No active agents");
            }
            else
            {
                Debug.Log($"Active agents ({activeAgents.Count}):");
                foreach (string agentId in activeAgents)
                {
                    Debug.Log($"  - {agentId}");
                }
            }
        }
        
        [ContextMenu("Enable All Agents")]
        public void EnableAllAgents()
        {
            if (orchestrator == null)
            {
                Debug.LogWarning("LLMOrchestrator not set");
                return;
            }
            
            Debug.Log("Enabling all agents");
        }
        
        [ContextMenu("Test Process Message")]
        public async void TestProcessMessage()
        {
            if (orchestrator == null)
            {
                Debug.LogWarning("LLMOrchestrator not set");
                return;
            }
            
            Debug.Log("[DEBUG] Testing LLM message processing");
            
            ChatSystem.Models.Context.ConversationContext context = 
                new ChatSystem.Models.Context.ConversationContext("debug-test");
            context.AddUserMessage("Test message from debug");
            
            var response = await orchestrator.ProcessMessageAsync(context);
            Debug.Log($"[DEBUG] Response: {response.content}");
        }
    }
}