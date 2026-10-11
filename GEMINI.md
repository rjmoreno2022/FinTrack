# 💰 GEMINI.md - Contexto y Reglas de Desarrollo de FinTrack

> **Documento de Contexto Operativo y Reglas Arquitectónicas para Modelos de Inteligencia Artificial y Agentes Autónomos.**  
> Basado en la **Guía de Desarrollo FinTrack v1.1** (`FinTrack-Documentacion-Desarrollo-v1.1.html`).

---

## 1. 📌 Visión General del Proyecto

**FinTrack** es una solución integral de finanzas personales diseñada para operar en economías con múltiples monedas coexistentes, con énfasis primordial en el contexto de **Venezuela**, donde el Bolívar (`VES`), el Dólar Estadounidense (`USD`), y las stablecoins (`USDT`) conviven en las transacciones cotidianas.

* **Paradigma Arquitectónico:** Clean Architecture (Arquitectura Cebolla / Onion) combinada con **Domain-Driven Design (DDD)** y el patrón **CQRS** (Command Query Responsibility Segregation).
* **Topología de Despliegue (MVP):** **Monolito Modular**. El frontend interactivo (Blazor Server) y los endpoints backend (ASP.NET Core Web API Controllers) conviven dentro del proyecto anfitrión `FinTrack.Web` para minimizar la sobrecarga operativa, sin romper la estricta separación de capas de Clean Architecture.
* **Plataforma:** .NET 10 (`net10.0` LTS) / C# 13 (con base de diseño migrada desde .NET 8 LTS).

---

## 2. 🏛️ Arquitectura y Reglas de Dependencia

La solución se divide en 4 capas concéntricas con dependencia unidireccional hacia el centro:

```
+-------------------------------------------------------+
|              Capa de Presentación                     |
|            (FinTrack.Web / Blazor)                    |
|   Pages, Layouts, Controllers, AppState, Servicios UI |
+---------------------------+---------------------------+
                            | Depende de
+---------------------------v---------------------------+
|              Capa de Infraestructura                  |
|             (FinTrack.Infrastructure)                 |
|   EF Core DbContext, Repositorios, Identity,          |
|   Conversión de Moneda, Serilog, Exportadores         |
+---------------------------+---------------------------+
                            | Depende de
+---------------------------v---------------------------+
|               Capa de Aplicación                      |
|             (FinTrack.Application)                    |
|   CQRS (Commands & Queries), DTOs, FluentValidation,  |
|   MediatR Behaviors, AutoMapper Profiles, Interfaces  |
+---------------------------+---------------------------+
                            | Depende de
+---------------------------v---------------------------+
|                 Capa de Dominio                       |
|               (FinTrack.Domain)                       |
|   Entities, Value Objects, Enums, Domain Events,      |
|   Excepciones de Negocio, Interfaces Base             |
|                                                       |
|       ⛔ CERO DEPENDENCIAS DE OTROS PROYECTOS         |
+-------------------------------------------------------+
```

### 🚨 Reglas Inquebrantables de Capas para Modelos y Agentes:

1. **`FinTrack.Domain` (Núcleo Puro):**
   * **PROHIBIDO** referenciar proyectos externos o librerías de infraestructura (como EF Core, ASP.NET Core, MediatR, Newtonsoft, etc.). Solo librerías estándar de .NET.
   * Contiene la verdad absoluta de las reglas de negocio.
   * Las violaciones de invariantes lanzan siempre excepciones específicas de dominio: `DomainException`.

2. **`FinTrack.Application` (Casos de Uso):**
   * Solo depende de `FinTrack.Domain`.
   * **PROHIBIDO** referenciar `FinTrack.Infrastructure` o `FinTrack.Web`.
   * Orquesta flujos mediante MediatR (`IRequest<Result<T>>`), valida entradas con FluentValidation, y define contratos/interfaces que Infraestructura implementará (`IRepository<T>`, `IUnitOfWork`, `ICurrencyConversionService`).
   * No debe lanzar excepciones para flujos de negocio controlados: utiliza el patrón `Result` / `Result<T>`.

3. **`FinTrack.Infrastructure` (Implementación Técnica):**
   * Depende de `FinTrack.Application` y `FinTrack.Domain`.
   * Implementa el acceso a datos (`FinTrackDbContext`, `Repository<T>`), seguridad (`ApplicationUser`, `JwtTokenService`), conversión de divisas, generación de archivos (ClosedXML, QuestPDF).

4. **`FinTrack.Web` (Host y Presentación):**
   * Depende de `FinTrack.Application` e `FinTrack.Infrastructure`.
   * Aloja la UI Blazor Server (componentes MudBlazor), los controladores API REST (`/api/...`), y el middleware de manejo global de excepciones.

---

