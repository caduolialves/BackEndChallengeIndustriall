# Purchase Order Challenge

API REST em C#/.NET para simular o fluxo de pedido de compras de uma empresa, com cálculo de valor total, aprovação hierárquica por alçada, revisão, reenvio, cancelamento e histórico completo das ações do pedido.

## Contexto

Um colaborador cria um pedido de compra contendo um ou mais itens. Após a criação, o pedido passa por uma cadeia de aprovação sequencial definida pelo valor total:

- Até R$ 100,00: aprovação por Suprimentos.
- Acima de R$ 100,00 e até R$ 1.000,00: aprovação por Suprimentos e Gestor.
- Acima de R$ 1.000,00: aprovação por Suprimentos, Gestor e Diretor.

Durante o fluxo, qualquer aprovador responsável pela etapa atual pode solicitar revisão ou cancelar o pedido. Quando um pedido entra em revisão, ele retorna ao solicitante para ajustes e, depois de reenviado, percorre novamente a cadeia de aprovação desde Suprimentos.

## Regras De Negócio

- RN1: um pedido deve conter pelo menos um item.
- RN2: o valor total do pedido é calculado pela soma de `quantity * unitPrice` dos itens.
- RN3: a cadeia de aprovação segue a alçada do valor total.
- RN4: a aprovação é sequencial; cada aprovador só atua depois da aprovação anterior.
- RN5: qualquer aprovador da etapa atual pode solicitar revisão.
- RN6: criação, aprovação, revisão, reenvio, conclusão e cancelamento são registrados no histórico.
- RN7: o pedido só é concluído após todas as aprovações exigidas pela alçada.
- RN8: qualquer nível de aprovação pode cancelar a solicitação de compra.

## Tecnologias

- C# / ASP.NET Core
- .NET 10.0
- Entity Framework Core
- SQL Server ou Azure SQL
- EF Core Migrations
- OpenAPI/Swagger UI
- Postman

## Estrutura Do Projeto

```text
PurchaseOrderChallenge/
  Controllers/
    PurchaseOrderController.cs
  Data/
    PurchaseOrderDbContext.cs
  Migrations/
    20260421144750_initDB.cs
  Models/
    ApprovalStep.cs
    PurchaseRequest.cs
    PurchaseRequestHistory.cs
    PurchaseRequestItem.cs
    DTOs/
      PurchaseRequestActionRequest.cs
    Enums/
      ApprovalStepStatus.cs
      HistoryActionType.cs
      PurchaseRequestStatus.cs
      UserRole.cs
  Repository/
    ApprovalStepsRepository.cs
    PurchaseRequestHistoryRepository.cs
    PurchaseRequestRepository.cs
    Interfaces/
  Service/
    PurchaseOrderService.cs
    Interfaces/
```

## Modelo De Domínio

Principais entidades:

- `PurchaseRequest`: representa o pedido de compra.
- `PurchaseRequestItem`: representa os itens do pedido.
- `ApprovalStep`: representa cada etapa da cadeia de aprovação.
- `PurchaseRequestHistory`: representa o histórico de ações do pedido.
- `PurchaseRequestActionRequest`: DTO usado para aprovar, revisar ou cancelar pedidos.

Cardinalidades:

```text
PurchaseRequest 1 ---- 1..* PurchaseRequestItem
PurchaseRequest 1 ---- 1..3 ApprovalStep
PurchaseRequest 1 ---- 1..* PurchaseRequestHistory
```

## Banco De Dados

O projeto utiliza `PurchaseOrderDbContext` com os seguintes `DbSet`s:

```csharp
public DbSet<PurchaseRequest> PurchaseRequests => Set<PurchaseRequest>();
public DbSet<PurchaseRequestItem> PurchaseRequestItems => Set<PurchaseRequestItem>();
public DbSet<ApprovalStep> ApprovalSteps => Set<ApprovalStep>();
public DbSet<PurchaseRequestHistory> PurchaseRequestHistories => Set<PurchaseRequestHistory>();
```

A connection string fica em:

- `PurchaseOrderChallenge/appsettings.json`
- `PurchaseOrderChallenge/appsettings.Development.json`

Exemplo:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=localhost;Database=PurchaseOrderChallengeDb;User Id=user_purchase_request;Password=YOUR_PASSWORD;TrustServerCertificate=True;MultipleActiveResultSets=true"
  }
}
```

> Observação: `TrustServerCertificate=True` foi usado para ambiente local com SQL Server e certificado não confiável. Em produção, o ideal é configurar um certificado confiável no servidor.

## Pré-Requisitos

- .NET SDK compatível com o projeto.
- SQL Server local ou Azure SQL.
- Usuário SQL com permissão no banco `PurchaseOrderChallengeDb`.
- Ferramenta de teste de APIs, como Postman ou Insomnia.

## Configuração Do Banco

Crie o login e o banco no SQL Server, se necessário:

```sql
USE master;
GO

IF DB_ID('PurchaseOrderChallengeDb') IS NULL
BEGIN
    CREATE DATABASE [PurchaseOrderChallengeDb];
END
GO

IF NOT EXISTS (
    SELECT 1
    FROM sys.sql_logins
    WHERE name = 'user_purchase_request'
)
BEGIN
    CREATE LOGIN [user_purchase_request]
    WITH PASSWORD = 'YOUR_PASSWORD',
    CHECK_POLICY = OFF;
