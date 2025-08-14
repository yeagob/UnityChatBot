# Unity LLM ChatBot System

**Phase 1 Complete** ✅ - Production-ready Unity chatbot with Model Context Protocol (MCP) compliance, multi-agent orchestration, and real-time tool execution.

## 🎯 Overview

Advanced Unity-based chatbot system featuring multi-LLM agent orchestration, MCP-compliant tool calling, and extensible architecture. Built following SOLID principles with comprehensive debug capabilities and production-ready performance.

### Key Features
- **MCP Compliance**: Full Model Context Protocol implementation
- **Multi-Agent System**: Orchestrated LLM agents with specialized capabilities  
- **Real-Time Tools**: UserToolSet & TravelToolSet with live execution
- **OpenAI Integration**: Validated tool calling without API errors
- **QWEN Integration**: Prepared tool calling 
- **SOLID Architecture**: Extensible, maintainable, production-ready

## ⚙️ Setup Instructions

### 1. Prerequisites
- **Unity**: 2022.3.0f1 or later
- **TextMeshPro**: Imported via Package Manager
- **API Keys**: OpenAI API key for LLM integration

### 2. Configure Provider ScriptableObject

Create Provider Configuration:
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

### 3. Scene Setup

Use the provided **ChatScene** with **ChatPrefab** - works plug & play once providers are configured.

**ChatPrefab Structure:**
```
ChatPrefab
├── ChatManager (All system initialization)
├── ChatCanvas (UI)
└── [Auto-configured components]
```

**ChatManager Configuration:**
- **Provider Configs**: Drag your Provider ScriptableObjects
- **Default Conversation Id**: "main-conversation"
- **Enable Debug Logs**: ✅ (for development)

## 🔧 Configuration Reference

### Provider Configuration Fields
```csharp
ModelConfig (ScriptableObject)  
├── provider: ServiceProvider  // OpenAI, QWEN, Claude
├── apiKey: string           // Provider API key
├── baseURL: string          // API endpoint

```

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

## 🏗️ Architecture

### System Flow
```
User Input → ChatView → ChatController → ChatOrchestrator → LLMOrchestrator → AgentExecutor → ToolSets
```

### Component Hierarchy
```
ChatManager (Root)
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
- Production-ready architecture

**Next Phase**: Multi-provider testing and advanced tool integration

## 📄 License

WIP

---

**Built with Unity 6000.0.45 | By Santiago Dopazo Hilario (@santiagogamelover) | Supported by Claude **
