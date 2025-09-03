using System;
using System.Threading.Tasks;
using ChatSystem.Models.Audio;

namespace ChatSystem.Controllers.Voice.Interfaces
{
    public interface IVoiceController
    {
        event Action<string> OnTranscriptionReceived;
        event Action<string> OnResponseReceived;
        event Action<string> OnErrorOccurred;
        
        Task StartVoiceSessionAsync(string conversationId, string agentId);
        Task ProcessVoiceInputAsync(AudioData audioData);
        Task StopVoiceSessionAsync();
        Task SetActiveAgentAsync(string agentId);
        
        bool IsSessionActive { get; }
        string CurrentConversationId { get; }
        string CurrentAgentId { get; }
    }
}