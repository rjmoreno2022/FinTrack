# 💰 FinTrack

**FinTrack** es una aplicación web de finanzas personales diseñada para personas que viven en economías con múltiples monedas, orientada especialmente al contexto venezolano donde coexisten el Bolívar (VES) y el Dólar Estadounidense (USD / USDT) como medios de intercambio cotidianos.

El proyecto está diseñado bajo principios de **Diseño Guiado por el Dominio (DDD)** y sigue los lineamientos de **Clean Architecture** (Arquitectura Cebolla), garantizando una base de código desacoplada, testeable y mantenible a largo plazo.

---

## 🚀 Características Principales (Alcance MVP)

- **Gestión de Cuentas:** CRUD completo de cuentas multi-moneda (Bancos nacionales, Cuentas internacionales, Efectivo, Binance/Criptomonedas).
- **Transacciones Inteligentes:** Registro de ingresos y gastos con categorización jerárquica y soporte de etiquetas.
- **Soporte de Doble Moneda (VES/USD):** Conversión automática y transparente de transacciones en base a tasas de cambio históricas y actuales.
- **Presupuestos Mensuales:** Control de límites de gasto por categorías con alertas de desviación.
- **Metas de Ahorro:** Seguimiento interactivo del progreso de objetivos financieros (ej. Fondo de Emergencia).
- **Gestión de Deudas:** Registro y amortización de deudas activas con control de pagos parciales (créditos informales, Cashea, etc.).
- **Dashboard Interactivo:** Resumen visual de la salud financiera mediante gráficos integrados con MudBlazor.

---

## 🛠️ Stack Tecnológico

| Tecnología | Versión | Propósito / Beneficio |
| :--- | :--- | :--- |
| **.NET 8 (ASP.NET Core)** | `8.0 LTS` | Runtime y framework principal con soporte de largo término (LTS). |
| **Blazor Server** | `8.0` | Frontend dinámico y reactivo escrito en C# sin dependencias de JS. |
| **Entity Framework Core** | `8.0` | ORM principal para persistencia de datos relacionales y migraciones. |
| **SQL Server** | `2022` | Base de datos relacional robusta en producción (SQLite opcional en dev). |
| **MudBlazor** | `6.x` | Set de componentes UI basados en Material Design (Tablas, Diálogos, Gráficos). |
| **MediatR** | `12.2.0` | Implementación del patrón Mediador y soporte nativo para **CQRS**. |
| **FluentValidation** | `11.8.0` | Validaciones declarativas y limpias para los comandos de aplicación. |
| **AutoMapper** | `12.0.1` | Mapeo automático y desacoplado entre entidades y DTOs. |
| **xUnit + Moq** | `2.4+` | Framework de pruebas unitarias e integración. |
| **Serilog** | `3.0+` | Registro estructurado de logs hacia múltiples destinos (Consola, Archivo, Seq). |

---

## 📐 Arquitectura del Software

FinTrack aplica **Clean Architecture** y **Domain-Driven Design (DDD)** para organizar sus componentes en capas concéntricas con dependencia hacia el dominio:

```
                  +-----------------------------------------------+
                  |                                               |
                  |              Presentation                     |
                  |          (FinTrack.Web / Blazor)              |
                  |                                               |
                  |    Pages, Components, Layouts, Services       |
                  +----------------------+------------------------+
                                         | Depends on
                  +----------------------v------------------------+
                  |                                               |
                  |              Application                      |
                  |       (FinTrack.Application)                  |
                  |                                               |
                  |   Commands, Queries, DTOs, Validators,        |
                  |   Mappings, Behaviors, Interfaces             |
                  +----------------------+------------------------+
                                         | Depends on
                  +----------------------v------------------------+
                  |                                               |
                  |              Infrastructure                   |
                  |      (FinTrack.Infrastructure)                |
                  |                                               |
                  |   DbContext, Repositories, Identity,          |
                  |   Email, External APIs, Logging               |
                  +----------------------+------------------------+
                                         | Depends on
                  +----------------------v------------------------+
                  |                                               |
                  |                  Domain                       |
                  |        (FinTrack.Domain)                      |
                  |                                               |
                  |   Entities, Value Objects, Enums,             |
                  |   Domain Events, Interfaces, Exceptions       |
                  |                                               |
                  |        NO DEPENDS ON ANY PROJECT              |
                  +-----------------------------------------------+
```

### Conceptos DDD Utilizados
- **Entidades (`Entity`):** Clases con identidad única (e.g., `Account`, `Transaction`, `Debt`, `Goal`).
- **Objetos de Valor (`ValueObject`):** Objetos inmutables comparados por su valor (e.g., `Money`, que agrupa un monto `decimal` y una moneda `Currency`).
- **Agregados y Raíces de Agregado (`Aggregate Root`):** `Account` actúa como la raíz que gestiona y valida sus `Transactions` asociadas.
- **Eventos de Dominio (`Domain Events`):** Eventos asíncronos que informan sobre cambios de estado significativos (e.g., `TransactionCreatedEvent`, `GoalReachedEvent`).
- **Patrón Resultado (`Result<T>`):** Control de flujo de negocio estructurado sin lanzar excepciones controladas en tiempo de ejecución.

---

## 📂 Estructura de la Solución

