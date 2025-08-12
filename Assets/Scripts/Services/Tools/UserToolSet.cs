using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using ChatSystem.Models.Tools;
using ChatSystem.Enums;
using ChatSystem.Services.Tools.Interfaces;
using ChatSystem.Services.Logging;

namespace ChatSystem.Services.Tools
{
    public class UserToolSet : IToolSet
    {
        public string ToolSetId => "user-management-toolset";
        public ToolType ToolSetType => ToolType.UserManagement;
        
        private readonly Dictionary<string, object> userData;
        
        public UserToolSet()
        {
            userData = new Dictionary<string, object>();
            LoggingService.LogInfo("UserToolSet initialized");
        }
        
        public List<ToolConfiguration> GetAvailableTools()
        {
            return new List<ToolConfiguration>
            {
                CreateUpdateUserTagTool(),
                CreateUpdateUserNameTool(),
                CreateAddUserCommentTool()
            };
        }
        
        public async Task<ToolResponse> ExecuteToolAsync(ToolCall toolCall)
        {
            LoggingService.LogToolCall(toolCall.name, toolCall.arguments);
            
            ToolResponse response = toolCall.name switch
            {
                "update_user_tag" => await ExecuteUpdateUserTagAsync(toolCall),
                "update_user_name" => await ExecuteUpdateUserNameAsync(toolCall),
                "add_user_comment" => await ExecuteAddUserCommentAsync(toolCall),
                _ => CreateErrorResponse(toolCall.id, $"Unknown tool: {toolCall.name}")
            };
            
            LoggingService.LogToolResponse(toolCall.name, response.content);
            return response;
        }
        
        public async Task<bool> ValidateToolCallAsync(ToolCall toolCall)
        {
            await Task.CompletedTask;
            
            if (!IsToolSupported(toolCall.name))
                return false;
                
            return toolCall.arguments != null;
        }
        
        public bool IsToolSupported(string toolName)
        {
            return toolName switch
            {
                "update_user_tag" or "update_user_name" or "add_user_comment" => true,
                _ => false
            };
        }
        
        private async Task<ToolResponse> ExecuteUpdateUserTagAsync(ToolCall toolCall)
        {
            await Task.Delay(100);
            
            try
            {
                Dictionary<string, object> args = toolCall.arguments;
                string userId = args["id"].ToString();
                string tag = args["tag"].ToString();
                
                string userKey = $"user_{userId}_tag";
                userData[userKey] = tag;
                
                return CreateSuccessResponse(toolCall.id, $"User tag updated successfully for user {userId}");
            }
            catch (Exception ex)
            {
                return CreateErrorResponse(toolCall.id, $"Error updating user tag: {ex.Message}");
            }
        }
        
        private async Task<ToolResponse> ExecuteUpdateUserNameAsync(ToolCall toolCall)
        {
            await Task.Delay(100);
            
            try
            {
                Dictionary<string, object> args = new Dictionary<string, object>(toolCall.arguments);
                string userId = args["id"].ToString();
                string name = args["name"].ToString();
                
                string userKey = $"user_{userId}_name";
                userData[userKey] = name;
                
                return CreateSuccessResponse(toolCall.id, $"User name updated successfully for user {userId}");
            }
            catch (Exception ex)
            {
                return CreateErrorResponse(toolCall.id, $"Error updating user name: {ex.Message}");
            }
        }
        
        private async Task<ToolResponse> ExecuteAddUserCommentAsync(ToolCall toolCall)
        {
            await Task.Delay(100);
            
            try
            {
                Dictionary<string, object> args = new  Dictionary<string, object> (toolCall.arguments);
                string userId = args["id"].ToString();
                string travelId = args["travelId"].ToString();
                string comment = args["comment"].ToString();
                
                string commentKey = $"user_{userId}_travel_{travelId}_comment";
                userData[commentKey] = comment;
                
                return CreateSuccessResponse(toolCall.id, $"Comment added successfully for user {userId} on travel {travelId}");
            }
            catch (Exception ex)
            {
                return CreateErrorResponse(toolCall.id, $"Error adding comment: {ex.Message}");
            }
        }
        
        private ToolConfiguration CreateUpdateUserTagTool()
        {
            return new ToolConfiguration
            {
                toolId = "update_user_tag",
                toolName = "update_user_tag",
                toolType = ToolType.UserManagement,
                inputSchema = new ToolSchema
                {
                    type = "object",
                    properties = new Dictionary<string, ParameterSchema>
                    {
                        ["id"] = new ParameterSchema { type = "string", description = "User ID" },
                        ["tag"] = new ParameterSchema { type = "string", description = "New user tag" }
                    },
                    required = new List<string> { "id", "tag" }
                },
                annotations = new ToolAnnotations
                {
                    title = "Update User Tag",
                    readOnlyHint = false,
                    destructiveHint = false,
                    idempotentHint = true,
                    openWorldHint = false
                }
            };
        }
        
        private ToolConfiguration CreateUpdateUserNameTool()
        {
            return new ToolConfiguration
            {
                toolId = "update_user_name",
                toolName = "update_user_name",
                toolType = ToolType.UserManagement,
                inputSchema = new ToolSchema
                {
                    type = "object",
                    properties = new Dictionary<string, ParameterSchema>
                    {
                        ["id"] = new ParameterSchema { type = "string", description = "User ID" },
                        ["name"] = new ParameterSchema { type = "string", description = "New user name" }
                    },
                    required = new List<string> { "id", "name" }
                },
                annotations = new ToolAnnotations
                {
                    title = "Update User Name",
                    readOnlyHint = false,
                    destructiveHint = false,
                    idempotentHint = true,
                    openWorldHint = false
                }
            };
        }
        
        private ToolConfiguration CreateAddUserCommentTool()
        {
            return new ToolConfiguration
            {
                toolId = "add_user_comment",
                toolName = "add_user_comment",
                toolType = ToolType.UserManagement,
                inputSchema = new ToolSchema
                {
                    type = "object",
                    properties = new Dictionary<string, ParameterSchema>
                    {
                        ["id"] = new ParameterSchema { type = "string", description = "User ID" },
                        ["travelId"] = new ParameterSchema { type = "string", description = "Travel ID" },
                        ["comment"] = new ParameterSchema { type = "string", description = "User comment" }
                    },
                    required = new List<string> { "id", "travelId", "comment" }
                },
                annotations = new ToolAnnotations
                {
                    title = "Add User Comment",
                    readOnlyHint = false,
                    destructiveHint = false,
                    idempotentHint = false,
                    openWorldHint = false
                }
            };
        }
        
        private ToolResponse CreateSuccessResponse(string toolCallId, string content)
        {
            return new ToolResponse
            {
                toolCallId = toolCallId,
                content = content,
                success = true,
                responseTimestamp = DateTime.UtcNow
            };
        }
        
        private ToolResponse CreateErrorResponse(string toolCallId, string error)
        {
            return new ToolResponse
            {
                toolCallId = toolCallId,
                content = error,
                success = false,
                responseTimestamp = DateTime.UtcNow
            };
        }
        

        

    }
}