## 3. 🧩 Modelo de Dominio y Conceptos DDD

### Entidades y Value Objects Principales

* **`Entity` (Clase Base):** Identificador `Guid Id`, marcas de tiempo `DateTime CreatedAt` y `DateTime? UpdatedAt` (siempre en `DateTime.UtcNow`), y gestión de eventos de dominio (`_domainEvents`, `AddDomainEvent`, `ClearDomainEvents`).
* **`Money` (Value Object Inmutable):**
  * Agrupa `decimal Amount` y `Currency Currency`.
  * Invariante: `Amount >= 0`.
  * Operaciones `Add(Money other)` y `Subtract(Money other)` exigen que ambas instancias compartan la misma `Currency`; de lo contrario, lanzan `DomainException`.
  * Conversión explícita: `ConvertTo(Currency targetCurrency, decimal rate)`.
* **`Account` (Aggregate Root):**
  * Controla sus `Transactions`.
  * Invariante: Solo cuentas activas (`IsActive == true`) pueden recibir transacciones.
  * `AddTransaction(...)`: Instancia la transacción, actualiza el balance de la cuenta según el `TransactionType` (`Income` suma, `Expense` resta, `Transfer` resta en la cuenta emisora) y despacha `TransactionCreatedEvent`.
* **`Transaction`:**
  * Pertenece a una cuenta. `Amount > 0`, `Description` no vacía.
  * Admite `CategoryId`, `Tags`, `IsRecurring`, y si es de tipo `Transfer`, almacena `TransferAccountId` y `RelatedTransactionId`.
* **`Category`:**
  * Jerárquica (`ParentCategoryId`, `SubCategories`).
  * Invariante: Si `IsSystem == true`, sus propiedades básicas no pueden ser alteradas o borradas.
* **`Debt` (Aggregate Root):**
  * `Creditor`, `OriginalAmount`, `RemainingAmount`, `Currency`, `DebtType` (`Owed`, `Receivable`), `Status` (`Active`, `Paid`, `Defaulted`, `Cancelled`), `DueDate`.
  * `RegisterPayment(amount, date, note)`: Disminuye `RemainingAmount`. Si llega a 0, transiciona automáticamente a `Status = DebtStatus.Paid`. Prohíbe pagos que excedan `RemainingAmount`.
* **`Goal` (Aggregate Root):**
  * Metas de ahorro. `TargetAmount`, `CurrentAmount`, `Status` (`Active`, `Completed`, `Cancelled`, `Paused`).
  * `Contribute(amount)`: Suma a `CurrentAmount`. Si alcanza `TargetAmount`, pasa a `Completed` y genera `GoalReachedEvent`.
  * `Withdraw(amount)`: Retiro controlado de fondos ahorrados.
* **`Budget`:**
  * Presupuestos mensuales/periódicos por categoría o generales. Control de `SpentAmount`, `LimitAmount`, `IsOverBudget` y mecanismo de `Rollover(...)`.
* **`ExchangeRate`:**
  * Par de divisas `FromCurrency` y `ToCurrency` (`From != To`), tasa `Rate > 0`, `Date` UTC, `RateSource` y estado activo.

### Enums Fundamentales (`FinTrack.Domain.Enums`)

* **`Currency`:** `USD = 1`, `VES = 2`, `USDT = 3`, `EUR = 4`.
* **`AccountType`:** `Bank = 1`, `Cash = 2`, `Crypto = 3`, `Wallet = 4`, `Savings = 5`.
* **`TransactionType`:** `Income = 1`, `Expense = 2`, `Transfer = 3`.
* **`CategoryType`:** `Income = 1`, `Expense = 2`, `Transfer = 3`.
* **`DebtType`:** `Owed = 1` (debo dinero), `Receivable = 2` (me deben dinero).
* **`DebtStatus`:** `Active = 1`, `Paid = 2`, `Defaulted = 3`, `Cancelled = 4`.
* **`GoalStatus`:** `Active = 1`, `Completed = 2`, `Cancelled = 3`, `Paused = 4`.
* **`RateSource`:** `BCV = 1`, `Parallel = 2`, `BinanceP2P = 3`, `Manual = 4`.

---

## 4. 💱 Reglas Críticas del Contexto Bimonetario (USD / VES)

El tratamiento de doble moneda es el núcleo funcional diferenciador de FinTrack. Todo modelo o agente que modifique transacciones o reportes debe aplicar estas reglas:

1. **Moneda de la Transacción vs Moneda de la Cuenta:**
   * Cuando se registra un movimiento en una divisa diferente a la divisa nativa de la cuenta (ej. pagar 50.000 Bs desde una cuenta en USD):
     * Debe obtenerse la tasa de cambio vigente a través de `ICurrencyConversionService`.
     * El balance de la cuenta se descuenta en su propia moneda usando la tasa convertida.
     * En el comando/entidad de transacción, se debe preservar la trazabilidad guardando `OriginalAmount`, `OriginalCurrency` y la tasa aplicada `ExchangeRateUsed`.
