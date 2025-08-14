# Unity LLM ChatBot System

**Phase 1 Complete** ✅ - Production-ready Unity chatbot with Model Context Protocol (MCP) compliance, multi-agent orchestration, and real-time tool execution.

## 🎯 Overview

Advanced Unity-based chatbot system featuring multi-LLM agent orchestration, MCP-compliant tool calling, and extensible architecture. Built following SOLID principles with comprehensive debug capabilities and production-ready performance.

### Key Features
- **MCP Compliance**: Full Model Context Protocol implementation
- **Multi-Agent System**: Orchestrated LLM agents with specialized capabilities  
- **Real-Time Tools**: UserToolSet & TravelToolSet with live execution
- **OpenAI Integration**: Validated tool calling without API errors
- **Debug System**: Comprehensive runtime monitoring and testing
- **SOLID Architecture**: Extensible, maintainable, production-ready

## 🏗️ Architecture

### System Flow
```
User Input → ChatView → ChatController → ChatOrchestrator → LLMOrchestrator → AgentExecutor → ToolSets
```

### Component Hierarchy
```
DependencyBootstrap (Root)
├── Services
│   ├── ContextManager (Conversation state)
│   ├── AgentExecutor (Multi-agent execution)
│   ├── PersistenceService (Storage layer)
│   └── LoggingService (Centralized logging)
├── Orchestrators  
│   ├── ChatOrchestrator (Flow coordination)
│   └── LLMOrchestrator (Agent management)
├── Controllers
│   └── ChatController (Business logic)
├── Views
│   ├── ChatView (Main UI)
│   └── MessageView (Individual messages)
└── ToolSets
    ├── UserToolSet (User management)
    └── TravelToolSet (Travel search)
```

### MVC + Orchestrator Pattern
- **Model**: ConversationContext, Message, Agent configurations
- **View**: ChatView (UI), MessageView (Components)  
- **Controller**: ChatController (Business logic)
- **Orchestrators**: ChatOrchestrator, LLMOrchestrator (Coordination)

## ⚙️ Setup Instructions

### 1. Prerequisites
- **Unity**: 2022.3.0f1 or later
- **TextMeshPro**: Imported via Package Manager
- **API Keys**: OpenAI API key for LLM integration

### 2. Configure ScriptableObjects

#### Create Provider Configuration
```
Assets → Create → LLM → Provider Configuration
```
**Required Settings:**
- **Provider**: OpenAI
- **Model Name**: "gpt-4" or "gpt-3.5-turbo"  
- **API Key**: Your OpenAI API key
- **Base URL**: "https://api.openai.com/v1"
- **Temperature**: 0.7-1.0
- **Max Tokens**: 2048-4096

#### Create Agent Configuration
```
Assets → Create → LLM → Agent Configuration  
```
**Example Travel Agent:**
- **Agent ID**: "travel-specialist"
- **Model Config**: Link Provider Configuration
- **System Prompt**: "You are Ana, travel specialist at Exoticca..."
- **Available Tools**: Select TravelToolSet tools
- **Debug Tools**: true (for development)
- **Max Tool Calls**: 5

### 3. Scene Setup

#### Core GameObject Structure
```
Scene
├── ChatCanvas (ChatView script)
│   └── [UI Components - auto-configured]
├── SystemBootstrap (DependencyBootstrap script)
│   ├── Chat View: Link ChatCanvas
│   ├── Agent Configs: Drag Agent ScriptableObjects
│   ├── Enable Debug Logs: ✅
│   └── Create Debug Objects: ✅
└── EventSystem (Auto-created)
```

#### DependencyBootstrap Configuration
- **Chat View**: Drag ChatCanvas GameObject
- **Default Conversation Id**: "main-conversation"  
- **Agent Configs**: Drag created Agent ScriptableObjects
- **Enable Debug Logs**: ✅ (for development)
- **Create Debug Objects**: ✅ (adds debug GameObjects)

### 4. UI Configuration (Auto-Setup)
The system auto-configures UI when properly referenced:
- **Input Field**: Message input with Enter key support
- **Send Button**: Triggers message processing
- **Scroll View**: Auto-scrolling message container
- **Message Prefab**: Auto-instantiated for each message

## 🛠️ Available Tools

### UserToolSet
- **update_user_tag**: Modify user classification tags
- **update_user_name**: Change user display name
- **add_user_comment**: Add notes to user profile

### TravelToolSet  
- **search_travels_by_country**: Find destinations by country
- **search_travels_advanced**: Advanced search with filters
- **get_travel_details**: Retrieve specific travel information

### Tool Execution Example
```
User: "I want to travel to Japan"
System: 🔧 Tool Executed: search_travels_by_country(country="Japan")
Assistant: I found 15 amazing destinations in Japan including Tokyo, Kyoto, and Osaka...
```

## 🔧 Configuration Reference

