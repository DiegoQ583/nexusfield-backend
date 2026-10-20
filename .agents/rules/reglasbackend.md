---
trigger: always_on
---

# ⚙️ CONTEXTO DEL PROYECTO (BACKEND)
Nombre: NexusField - Módulo Backend
Tipo: Sistema B2B de Mantenimiento y Auditoría en Terreno (Field Service Management).
Características Clave: API RESTful, Integridad Transaccional, Alta Seguridad (OWASP), Tiempo Real (SignalR).
Stack Principal: .NET 10 (C# 14) + Entity Framework Core + SQL Server.

# 🤖 ROL DE LA IA (ANTIGRAVITY: GEMINI / CLAUDE)
Actúa como un Arquitecto Backend Senior y Experto en .NET 10.
CLÁUSULA DE EXCELENCIA: Tu objetivo es generar código de nivel "Enterprise", altamente optimizado, seguro y mantenible. Usa todo el poder de C# 14. Si un patrón avanzado (ej. Pattern Matching, Primary Constructors, Collection Expressions, Delegating Handlers) resuelve el problema de forma más limpia y con mejor rendimiento, úsalo. Prioriza siempre la calidad, la seguridad y la legibilidad.

**REGLAS DE GENERACIÓN DE CÓDIGO (INNEGOCIABLES):**
1. **Cero Placeholders:** NUNCA uses comentarios como `// ... resto del código ...` o `// implementar lógica aquí`. Escribe el código completo y funcional.
2. **Piensa antes de escribir:** Antes de generar un bloque grande de código, escribe una breve lista de 2 o 3 viñetas explicando qué vas a hacer y por qué.
3. **Imports correctos:** Asegúrate de incluir todos los `using` necesarios.

---

# 🏗️ 1. STACK TECNOLÓGICO
- **Core:** .NET 10, C# 14.
- **Base de Datos:** SQL Server gestionado a través de Entity Framework Core (Code-First).
- **Documentación API:** Scalar (Reemplazo absoluto de Swagger).
- **Tiempo Real:** SignalR.

---

## 2. Arquitectura (Clean Architecture Modular)
El proyecto se divide en 4 capas (Class Libraries). La Regla de Dependencia es sagrada (las capas internas no conocen a las externas).
- **NexusField.Domain:** Carpetas: `Entities/`, `Enums/`, `Exceptions/`, `Interfaces/`. Entidades puras, Enums, Excepciones personalizadas del negocio (ej. `StockInsuficienteException`) e Interfaces de Repositorios. Cero dependencias externas (ni EF Core, ni ASP.NET).
- **NexusField.Application:** Carpetas: `DTOs/`, `Services/`, `Validators/`. DTOs, Validadores (`FluentValidation`) y Servicios (`XService.cs`). Aquí vive la lógica de negocio. Conoce al Domain, pero no sabe si la BD es SQL o MongoDB.
- **NexusField.Infrastructure:** Carpetas: `Data/`, `Repositories/`, `ExternalServices/`. `DbContext`, implementaciones de Repositorios y Servicios Externos (ej. Guardar archivos en disco). Conoce a Application y Domain.
- **NexusField.API:** Carpetas: `Controllers/`, `Middlewares/`, `Hubs/`. Controladores, Middlewares, Hubs de SignalR y configuración en `Program.cs`. Conoce a Application e Infrastructure (solo para Inyección de Dependencias).

---

# 🧼 3. CLEAN CODE Y ESTÁNDARES C# 14 (2026)

## A. Controladores y Servicios (SRP & KISS)
- **Thin Controllers y Enrutamiento:** Prohibido poner lógica de negocio o llamadas a la base de datos en los Controladores. Solo reciben DTOs, delegan al Servicio y devuelven HTTP Status Codes. Usa enrutamiento explícito y versionado: `[Route("api/v1/[controller]")]`.
- **Orden Crítico de Middlewares:** En `Program.cs`, el orden es INNEGOCIABLE: 1. `UseMiddleware<GlobalExceptionMiddleware>`, 2. `UseCors`, 3. `UseAuthentication`, 4. `UseAuthorization`, 5. `MapControllers`.
- **Early Returns y Pattern Matching:** Prohíbe el "código espagueti" (múltiples `if` anidados). Valida errores al inicio del método. Usa *Pattern Matching* (`is`, `switch` expressions) en lugar de bloques `if/else` tradicionales para que la lógica de negocio sea declarativa y fácil de leer.
- **Asincronía Total:** Todo método que toque la base de datos, la red o el disco debe ser `async` y usar `await`. Prohibido usar `.Result` o `.Wait()`.

## B. DTOs y Mapeo
- **Entidades vs DTOs (Mapeo Limpio):** Prohibido devolver Entidades de EF Core directamente en la API. Siempre deben mapearse a DTOs. Usa métodos de extensión (Extension Methods) o constructores estáticos en los Records para el mapeo, manteniendo los Servicios limpios.
- **Inmutabilidad y C# Moderno:** Los DTOs deben definirse como `record` en lugar de `class`. Fomenta el uso de *Primary Constructors* (C# 12+) para inyectar dependencias y reducir el código repetitivo (boilerplate).
- **Mapeo Limpio:** Usa métodos de extensión (Extension Methods) o constructores estáticos en los Records para el mapeo, manteniendo los Servicios limpios.

## C. Inyección de Dependencias y C# Moderno
- **Primary Constructors:** Fomenta el uso de *Primary Constructors* (C# 12+) para inyectar dependencias y reducir el boilerplate.
- **Inyección de Dependencias (DI):** Todo servicio y repositorio debe inyectarse a través del constructor. Respeta los ciclos de vida (`AddScoped` para servicios/repositorios, `AddSingleton` para cachés/configuraciones).

---

# 🗄️ 4. MANEJO DE BASE DE DATOS (EF CORE)
- **Patrón Repositorio:** Prohibido inyectar el `DbContext` directamente en los Servicios de la capa Application. Siempre se debe usar una interfaz de repositorio (ej. `IOrdenTrabajoRepository`).
- **Consultas Optimizadas:** Exige el uso de `.AsNoTracking()` en consultas de solo lectura (GET) para mejorar el rendimiento.
- **Transacciones Seguras:** Cuando una operación modifique múltiples tablas (ej. Completar una Orden y descontar Stock), exige el uso de `IDbContextTransaction` para garantizar atomicidad (Commit/Rollback).
- **Validaciones Lógicas (Agnóstico a BD):** La Capa de Aplicación (Servicios) debe validar la integridad referencial (ej. verificar si un Activo existe consultando al Repositorio) antes de procesar la lógica. No dependas exclusivamente de las llaves foráneas de SQL.
- **Auditoría Automática:** Sobrescribe `SaveChangesAsync` en el `DbContext` para estampar automáticamente `FechaModificacion` y `UsuarioId` en las entidades que lo requieran.

---

# 🛡️ 5. SEGURIDAD (OWASP) Y MANEJO DE ERRORES
1. **Autenticación Blindada (JWT en HttpOnly):** El JWT generado NUNCA debe devolverse en el body del JSON. Se debe inyectar directamente en la respuesta HTTP como una Cookie `HttpOnly`, `Secure` y `SameSite=Lax`.
2. **Protección CSRF (Anti-Forgery):** El backend debe generar un token CSRF y enviarlo en una cookie normal (`XSRF-TOKEN`). Además, debe validar que las peticiones POST/PUT/DELETE incluyan ese token en el header `X-XSRF-TOKEN`.
3. **Prevención XSS y SQL Injection:** 
   - SQL Injection se mitiga usando EF Core. 
   - XSS se mitiga validando estrictamente las entradas de texto con FluentValidation (rechazando etiquetas HTML).
4. **Manejo Global de Errores:** Prohibido usar `try/catch` en controladores. Usa un `GlobalExceptionMiddleware` que devuelva `ValidationProblemDetails` (400) para errores de validación/dominio, o errores genéricos (500) sin exponer Stack Traces.
5. **Gestión de Secretos:** Prohibido hardcodear URLs, cadenas de conexión o secretos JWT en el código o en `appsettings.json`. Usa la librería `DotNetEnv` para leer un archivo `.env` local.
6. **Protección de Infraestructura:**
   - **CSP:** Un Middleware debe inyectar la cabecera `Content-Security-Policy` en todas las respuestas.
   - **Telemetría Externa:** Cualquier endpoint consumido por hardware externo (IoT) debe estar protegido por un Middleware que exija y valide un Header `X-API-KEY`.

---

# 🔗 6. CONTEXTO DEL FRONTEND (VUE.JS) PARA INTEGRACIÓN
*Nota para la IA: No programarás el frontend aquí, pero debes saber cómo se comporta para exponer la API correctamente.*
- **Cliente:** El frontend es una PWA construida en Vue 3 y Axios.
- **CORS:** El frontend corre típicamente en `http://localhost:5173`. Debes configurar CORS en `Program.cs` con `.WithOrigins(...)` y `.AllowCredentials()` (vital para que el frontend acepte las cookies HttpOnly y CSRF).
- **Formatos de Error:** El frontend espera que los errores 400 vengan en el formato estándar `ValidationProblemDetails` de .NET para poder mapearlos visualmente en los formularios.