2. **Validación de Fondos en Gastos y Transferencias:**
   * Antes de registrar un `Expense` o `Transfer`, verificar que el monto convertido no supere el `Balance` disponible de la cuenta. Si supera, retornar `Result.Fail("Fondos insuficientes...")`.
3. **Regla de Oro en Reportes Históricos:**
   * **NUNCA** normalizar reportes históricos usando la tasa de cambio actual del día.
   * Toda transacción histórica debe normalizarse utilizando la tasa de cambio registrada en la **fecha exacta en que ocurrió la transacción** (`Transaction.Date`).
4. **Mapeo de Tasas Directas, Inversas y Puente:**
   * Si no existe una tasa directa (ej. USD -> VES), el servicio debe buscar la inversa (VES -> USD) y calcular el recíproco ($1 / \text{tasa}$).
   * Si se convierten dos monedas no base (ej. EUR -> VES), usar USD como moneda puente (EUR -> USD -> VES).

---

## 5. ⚡ Estándares de la Capa de Aplicación (CQRS & MediatR)

* **Commands (Escritura):**
  * Modifican estado en la base de datos.
  * Formato: `record CreateAccountCommand(...) : IRequest<Result<Guid>>;`
  * Nomenclatura: `[Acción][Entidad]Command` y `[Acción][Entidad]Handler`.
* **Queries (Lectura):**
  * Solo leen datos (sin efectos secundarios ni mutaciones de estado).
  * Formato: `record GetAccountByIdQuery(Guid Id) : IRequest<Result<AccountDto>>;`
  * Las listas paginadas deben devolver `PagedList<TDto>`.
* **Patrón Result:**
  * Se utiliza `Result` (sin valor) y `Result<T>` (con valor).
  * `Result.Ok(valor)` para éxito.
  * `Result.Fail("Mensaje de error")` o `Result.Fail(string[] errores)` para fallos de validación o lógica.
* **Validaciones Automáticas (Pipeline Behavior):**
  * Toda mutación (Command) debe contar con su validador FluentValidation (`AbstractValidator<TCommand>`).
  * El middleware `ValidationBehavior<TRequest, TResponse>` ejecuta automáticamente todos los validadores inyectados antes del handler. Si hay errores, detiene el pipeline y retorna `Result.Fail(errors)`.
* **AutoMapper:**
  * Mapear siempre entidades hacia DTOs en `MappingProfile.cs`. Nunca retornar entidades de dominio directamente en endpoints API o componentes UI.

---

## 6. 🗄️ Persistencia y Base de Datos (EF Core & SQL Server)

* **Precisión Numérica Financiera:**
  * En todas las configuraciones Fluent API, las propiedades de dinero deben usar `decimal` con precisión de 4 decimales:
    ```csharp
    builder.Property(x => x.Amount).HasPrecision(18, 4);
    builder.Property(x => x.Balance).HasPrecision(18, 4);
    ```
* **Conversión de Enums:**
  * Almacenar siempre enums como cadenas de texto legibles:
    ```csharp
    builder.Property(x => x.Currency).HasConversion<string>().HasMaxLength(10);
    builder.Property(x => x.Type).HasConversion<string>().HasMaxLength(50);
    ```
* **Configuraciones Desacopladas:**
  * Cada entidad debe tener su propia clase `IEntityTypeConfiguration<T>` ubicada en `FinTrack.Infrastructure.Data.Configurations`.
  * `FinTrackDbContext` las carga de forma automática mediante `modelBuilder.ApplyConfigurationsFromAssembly(...)`.
* **Prevención Activa de Queries N+1:**
  * Cargar siempre relaciones explícitas con `.Include()` / `.ThenInclude()`, o proyectar directamente a DTOs con `.Select(...)`.
* **Índices de Rendimiento Obligatorios:**
  * `Accounts`: `(Name, IsActive)`
  * `Transactions`: `(AccountId, Date DESC)`, `(CategoryId, Date)`, `(Date)`, `(Type)`
  * `Debts`: `(Status, DueDate)`
  * `ExchangeRates`: `(FromCurrency, ToCurrency, Date DESC)`

---

## 7. 🖥️ Capa de Presentación (Blazor Server & MudBlazor)

* **Componentes UI:**
  * Uso exclusivo de la librería **MudBlazor** (`MudTable`, `MudCard`, `MudButton`, `MudDialog`, `MudSnackbar`, `MudChart`).
  * Tema oscuro integrado basado en la paleta oficial (Primary: `#238636`, Secondary: `#58a6ff`, Background: `#0d1117`, Surface: `#161b22`).