END
GO

USE [PurchaseOrderChallengeDb];
GO

IF NOT EXISTS (
    SELECT 1
    FROM sys.database_principals
    WHERE name = 'user_purchase_request'
)
BEGIN
    CREATE USER [user_purchase_request]
    FOR LOGIN [user_purchase_request];
END
GO

ALTER ROLE db_owner ADD MEMBER [user_purchase_request];
GO
```

Depois ajuste a senha na `DefaultConnection`.

## Executando O Projeto

Na raiz do repositório:

```bash
dotnet restore PurchaseOrderChallenge/PurchaseOrderChallenge.csproj
```

Aplicar migrations:

```bash
dotnet ef database update --project PurchaseOrderChallenge/PurchaseOrderChallenge.csproj
```

Executar a API:

```bash
dotnet run --project PurchaseOrderChallenge/PurchaseOrderChallenge.csproj
```

Por padrão, o perfil HTTP usa:

```text
http://localhost:5014
```

Em ambiente de desenvolvimento, a documentação OpenAPI/Swagger UI fica disponível em:

```text
http://localhost:5014/swagger
```

## Endpoints

Base URL:

```text
http://localhost:5014/api/PurchaseOrder
```

| Método | Rota | Descrição |
| --- | --- | --- |
| `GET` | `/api/PurchaseOrder` | Lista todos os pedidos. |
| `GET` | `/api/PurchaseOrder/{id}` | Busca um pedido por Id. |
| `POST` | `/api/PurchaseOrder` | Cria um novo pedido. |
| `PUT` | `/api/PurchaseOrder/{id}/approve` | Aprova a etapa atual do pedido. |
| `PUT` | `/api/PurchaseOrder/{id}/review` | Solicita revisão do pedido. |
| `PUT` | `/api/PurchaseOrder/{id}/resubmit` | Reenvia um pedido que estava em revisão. |
| `PUT` | `/api/PurchaseOrder/{id}/cancel` | Cancela um pedido. |

## Exemplos De Requisição

### Criar Pedido

```http
POST /api/PurchaseOrder
Content-Type: application/json
```

```json
{
  "requesterName": "Carlos",
  "items": [
    {
      "productName": "Celular",
      "quantity": 2,
      "unitPrice": 500.00
    }
  ]
}
```

### Aprovar Pedido

```http
PUT /api/PurchaseOrder/1/approve
Content-Type: application/json
```

```json
{
  "approverRole": "Supply",
  "actionBy": "Joao",
  "comments": "Pedido aprovado por Suprimentos."
}
```

Papéis válidos:

```text
Supply
Manager
Director
```

### Solicitar Revisão

```http
PUT /api/PurchaseOrder/1/review
Content-Type: application/json
```

```json
{
  "approverRole": "Manager",
  "actionBy": "Maria",
  "comments": "Necessário revisar os itens do pedido."
}
```

### Reenviar Pedido Revisado

```http
PUT /api/PurchaseOrder/1/resubmit
Content-Type: application/json
```

```json
{
  "requesterName": "Carlos",
  "items": [
    {
      "productName": "Monitor",
      "quantity": 1,
      "unitPrice": 750.00
    }
  ]
}
```

### Cancelar Pedido

```http
PUT /api/PurchaseOrder/1/cancel
Content-Type: application/json
```

```json
{
  "approverRole": "Supply",
  "actionBy": "Joao",
  "comments": "Pedido cancelado por Suprimentos."
}
```

## Coleção Postman

A coleção Postman está disponível na raiz do repositório:

```text
Pedidos de Compras APIs.postman_collection.json
```

Para usar:

1. Abra o Postman.
2. Clique em `Import`.
3. Selecione o arquivo `Pedidos de Compras APIs.postman_collection.json`.
4. Execute as requisições usando a base URL `http://localhost:5014`.

## Diagramas

O desafio solicita:

- Diagrama de Atividades.
- Diagrama de Classes.
- Diagrama Físico de Banco de Dados.

O modelo implementado tem como núcleo:

```text
  PurchaseRequest
  Items
  ApprovalSteps
  History
```

A aprovação é controlada por `ApprovalStep.Sequence` e `ApprovalStep.Status`, garantindo que cada etapa só seja executada depois da anterior.

## Observações De Implementação

- Classes, métodos e variáveis estão em inglês.
- Comentários e docstrings estão em português.
- A API usa interfaces para facilitar injeção de dependência:
  - `IPurchaseOrderService`
  - `IPurchaseRequestRepository`
  - `IApprovalStepsRepository`
  - `IPurchaseRequestHistoryRepository`
- O EF Core usa migrations para versionamento do banco.
- O histórico do pedido registra ações relevantes para rastreabilidade.

## Comandos Úteis

Criar nova migration:

```bash
dotnet ef migrations add NomeDaMigration --project PurchaseOrderChallenge/PurchaseOrderChallenge.csproj
```

Aplicar migrations:

```bash
dotnet ef database update --project PurchaseOrderChallenge/PurchaseOrderChallenge.csproj
```

Compilar:

```bash
dotnet build PurchaseOrderChallenge/PurchaseOrderChallenge.csproj
```

Executar:

```bash
dotnet run --project PurchaseOrderChallenge/PurchaseOrderChallenge.csproj
```
