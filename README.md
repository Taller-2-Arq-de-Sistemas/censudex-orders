# Censudex Orders - Microservicio de Gestión de Pedidos

## Descripción General

**Censudex Orders** es un microservicio basado en gRPC desarrollado con .NET 9.0 que gestiona el ciclo completo de pedidos en el ecosistema Censudex. El servicio implementa patrones avanzados de arquitectura de software como CQRS, Outbox Pattern, y Event-Driven Architecture para garantizar la consistencia de datos y la comunicación asíncrona entre microservicios.

## Tabla de Contenidos

- [Stack Tecnológico](#stack-tecnológico)
- [Patrones de Arquitectura](#patrones-de-arquitectura)
- [Estructura del Proyecto](#estructura-del-proyecto)
- [Operaciones gRPC Disponibles](#operaciones-grpc-disponibles)
- [Esquema de Base de Datos](#esquema-de-base-de-datos)
- [Integración con RabbitMQ](#integración-con-rabbitmq)
- [Servicio de Notificaciones por Email](#servicio-de-notificaciones-por-email)
- [Configuración](#configuración)
- [Ejecución del Proyecto](#ejecución-del-proyecto)
- [Guía de Desarrollo](#guía-de-desarrollo)

---

## Stack Tecnológico

### Framework y Lenguaje

- **.NET 9.0** - Framework principal
- **C# 12** - Lenguaje de programación
- **ASP.NET Core** - Infraestructura web

### Comunicación

- **gRPC** - Protocolo de comunicación principal
- **Protocol Buffers (Protobuf)** - Serialización de mensajes

### Base de Datos

- **MySQL 9.4.0** - Base de datos relacional
- **Entity Framework Core 9.0** - ORM
- **Pomelo.EntityFrameworkCore.MySql** - Proveedor de MySQL

### Mensajería y Eventos

- **RabbitMQ 4.2.0** - Message Broker
- **RabbitMQ.Client** - Cliente oficial de RabbitMQ

### Librerías Principales

- **MediatR** - Implementación de patrón Mediator y CQRS
- **FluentValidation** - Validación declarativa
- **Mapster** - Mapeo de objetos
- **SendGrid** - Servicio de envío de emails

---

## Patrones de Arquitectura

### 1. CQRS (Command Query Responsibility Segregation)

Separación clara entre operaciones de escritura (Commands) y lectura (Queries):

```
Commands (Escritura)          Queries (Lectura)
    |                              |
CreateOrderCommand          GetOrderByIdQuery
UpdateOrderStatusCommand    GetOrderByNumberQuery
CancelOrderCommand          GetOrdersByUserIdQuery
```

**Beneficios:**

- Optimización independiente de lecturas y escrituras
- Código más mantenible y testeable
- Escalabilidad mejorada

### 2. Outbox Pattern

Garantiza la consistencia eventual entre la base de datos y el message broker:

```
1. Transacción DB:
   - Guardar entidad (Order)
   - Guardar evento (OutboxMessage)

2. OutboxProcessorWorker:
   - Lee eventos pendientes
   - Publica a RabbitMQ
   - Marca como publicado
```

**Beneficios:**

- Garantía de entrega de eventos
- No hay pérdida de mensajes si RabbitMQ está caído
- Consistencia transaccional

### 3. Repository Pattern + Unit of Work

Abstracción de la capa de datos:

```csharp
IUnitOfWork
IOrdersRepository
IProductsRepository
IUsersRepository
IOrderProductsRepository
```

**Beneficios:**

- Desacoplamiento de la lógica de negocio
- Facilita testing con mocks
- Gestión centralizada de transacciones

### 4. Mediator Pattern

Desacopla la lógica de negocio usando MediatR:

```
gRPC Service + MediatR + Handler
                 |
            Behaviors (Pipeline)
            - LoggingBehavior
            - CommandValidationBehavior
            - QueryValidationBehavior
```

### 5. Event-Driven Architecture

Comunicación asíncrona entre microservicios:

```
Orders Service                Products Service
     |                              |
[OrderCreated]  -  RabbitMQ  -  [Validate Stock]
     |                              |
[StockInsufficient] - RabbitMQ - [Cancel Order]
```

### 6. Domain Events

Eventos de dominio para operaciones críticas:

```csharp
Order.RaiseOrderCreatedEvent()
   OrderCreatedDomainEvent
     OutboxMessage
       RabbitMQ
         Products/Users Services
```

---

## Estructura del Proyecto

```
censudex-orders/

 src/
    Behaviors/                    # Behaviors de MediatR
       LoggingBehavior.cs        # Logging de requests/responses
       CommandValidationBehavior.cs  # Validación de comandos
       QueryValidationBehavior.cs    # Validación de queries

    CQRS/                         # Interfaces base CQRS
       ICommand.cs               # Interfaz para comandos
       ICommandHandler.cs        # Handler de comandos
       IQuery.cs                 # Interfaz para queries
       IQueryHandler.cs          # Handler de queries

    Data/                         # Contexto y migraciones
       OrdersContext.cs          # DbContext principal
       Seed.cs                   # Seeder principal
       Seeders/                  # Seeders específicos
       Migrations/               # Migraciones de EF Core

    Events/                       # Sistema de eventos
       Domain/                   # Eventos de dominio
          EntityBase.cs         # Clase base con eventos
          IDomainEvent.cs       # Interfaz de eventos
          OrderCreatedDomainEvent.cs

       Integration/              # Eventos de integración
           Published/            # Eventos publicados
              OrderIssuedForStockValidationIntegrationEvent.cs
              (otros eventos publicados)

           Consumed/             # Eventos consumidos
               UserCreatedIntegrationEvent.cs
               ProductCreatedIntegrationEvent.cs
               OrderCancelledByInsufficientStockIntegrationEvent.cs
               (otros eventos consumidos)

    Exceptions/                   # Excepciones personalizadas
       BadRequestException.cs    # Error 400
       NotFoundException.cs      # Error 404
       UnauthorizedException.cs  # Error 401
       InternalServerException.cs # Error 500
       Handler/
           GlobalExceptionHandler.cs  # Interceptor gRPC

    MessageBroker/                # Infraestructura RabbitMQ
       Configuration/
          RabbitMqSettings.cs   # Configuración
          RabbitMqConnection.cs # Gestión de conexión
          OutboxSettings.cs     # Config del Outbox

       Publishers/
          RabbitMqEventPublisher.cs  # Publicador de eventos

       Consumers/                # Consumidores de eventos
          UserCreatedConsumer.cs
          ProductCreatedConsumer.cs
          OrderCancelledByInsufficientStockConsumer.cs
          (otros consumidores)

       Workers/                  # Background workers
           OutboxProcessorWorker.cs      # Procesa outbox
           RabbitMqConsumerWorker.cs     # Consume de RabbitMQ

    Models/                       # Modelos de dominio
       Order.cs                  # Entidad principal
       OrderProducts.cs          # Tabla intermedia
       Product.cs                # Catálogo de productos
       User.cs                   # Usuarios sincronizados
       OutboxMessage.cs          # Mensajes pendientes
       ProcessedEvent.cs         # Eventos procesados (idempotencia)

    Orders/                       # Lógica de negocio
       Commands/                 # Comandos (escritura)
          CreateOrderCommand.cs
          UpdateOrderStatusCommand.cs
          CancelOrderCommand.cs

       Queries/                  # Queries (lectura)
           GetOrderByIdQuery.cs
           GetOrderByNumberQuery.cs
           GetOrdersByUserIdQuery.cs

    Protos/                       # Definiciones Protocol Buffers
       orders.proto              # Contrato gRPC

    Repositories/                 # Capa de datos
       Interfaces/
          IUnitOfWork.cs
          IOrdersRepository.cs
          IProductsRepository.cs
          IUsersRepository.cs
          IOrderProductsRepository.cs
          IOutboxRepository.cs
          IProcessedEventsRepository.cs

       (Implementaciones)
           UnitOfWork.cs
           OrdersRepository.cs
           (otros repositorios)

    Services/                     # Servicios
       OrdersService.cs          # Servicio gRPC principal
       SendGridService.cs        # Servicio de emails
       EmailTemplateService.cs   # Renderizado de templates
       Interfaces/
           ISendGridService.cs
           IEmailTemplateService.cs

    Views/                        # Templates de emails
       Base/
           EmailTemplate.html    # Template base
       Emails/
           OrderConfirmation.html
           OrderStatusUpdate.html
           OrderCancellation.html

 Program.cs                        # Punto de entrada
 appsettings.json                  # Configuración base
 Dockerfile                        # Imagen Docker
 docker-compose.yaml               # Composición de servicios
 docker-compose.override.yaml      # Override para desarrollo
 .env.development                  # Variables de entorno
```

---

## Operaciones gRPC Disponibles

### Servicio: `OrdersProtoService`

Todas las operaciones están definidas en `src/Protos/orders.proto`

#### 1. CreateOrder

**Descripción:** Crea un nuevo pedido

**Request:**

```protobuf
message CreateOrderRequest {
    string customerId = 1;
    repeated ProductOrdered products = 2;
}

message ProductOrdered {
    string productId = 1;
    int32 quantity = 2;
}
```

**Response:**

```protobuf
message CreateOrderResponse {
    Order order = 1;
}
```

**Flujo:**

1. Valida que el cliente existe
2. Valida que los productos existen y tienen stock suficiente
3. Reduce el stock de los productos
4. Crea el pedido con estado "pendiente"
5. Envía email de confirmación
6. Publica evento `OrderIssuedForStockValidationIntegrationEvent`

**Validaciones:**

- CustomerId debe ser un GUID válido
- Debe haber al menos 1 producto
- Cada ProductId debe ser un GUID válido
- Quantity debe ser mayor a 0

---

#### 2. GetOrderById

**Descripción:** Obtiene un pedido por su ID

**Request:**

```protobuf
message GetOrderByIdRequest {
    string orderId = 1;
    string customerId = 2;
    string customerRole = 3;  // "Admin" o "User"
}
```

**Response:**

```protobuf
message GetOrderByIdResponse {
    Order order = 1;
}
```

**Autorización:**

- Si `customerRole == "Admin"`: acceso completo
- Si `customerRole == "User"`: solo puede ver sus propios pedidos

**Validaciones:**

- OrderId debe ser un GUID válido
- CustomerId debe ser un GUID válido
- CustomerRole debe ser "Admin" o "User"

---

#### 3. GetOrderByNumber

**Descripción:** Obtiene un pedido por su número de orden

**Request:**

```protobuf
message GetOrderByNumberRequest {
    int32 orderNumber = 1;
    string customerId = 2;
    string customerRole = 3;
}
```

**Response:**

```protobuf
message GetOrderByNumberResponse {
    Order order = 1;
}
```

**Validaciones:**

- OrderNumber debe ser mayor a 0
- CustomerId debe ser un GUID válido
- CustomerRole debe ser "Admin" o "User"

---

#### 4. GetOrdersByUserId

**Descripción:** Obtiene todos los pedidos de un usuario

**Request:**

```protobuf
message GetOrdersByUserIdRequest {
    string userId = 1;
}
```

**Response:**

```protobuf
message GetOrdersByUserIdResponse {
    repeated Order orders = 1;
}
```

**Validaciones:**

- UserId debe ser un GUID válido
- El usuario debe existir en la base de datos

---

#### 5. UpdateOrderStatus

**Descripción:** Actualiza el estado de un pedido

**Request:**

```protobuf
message UpdateOrderStatusRequest {
    string orderId = 1;
    string newStatus = 2;  // "pendiente", "en procesamiento", "enviado", "entregado", "cancelado"
    string userRole = 3;   // Solo "Admin" puede actualizar
}
```

**Response:**

```protobuf
google.protobuf.Empty
```

**Estados Válidos:**

- `pendiente` - `en procesamiento`, `cancelado`
- `en procesamiento` - `enviado`, `cancelado`
- `enviado` - `entregado`
- `entregado` (estado final)
- `cancelado` (estado final)

**Flujo:**

1. Valida que el pedido existe
2. Valida que el usuario es Admin
3. Actualiza el estado
4. Envía email de notificación

**Validaciones:**

- OrderId debe ser un GUID válido
- NewStatus debe ser un estado válido
- UserRole debe ser "Admin"

---

#### 6. CancelOrder

**Descripción:** Cancela un pedido

**Request:**

```protobuf
message CancelOrderRequest {
    string orderId = 1;
    string customerId = 2;
    string customerRole = 3;
}
```

**Response:**

```protobuf
google.protobuf.Empty
```

**Autorización:**

- Usuarios normales solo pueden cancelar sus propios pedidos
- Admins pueden cancelar cualquier pedido

**Validaciones:**

- OrderId debe ser un GUID válido
- CustomerId debe ser un GUID válido
- CustomerRole debe ser "Admin" o "User"

---

### Estructura del Mensaje Order

```protobuf
message Order {
    string id = 1;
    int32 orderNumber = 2;
    string status = 3;
    int32 totalCharge = 4;
    google.protobuf.Timestamp createdAt = 5;
    string customerId = 6;
    repeated OrderProduct orderProducts = 7;
}

message OrderProduct {
    string productId = 1;
    int32 quantity = 2;
    int32 price = 3;  // Precio al momento de la compra
}
```

---

## Esquema de Base de Datos

### Tabla: Orders

```sql
CREATE TABLE Orders (
    Id CHAR(36) PRIMARY KEY,
    OrderNumber INT AUTO_INCREMENT UNIQUE,
    Status VARCHAR(50) NOT NULL,
    TotalCharge INT NOT NULL,
    CreatedAt DATETIME NOT NULL,
    CustomerId CHAR(36) NOT NULL,
    FOREIGN KEY (CustomerId) REFERENCES Users(Id)
);
```

### Tabla: OrderProducts

```sql
CREATE TABLE OrderProducts (
    Id CHAR(36) PRIMARY KEY,
    OrderId CHAR(36) NOT NULL,
    ProductId CHAR(36) NOT NULL,
    Quantity INT NOT NULL,
    Price INT NOT NULL,
    FOREIGN KEY (OrderId) REFERENCES Orders(Id),
    FOREIGN KEY (ProductId) REFERENCES Products(Id)
);
```

### Tabla: Products (Sincronizada)

```sql
CREATE TABLE Products (
    Id CHAR(36) PRIMARY KEY,
    Name VARCHAR(255) NOT NULL,
    Description TEXT,
    Price INT NOT NULL,
    Stock INT NOT NULL,
    IsActive BOOLEAN NOT NULL DEFAULT TRUE
);
```

### Tabla: Users (Sincronizada)

```sql
CREATE TABLE Users (
    Id CHAR(36) PRIMARY KEY,
    Name VARCHAR(255) NOT NULL,
    Email VARCHAR(255) NOT NULL UNIQUE,
    IsActive BOOLEAN NOT NULL DEFAULT TRUE
);
```

### Tabla: OutboxMessages

```sql
CREATE TABLE OutboxMessages (
    Id CHAR(36) PRIMARY KEY,
    EventType VARCHAR(255) NOT NULL,
    Payload TEXT NOT NULL,
    CreatedAt DATETIME NOT NULL,
    PublishedAt DATETIME NULL,
    Error TEXT NULL,
    ProcessedCount INT DEFAULT 0
);
```

### Tabla: ProcessedEvents

```sql
CREATE TABLE ProcessedEvents (
    Id CHAR(36) PRIMARY KEY,
    EventId CHAR(36) NOT NULL UNIQUE,
    EventType VARCHAR(255) NOT NULL,
    ProcessedAt DATETIME NOT NULL
);
```

---

## Integración con RabbitMQ

### Configuración del Exchange

- **Nombre:** `censudex.events`
- **Tipo:** `topic`
- **Durable:** `true`

### Eventos Publicados

| Evento                                          | Routing Key                                                     | Descripci�n                                           |
| ----------------------------------------------- | --------------------------------------------------------------- | ----------------------------------------------------- |
| `OrderIssuedForStockValidationIntegrationEvent` | `censudex.orders.orderissuedforstockvalidationintegrationevent` | Solicita validación de stock al servicio de productos |

**Payload de Ejemplo:**

```json
{
  "eventId": "uuid",
  "orderId": "uuid",
  "orderNumber": 12345,
  "customerId": "uuid",
  "products": [
    {
      "productId": "uuid",
      "quantity": 2
    }
  ],
  "occurredAt": "2024-11-16T10:00:00Z"
}
```

---

### Eventos Consumidos

El servicio consume eventos de:

- **Users Service:** `censudex.users.*`
- **Products Service:** `censudex.products.*`

| Evento                                              | Routing Key                                                           | Acción                            |
| --------------------------------------------------- | --------------------------------------------------------------------- | --------------------------------- |
| `UserCreatedIntegrationEvent`                       | `censudex.users.usercreatedintegrationevent`                          | Crea usuario local                |
| `UserUpdatedIntegrationEvent`                       | `censudex.users.userupdatedintegrationevent`                          | Actualiza usuario local           |
| `UserDeletedIntegrationEvent`                       | `censudex.users.userdeletedintegrationevent`                          | Marca usuario como inactivo       |
| `ProductCreatedIntegrationEvent`                    | `censudex.products.productcreatedintegrationevent`                    | Crea producto local               |
| `ProductUpdatedIntegrationEvent`                    | `censudex.products.productupdatedintegrationevent`                    | Actualiza producto local          |
| `ProductDeletedIntegrationEvent`                    | `censudex.products.productdeletedintegrationevent`                    | Marca producto como inactivo      |
| `OrderCancelledByInsufficientStockIntegrationEvent` | `censudex.products.ordercancelledbyinsufficientstockintegrationevent` | Cancela pedido por falta de stock |

---

### Sistema de Reintentos y Dead Letter Queue

**Configuración:**

- **Max Retries:** 3
- **Retry Delay:** Exponencial (2^n segundos)
- **Dead Letter Exchange:** `censudex.dlx`
- **Dead Letter Queue:** `censudex.orders.dlq`

**Flujo de Reintentos:**

```
Mensaje falla
  |
Reintento 1 (2s delay)
  |
Reintento 2 (4s delay)
  |
Reintento 3 (8s delay)
  |
Dead Letter Queue
```

---

## Servicio de Notificaciones por Email

### Proveedor: SendGrid

### Templates Disponibles

#### 1. Order Confirmation

**Cuando:** Al crear un pedido
**Variables:**

- `CustomerName`
- `OrderNumber`
- `OrderDate`
- `OrderStatus`
- `TotalAmount`

#### 2. Order Status Update

**Cuando:** Al cambiar el estado del pedido
**Variables:**

- `CustomerName`
- `OrderNumber`
- `NewStatus`

#### 3. Order Cancellation

**Cuando:** Al cancelar un pedido
**Variables:**

- `CustomerName`
- `OrderNumber`
- `CancellationReason`

### Sistema de Templates

Los templates se encuentran en `src/Views/`:

- **Base:** `Base/EmailTemplate.html` - Template maestro con branding
- **Contenido:** `Emails/*.html` - Templates específicos por tipo

**Características:**

- Responsive design
- Inline CSS para compatibilidad con clientes de email
- Reemplazo de placeholders `{{Variable}}`

---

## Configuración

### Variables de Entorno

#### Base de Datos

```env
ConnectionStrings__Database=server=orders-db;port=3306;database=orders;uid=root;pwd=password
```

#### SendGrid

```env
SendGrid__ApiKey=tu-api-key
SendGrid__FromEmail=noreply@tudominio.com
```

#### RabbitMQ

```env
RabbitMQ__Host=rabbitmq
RabbitMQ__Port=5672
RabbitMQ__VirtualHost=/
RabbitMQ__Username=guest
RabbitMQ__Password=guest
RabbitMQ__ExchangeName=censudex.events
RabbitMQ__ExchangeType=topic
RabbitMQ__QueueName=censudex.orders.queue
```

### Archivo: appsettings.json

```json
{
  "ConnectionStrings": {
    "Database": ""
  },
  "SendGrid": {
    "ApiKey": "",
    "FromEmail": ""
  },
  "RabbitMQ": {
    "Host": "localhost",
    "Port": 5672,
    "ExchangeName": "censudex.events",
    "QueueName": "censudex.orders.queue",
    "MaxRetryCount": 3,
    "PrefetchCount": 10
  },
  "Kestrel": {
    "EndpointDefaults": {
      "Protocols": "Http2"
    }
  }
}
```

---

## Ejecución del Proyecto

### Requisitos Previos

- Docker y Docker Compose
- .NET 9.0 SDK (para desarrollo local)
- MySQL 9.4.0 (si ejecutas sin Docker)
- RabbitMQ 4.2.0 (si ejecutas sin Docker)

### Con Docker Compose (Recomendado)

```bash
# 1. Clonar el repositorio
git clone <repo-url>
cd censudex-orders

# 2. Configurar variables de entorno
cp .env.example .env.development
# Editar .env.development con tus valores

# 3. Construir y levantar servicios
docker-compose up --build

# 4. El servicio estará disponible en:
# - gRPC: localhost:6000
# - RabbitMQ Management: http://localhost:15672
# - MySQL: localhost:3306
```

### Ejecución Local (Sin Docker)

```bash
# 1. Configurar user secrets
dotnet user-secrets set "ConnectionStrings:Database" "tu-connection-string"
dotnet user-secrets set "SendGrid:ApiKey" "tu-api-key"
dotnet user-secrets set "SendGrid:FromEmail" "tu-email"

# 2. Aplicar migraciones
dotnet ef database update

# 3. Ejecutar el proyecto
dotnet run
```

### Comandos útiles

```bash
# Ver logs
docker-compose logs -f censudex-orders

# Reiniciar servicios
docker-compose restart

# Detener servicios
docker-compose down

# Ver estado de contenedores
docker-compose ps

# Acceder a la base de datos
docker exec -it orders-db mysql -u root -p

# Ver cola de RabbitMQ
# Acceder a: http://localhost:15672
# Usuario: guest / Contraseña: guest
```

## Manejo de Errores

### Excepciones Personalizadas

El servicio define excepciones personalizadas que se mapean a c�digos gRPC:

| Excepción                 | Código gRPC        | HTTP Equiv. |
| ------------------------- | ------------------ | ----------- |
| `NotFoundException`       | `NotFound`         | 404         |
| `BadRequestException`     | `InvalidArgument`  | 400         |
| `UnauthorizedException`   | `PermissionDenied` | 401         |
| `InternalServerException` | `Internal`         | 500         |
| `ValidationException`     | `InvalidArgument`  | 400         |

### GlobalExceptionHandler

Interceptor gRPC que captura todas las excepciones y las formatea:

```csharp
try
{
    return await continuation(request, context);
}
catch (NotFoundException ex)
{
    throw new RpcException(new Status(StatusCode.NotFound, ex.Message));
}
catch (ValidationException ex)
{
    // Formatea errores de validaci�n con detalles
    var status = new Status(StatusCode.InvalidArgument, "Validation failed");
    throw new RpcException(status, CreateValidationMetadata(ex));
}
```

---

## Buenas Prácticas

### 1. Validación

- Usar FluentValidation para todas las entradas
- Validar GUIDs, rangos numéricos, y strings no vacíos
- Mensajes de error descriptivos

### 2. Logging

- LoggingBehavior registra automáticamente todas las operaciones
- Usar niveles apropiados (Information, Warning, Error)
- No loggear información sensible

### 3. Transacciones

- Usar `IUnitOfWork` para operaciones que requieren consistencia
- Guardar eventos de dominio en la misma transacción que la entidad

### 4. Eventos

- Publicar eventos ANTES de `SaveChangesAsync()` para que se guarden en Outbox
- Usar nombres descriptivos para eventos
- Incluir `EventId` y `OccurredAt` en todos los eventos

### 5. Idempotencia

- Usar `ProcessedEvents` para evitar procesar eventos duplicados
- Verificar `EventId` antes de procesar eventos consumidos

---

## Troubleshooting

### Problema: RabbitMQ no conecta en Docker

**Solución:** Verificar que todos los servicios están en la misma red:

```yaml
networks:
  - censudex-network
```

### Problema: Migraciones no se aplican

**Solución:** Ejecutar manualmente:

```bash
dotnet ef database update
```

### Problema: Eventos no se publican

**Verificar:**

1. OutboxProcessorWorker está ejecutándose
2. Conexión a RabbitMQ exitosa
3. Eventos guardados en tabla `OutboxMessages`

### Problema: Emails no se envían

**Verificar:**

1. SendGrid ApiKey configurado
2. FromEmail verificado en SendGrid
3. Templates copiados a directorio de salida

---

## Recursos Adicionales

- **Documentaci�n T�cnica Detallada:** Ver `CLAUDE.md`
- **Protocol Buffers:** [https://protobuf.dev/](https://protobuf.dev/)
- **gRPC .NET:** [https://learn.microsoft.com/en-us/aspnet/core/grpc/](https://learn.microsoft.com/en-us/aspnet/core/grpc/)
- **MediatR:** [https://github.com/jbogard/MediatR](https://github.com/jbogard/MediatR)
- **FluentValidation:** [https://docs.fluentvalidation.net/](https://docs.fluentvalidation.net/)
- **RabbitMQ:** [https://www.rabbitmq.com/documentation.html](https://www.rabbitmq.com/documentation.html)

---

## Autor

Jairo Calcina Valda - 20.734.228-9