```
FinTrack/
├── FinTrack.sln                   # Archivo de solución de .NET
├── src/
│   ├── FinTrack.Domain/           # Capa del Dominio (Entidades, Value Objects, Enums)
│   ├── FinTrack.Application/      # Capa de Aplicación (CQRS: Commands/Queries, Validators, DTOs)
│   ├── FinTrack.Infrastructure/   # Capa de Infraestructura (EF Core DbContext, Repositorios, Identity, Servicios de Terceros)
│   └── FinTrack.Web/              # Capa de Presentación (Frontend Blazor Server & Controllers API)
├── tests/
│   ├── FinTrack.Domain.Tests/     # Pruebas Unitarias de Lógica de Negocio
│   ├── FinTrack.Application.Tests/# Pruebas Unitarias de Casos de Uso (Handlers y Validators)
│   └── FinTrack.Integration.Tests/# Pruebas de Integración con Base de Datos real
├── docker-compose.yml             # Despliegue local de la aplicación y SQL Server
└── Dockerfile                     # Construcción de imagen de Docker optimizada
```

---

## 💱 Arquitectura de Multi-Moneda (VES / USD)

La lógica de conversión de monedas es un pilar crítico en **FinTrack**. El sistema se basa en:
1. **`Money` Value Object:** Asegura que no se puedan mezclar ni operar de forma directa montos de distintas monedas.
2. **`CurrencyConversionService`:** Implementa la conversión y normalización basada en:
   - Tasas de cambio directas (`USD -> VES`).
   - Tasas inversas e indirectas usando `USD` como moneda puente.
   - Historial de tasas diarias para garantizar reportes históricos consistentes.
3. **`IExchangeRateProvider`:** Interfaz extensible para tasas manuales o integraciones automáticas futuras (BCV / Binance P2P API).

> [!TIP]
> **Historial de Tasas:** Siempre se utiliza la tasa de cambio de la fecha en que se registró la transacción. Esto evita que fluctuaciones cambiarias alteren los reportes financieros del pasado.

---

## ⚙️ Configuración e Instalación

### Requisitos Previos
- **.NET 8.0 SDK** o superior.
- **SQL Server Express/Developer Edition** (o Docker para levantar SQL Server en un contenedor).

### Pasos de Configuración

1. **Clonar el repositorio:**
   ```bash
   git clone https://github.com/tu-usuario/FinTrack.git
   cd FinTrack
   ```

2. **Configurar la cadena de conexión:**
   Asegúrate de configurar tu cadena de conexión en el archivo `src/FinTrack.Web/appsettings.json` o en `appsettings.Development.json`:
   ```json
   "ConnectionStrings": {
     "DefaultConnection": "Server=localhost;Database=FinTrackDb;Trusted_Connection=True;TrustServerCertificate=True;"
   }
   ```

3. **Restaurar y compilar la solución:**
   ```bash
   dotnet restore src/FinTrack.sln
   dotnet build src/FinTrack.sln
   ```

4. **Instalar la herramienta de migraciones de EF Core:**
   ```bash
   dotnet tool install --global dotnet-ef
   ```

5. **Aplicar las migraciones a la Base de Datos:**
   ```bash
   dotnet ef database update --project src/FinTrack.Infrastructure --startup-project src/FinTrack.Web
   ```

6. **Ejecutar el proyecto Frontend/Web:**
   ```bash
   dotnet run --project src/FinTrack.Web
   ```
   La aplicación se abrirá en `http://localhost:5000` (o el puerto configurado en `launchSettings.json`).

---

## 🐳 Ejecución con Docker

Puedes levantar la base de datos y la aplicación automáticamente utilizando Docker Compose:

1. **Ejecutar contenedores:**
   ```bash
   docker-compose up -d --build
   ```
2. **Detener contenedores:**
   ```bash
   docker-compose down
   ```

---

## 🧪 Pruebas Automatizadas

El proyecto cuenta con cobertura de pruebas unitarias y de integración. Para ejecutarlas:

```bash
# Ejecutar todas las suites de pruebas
dotnet test src/FinTrack.sln

# Ejecutar con detalles del resultado de pruebas
dotnet test src/FinTrack.sln --verbosity normal
```

---

## 📝 Convenciones de Desarrollo

- **Conventional Commits:** Todas las confirmaciones de código deben seguir el formato estandarizado:
  - `feat(ambito): descripción`
  - `fix(ambito): descripción`
  - `refactor(ambito): descripción`
  - `test(ambito): descripción`
- **Flujo de Trabajo (GitFlow Simplificado):**
  - `main` alberga código listo para producción.
  - `develop` para desarrollo activo.
  - `feature/nombre-de-feature` para nuevas características o componentes.
- **Clean Code:** Métodos cortos, variables descriptivas y fuerte acoplamiento a los principios **SOLID**.

---

## 🗺️ Plan de Desarrollo (Fases de Implementación)

1. **Fase 1: Fundamentos (Semana 1):** Setup de la solución, configuración inicial de EF Core y base de datos relacional.
2. **Fase 2: Dominio Core (Semana 2):** Implementación de reglas y agregados del Dominio con pruebas unitarias iniciales.
3. **Fase 3: Capa de API y CQRS (Semana 3):** Endpoints REST con handlers de MediatR, FluentValidation y AutoMapper.
4. **Fase 4: Frontend Interactivo (Semanas 4-5):** Maquetación responsiva con MudBlazor y componentes reactivos.
5. **Fase 5: Autenticación & Seguridad (Semana 6):** Setup de Identity y tokens JWT para control de accesos.
6. **Fase 6: Funcionalidades Multi-Moneda (Semanas 7-8):** Módulos avanzados de presupuestos, metas, deudas y reportes normalizados.
7. **Fase 7: Pruebas y QA (Semana 9):** Incremento de cobertura de pruebas unitarias, de integración y bUnit (UI).
8. **Fase 8: CI/CD y Deploy (Semana 10):** Dockerización, orquestación, health checks y deploy final a la nube.

---

## 🔗 Recursos Relacionados

- [Documentación Técnica Detallada de Desarrollo](file:///c:/Develops/FinTrack/Fintrack/FinTrack/FinTrack-Documentacion-Desarrollo.html)
- [Sitio Oficial de MudBlazor](https://mudblazor.com)