### Agent Configuration Fields
```csharp
AgentConfig (ScriptableObject)
├── agentId: string             // Unique identifier
├── modelConfig: ModelConfig    // LLM settings
├── promptConfig: PromptConfig  // System prompts  
├── availableTools: ToolConfig[] // Assigned tools
├── debugTools: bool           // Debug messages in chat
├── maxToolCalls: int         // Tool call limit
└── temperature: float        // Response creativity
```

### Model Configuration Fields
```csharp
ModelConfig (ScriptableObject)  
├── provider: ServiceProvider  // OpenAI, QWEN, Claude
├── modelName: string         // "gpt-4", "gpt-3.5-turbo"
├── apiKey: string           // Provider API key
├── baseURL: string          // API endpoint
├── temperature: float       // 0.0-2.0
├── maxTokens: int          // Response limit
├── costPer1KTokens: float  // Cost tracking
└── timeoutMs: int          // Request timeout
```

## 🧪 Testing & Debug

### Debug Features
- **Debug GameObjects**: Auto-created with ContextMenu testing
- **Tool Debug Messages**: 🔧/❌ indicators in chat
- **Comprehensive Logging**: Debug/Info/Warning/Error/Critical levels
- **Runtime Inspection**: Service states and message flow

### Testing Scenarios
1. **Basic Chat**: "Hello" → Assistant response
2. **Tool Calling**: "I want to travel to China" → Automatic tool execution
3. **Multi-Tool**: Complex queries triggering multiple tools
4. **Error Recovery**: Invalid tool calls, API errors
5. **Debug Messages**: Tool execution visibility in chat

### ContextMenu Testing
Right-click debug GameObjects for:
- **Test Process Message**: Simulate user input
- **Show Active Agents**: Display registered agents
- **Show Service Info**: Runtime service status
- **Clear Context**: Reset conversation state

## 📊 Performance Metrics

### Response Times (Typical)
- **Basic Chat**: 1-2 seconds
- **Tool Execution**: 1.5-2.5 seconds  
- **Multi-Tool**: 3-5 seconds
- **Context Load**: <100ms

### Scalability (Tested)
- **Concurrent Conversations**: 10+
- **Messages per Conversation**: 100+
- **Registered ToolSets**: 10+
- **Agents per Conversation**: 5+

## 🔌 Supported LLM Providers

### OpenAI (✅ Fully Functional)
- **Models**: GPT-4, GPT-3.5-turbo, GPT-4-turbo
- **Tool Calling**: Complete MCP implementation
- **Status**: Production ready

### QWEN (🔄 Prepared)
- **Models**: qwen-max, qwen-plus, qwen-turbo
- **Tool Calling**: Architecture ready
- **Status**: Integration pending

### Claude (🔄 Framework Ready)
- **Models**: Claude-3, Claude-2
- **Tool Calling**: MCP structure prepared  
- **Status**: Implementation pending

## 📁 Project Structure

```
Assets/Scripts/
├── Configuration/
│   ├── ScriptableObjects/    # Agent, Tool, Model configs
│   └── ApplicationConfig.cs  # Global settings
├── Controllers/
│   └── ChatController.cs     # Business logic
├── Views/Chat/
│   ├── ChatView.cs          # Main UI
│   └── MessageView.cs       # Message components
├── Services/
│   ├── Orchestrators/       # Chat & LLM orchestration
│   ├── Context/            # Conversation management
│   ├── Agents/             # Agent execution
│   ├── Tools/              # Tool systems
│   ├── Persistence/        # Storage layer
│   └── Logging/            # Centralized logging
├── Models/
│   ├── Context/            # Message & conversation
│   ├── Tools/MCP/          # MCP structures
│   └── Agents/             # Agent definitions
└── Enums/                  # Type definitions
```

## 🚀 Production Deployment

### Security Checklist
- ✅ **API Key Security**: Secure token handling
- ✅ **Input Validation**: Tool parameter validation  
- ✅ **Tool Sandboxing**: Isolated execution
- ✅ **Error Handling**: Comprehensive try/catch
- ✅ **Rate Limiting**: Framework prepared

### Monitoring Ready
- ✅ **Comprehensive Logging**: All operations tracked
- ✅ **Performance Metrics**: Response time monitoring
- ✅ **Error Tracking**: Exception capture
- ✅ **Usage Analytics**: Tool usage statistics
- ✅ **Cost Tracking**: Token usage monitoring

## 🔮 Phase 2 Roadmap

### High Priority
1. **Multi-Provider Testing**: Validate QWEN & Claude integration
2. **Advanced Tools**: Weather, Calendar, Database integration
3. **Enhanced UI**: Rich formatting, progress indicators
4. **Performance**: Caching, streaming, optimization

### Enterprise Features
- Multi-tenant support
- Analytics dashboard  
- A/B testing framework
- Advanced user management

## 📝 Current Status

**Phase 1**: ✅ **COMPLETE**
- MCP compliance validated
- OpenAI integration functional
- Multi-agent orchestration working
- Tool execution in real-time
- Debug system operational
- Production-ready architecture

**Next Phase**: Multi-provider testing and advanced tool integration

## 📄 License

Private repository - All rights reserved

---

**Built with Unity 2022.3 | Powered by OpenAI | MCP Compliant**