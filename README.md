# ChatServer – Backend Realtime Chat

ChatServer là backend cho ứng dụng chat realtime, xây dựng bằng **.NET 9**, **SignalR** và **RabbitMQ**.  
Kiến trúc hướng thông điệp (message-driven) giúp tách rời các thành phần, dễ mở rộng và bảo đảm không mất dữ liệu khi một dịch vụ tạm ngừng.

---

## Kiến trúc tổng quan

```text
Client ──HTTP POST──► /api/messages (Producer)
                       │
                       ▼
                 RabbitMQ Exchange
                       │
                       ▼
           RabbitMQ Queue (durable)
                       │
                       ▼
   Background Consumer ──► SignalR Hub ──► Connected Clients
```
