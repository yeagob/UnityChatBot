using UnityEngine;
using TMPro;
using ChatSystem.Models.Context;
using ChatSystem.Enums;

namespace ChatSystem.Views.Chat
{
    public class MessageView : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI messageText;
        [SerializeField] private RectTransform messageContainer;
        [SerializeField] private GameObject userMessagePrefab;
        [SerializeField] private GameObject assistantMessagePrefab;
        [SerializeField] private GameObject toolMessagePrefab;

        private Message currentMessage;

        public void Initialize(Message message)
        {
            currentMessage = message;
            SetupMessage();
        }

        private void SetupMessage()
        {
            ConfigureMessageAppearance();
            SetMessageContent();
            ConfigureAlignment();
        }

        private void ConfigureMessageAppearance()
        {
            switch (currentMessage.role)
            {
                case MessageRole.User:
                    ConfigureUserMessage();
                    break;
                case MessageRole.Assistant:
                    ConfigureAssistantMessage();
                    break;
                case MessageRole.Tool:
                    ConfigureToolMessage();
                    break;
                case MessageRole.System:
                    ConfigureSystemMessage();
                    break;
            }
        }

        private void ConfigureUserMessage()
        {
            messageText.color = Color.white;
            messageText.alignment = TextAlignmentOptions.Left;
            messageContainer.anchorMin = new Vector2(0f, 0f);
            messageContainer.anchorMax = new Vector2(0.8f, 1f);
        }

        private void ConfigureAssistantMessage()
        {
            messageText.color = Color.cyan;
            messageText.alignment = TextAlignmentOptions.Right;
            messageContainer.anchorMin = new Vector2(0.2f, 0f);
            messageContainer.anchorMax = new Vector2(1f, 1f);
        }

        private void ConfigureToolMessage()
        {
            messageText.color = Color.yellow;
            messageText.alignment = TextAlignmentOptions.Center;
            messageContainer.anchorMin = new Vector2(0.1f, 0f);
            messageContainer.anchorMax = new Vector2(0.9f, 1f);
        }

        private void ConfigureSystemMessage()
        {
            messageText.color = Color.gray;
            messageText.alignment = TextAlignmentOptions.Center;
            messageContainer.anchorMin = new Vector2(0f, 0f);
            messageContainer.anchorMax = new Vector2(1f, 1f);
        }

        private void SetMessageContent()
        {
            string formattedContent = FormatMessageContent();
            messageText.text = formattedContent;
        }

        private string FormatMessageContent()
        {
            string prefix = GetRolePrefix();
            string timestamp = currentMessage.timestamp.ToString("HH:mm");
            return $"<size=80%><color=gray>[{timestamp}]</color></size> {prefix}{currentMessage.content}";
        }

        private string GetRolePrefix()
        {
            switch (currentMessage.role)
            {
                case MessageRole.User:
                    return "<b>You:</b> ";
                case MessageRole.Assistant:
                    return "<b>Assistant:</b> ";
                case MessageRole.Tool:
                    return "<i><color=orange>[Tool Response]</color></i> ";
                case MessageRole.System:
                    return "<i><color=gray>[System]</color></i> ";
                default:
                    return "";
            }
        }

        private void ConfigureAlignment()
        {
            if (messageContainer != null)
            {
                messageContainer.sizeDelta = new Vector2(messageContainer.sizeDelta.x, GetPreferredHeight());
            }
        }

        private float GetPreferredHeight()
        {
            if (messageText != null)
            {
                return messageText.preferredHeight + 20f;
            }
            return 50f;
        }
    }
}