* **Estado Global de la Aplicación (`AppState`):**
  * Singleton registrado en el contenedor DI que mantiene el listado de cuentas sincronizado y la tasa de cambio USD/VES activa en memoria.
  * Notifica a los componentes ante mutaciones mediante el evento `OnChange`.
* **Servicios de Consumo:**
  * Páginas Blazor consumen servicios tipados (`IAccountService`, `ITransactionService`, `IReportService`), los cuales a su vez interactúan con los controladores de API o la capa de aplicación.

---

## 8. 🔒 Seguridad y Manejo de Errores

* **Autenticación:**
  * ASP.NET Core Identity gestiona las cuentas de usuario (`ApplicationUser`).
  * Tokens JWT (`JwtTokenService`) para clientes API y `CustomAuthStateProvider` con almacenamiento seguro para Blazor.
* **Manejo Global de Excepciones:**
  * `ErrorHandlingMiddleware` captura excepciones no controladas en el pipeline HTTP:
    * `DomainException` -> Código HTTP `400 Bad Request` (`{ error: ex.Message, type: "DomainError" }`).
    * Excepciones generales -> Código HTTP `500 Internal Server Error` (`{ error: "Ocurrio un error interno...", type: "ServerError" }`).
* **Protección de Secretos:**
  * **NUNCA** colocar secretos, contraseñas de BD o `JwtSettings:SecretKey` en texto claro dentro del repositorio o `appsettings.json`. Usar `dotnet user-secrets` en desarrollo y variables de entorno en producción.

---

## 9. 📏 Convenciones de Código y Estándares de Ingeniería

### Nomenclatura C#

| Elemento | Convención | Ejemplo |
| :--- | :--- | :--- |
| Clases, Records, Structs | `PascalCase` | `AccountService`, `Money` |
| Interfaces | `PascalCase` con prefijo `I` | `IRepository<T>`, `IUnitOfWork` |
| Métodos y Propiedades Públicas | `PascalCase` (Verbo en métodos) | `GetByIdAsync()`, `RemainingAmount` |
| Campos Privados | `_camelCase` | `private readonly IMediator _mediator;` |
| Parámetros y Variables Locales | `camelCase` | `accountId`, `exchangeRate` |
| Enums | `PascalCase` (Singular) | `AccountType`, `TransactionType` |

### Formato de Commits (Conventional Commits)

Los commits deben seguir la convención formal: `<tipo>(<ámbito>): <descripción>`
* Tipos permitidos: `feat`, `fix`, `refactor`, `test`, `docs`, `style`, `chore`, `perf`.
* Ejemplos:
  * `feat(accounts): agregar validacion de balance negativo en transferencias`
  * `fix(rates): corregir calculo de tasa inversa en CurrencyConversionService`
  * `test(domain): agregar tests para la entidad Budget`

---

## 10. 🛠️ Comandos de Referencia y Verificación

Cualquier agente que realice modificaciones en el código **debe ejecutar la suite de pruebas** para garantizar que no existan regresiones:

```powershell
# Compilar la solución completa
dotnet build FinTrack.slnx

# Ejecutar todas las suites de prueba (Unitarias, Integración, UI)
dotnet test --no-restore

# Ejecutar pruebas con detalle y diagnóstico
dotnet test --verbosity normal

# Crear una nueva migración de Entity Framework
dotnet ef migrations add <NombreMigracion> --project src/FinTrack.Infrastructure --startup-project src/FinTrack.Web --output-dir Data/Migrations

# Aplicar migraciones a la base de datos
dotnet ef database update --project src/FinTrack.Infrastructure --startup-project src/FinTrack.Web
```

---

## 11. 🧭 Guía de Acción Rápida para Modelos de IA

Cuando se te solicite implementar una nueva funcionalidad o resolver un bug en FinTrack:

1. **Ubica la Capa Adecuada:**
   * ¿Es una regla de negocio o invariante? -> `FinTrack.Domain` (Entidad, Value Object o Evento).
   * ¿Es un caso de uso o validación de entrada? -> `FinTrack.Application` (Command/Query, Handler, DTO, Validator).
   * ¿Es persistencia, SQL, o llamada externa? -> `FinTrack.Infrastructure` (Configuration, Repository, Service).
   * ¿Es interfaz gráfica o endpoint REST? -> `FinTrack.Web` (Razor Page/Component o Controller).
2. **Preserva la Integridad de Dependencias:** No agregues paquetes NuGet o referencias a capas superiores desde capas inferiores.
3. **Maneja Multi-Moneda:** Recuerda registrar transacciones considerando la moneda de la cuenta vs la moneda original, y usa tasas históricas para reportes.
4. **Verifica:** Corre `dotnet test` y asegúrate de que todos los tests sigan pasando en verde.
