# Messaging Service

The Messaging Service is the core communication microservice of the chat application. It handles all message operations, reactions, and attachments.

## Features

- **Message Management**: Send, edit, delete messages
- **Threading**: Reply to messages to create conversation threads
- **Reactions**: Add/remove emoji reactions to messages
- **Search**: Full-text search within channel messages
- **Soft Delete**: Preserve conversation history while hiding deleted content
- **Pagination**: Efficient loading of large message histories
- **Authorization**: Channel membership verification before operations

## Architecture

The service follows Clean Architecture with DDD principles:

- **Domain Layer**: Business logic, entities, value objects, domain events
- **Application Layer**: Commands, queries, DTOs, validators (CQRS with MediatR)
- **Infrastructure Layer**: Database, repositories, HTTP clients
- **API Layer**: REST controllers, authentication, configuration

## Technology Stack

- .NET 10.0
- PostgreSQL (with Entity Framework Core)
- MediatR (CQRS pattern)
- FluentValidation
- AutoMapper
- Serilog (structured logging)
- JWT Bearer authentication

## Database Schema

### Tables

- **messages**: Main message data
- **message_reactions**: Emoji reactions (soft delete pattern)
- **message_attachments**: File attachment metadata

### Key Indexes

- Channel + CreatedAt + IsDeleted (composite for efficient message retrieval)
- MessageId + Emoji + IsRemoved (for reaction grouping)
- Full-text index on Content (for search)

## API Endpoints

### Messages

- `POST /api/channels/{channelId}/messages` - Send message
- `GET /api/channels/{channelId}/messages` - Get messages (paginated)
- `GET /api/channels/{channelId}/messages/{messageId}` - Get specific message
- `GET /api/channels/{channelId}/messages/search` - Search messages
- `PUT /api/channels/{channelId}/messages/{messageId}` - Edit message
- `DELETE /api/channels/{channelId}/messages/{messageId}` - Delete message

### Reactions

- `POST /api/channels/{channelId}/messages/{messageId}/reactions` - Add reaction
- `DELETE /api/channels/{channelId}/messages/{messageId}/reactions/{emoji}` - Remove reaction

## Getting Started

### Prerequisites

- .NET 10.0 SDK
- PostgreSQL 16
- Channel Service running on port 5003
- User Management Service running on port 5002

### Setup

1. **Create Database**:
```bash
   createdb messaging_service
```

2. **Update Connection String** in `appsettings.json`:
```json
   "ConnectionStrings": {
     "PostgreSQL": "Host=localhost;Port=5432;Database=messaging_service;Username=postgres;Password=yourpassword"
   }
```

3. **Run Migrations**:
```bash
   cd src/MessagingService.Infrastructure
   dotnet ef migrations add InitialCreate --startup-project ../MessagingService.API
   dotnet ef database update --startup-project ../MessagingService.API
```

4. **Run the Service**:
```bash
   cd src/MessagingService.API
   dotnet run
```

The service will start on `http://localhost:5004`.

### Swagger UI

Access API documentation at: `http://localhost:5004`

## Configuration

### JWT Settings

Must match Identity Service configuration:
```json
"Jwt": {
  "SecretKey": "your-secret-key",
  "Issuer": "IdentityService",
  "Audience": "ChatApplication"
}
```

### Service URLs

Configure other service endpoints:
```json
"Services": {
  "ChannelService": {
    "BaseUrl": "http://localhost:5003"
  },
  "UserManagementService": {
    "BaseUrl": "http://localhost:5002"
  }
}
```

## Business Rules

### Messages

- Users can only send messages to channels they're members of
- Only message sender can edit their own messages
- Only message sender can delete their own messages
- System messages cannot be edited
- Deleted messages show "[Message deleted]" but preserve metadata
- Messages support threading via ParentMessageId

### Reactions

- Users can only react once per emoji per message
- Removed reactions are soft-deleted for history
- Re-reacting with same emoji restores the removed reaction
- Cannot react to deleted messages

## Performance Considerations

- **IQueryable Pattern**: All queries use deferred execution
- **Composite Indexes**: Optimized for common query patterns
- **Pagination**: Default 50 messages per page
- **Batch User Lookups**: Minimize inter-service calls
- **Connection Pooling**: PostgreSQL connection pooling enabled

## Logging

Structured logging with Serilog:
- Console output for development
- File output: `logs/messaging-service-{Date}.txt`
- Seq integration ready (configure in appsettings)

## Health Checks

- **Endpoint**: `GET /health`
- **Checks**: Database connectivity

## Domain Events

Domain events are defined but not yet published. When RabbitMQ integration is added:

- `MessageSentEvent` → Real-time Service pushes to WebSocket clients
- `MessageEditedEvent` → Update UI for active viewers
- `MessageDeletedEvent` → Remove from UI for active viewers
- `ReactionAddedEvent` → Update reaction counts in real-time
- `ReactionRemovedEvent` → Update reaction counts in real-time

## Future Enhancements

- [ ] Implement event publishing to RabbitMQ
- [ ] Add read receipts
- [ ] Implement typing indicators
- [ ] Add message pinning
- [ ] Support for message forwarding
- [ ] Implement message translation
- [ ] Add rich text formatting
- [ ] Support for code block syntax highlighting