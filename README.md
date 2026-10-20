# NexusField - Backend API

NexusField es el motor backend para un sistema B2B de Gestión de Servicios en Terreno (Field Service Management). Está diseñado bajo una arquitectura Offline-First, garantizando integridad transaccional, sincronización de datos diferida y altos estándares de seguridad para entornos con conectividad inestable.

## Arquitectura y Stack Tecnológico

El proyecto está construido utilizando Clean Architecture Modular, dividiendo las responsabilidades en cuatro capas estrictas (Domain, Application, Infrastructure, API) para garantizar un bajo acoplamiento y alta testeabilidad.

*   **Framework:** .NET 10 (C# 14)
*   **Base de Datos:** SQL Server (Entity Framework Core - Code First)
*   **Documentación:** Scalar (OpenAPI)
*   **Seguridad:** JWT en Cookies HttpOnly, BCrypt, Anti-Forgery (CSRF)

## Configuración del Entorno Local

Para levantar este proyecto en una máquina local, siga los siguientes pasos:

### 1. Requisitos Previos
*   .NET 10 SDK instalado.
*   SQL Server (2022 o superior) en ejecución.
*   Herramienta global de EF Core instalada (`dotnet tool install --global dotnet-ef`).

### 2. Variables de Entorno
El proyecto utiliza la librería `DotNetEnv` para la gestión de secretos. Cree un archivo llamado `.env` en la raíz del proyecto `NexusField.API` con la siguiente estructura:

```env
CONNECTION_STRING=Server=localhost\SQLEXPRESS;Database=NexusFieldDB;User Id=TuUsuario;Password=TuPassword;TrustServerCertificate=True;
JWT_SECRET=Clave_Secreta_Para_Firma_De_Tokens_Minimo_32_Caracteres
JWT_ISSUER=NexusFieldBackend
JWT_AUDIENCE=NexusFieldFrontend
```

### 3. Despliegue de Base de Datos
El sistema utiliza migraciones Code-First. Para generar la base de datos y aplicar el sembrado de datos (Data Seeding) inicial, ejecute el siguiente comando desde la raíz de la solución:

```bash
dotnet ef database update --project .\NexusField.Infrastructure\NexusField.Infrastructure.csproj --startup-project .\NexusField.API\NexusField.API.csproj
```

### 4. Ejecución
Para iniciar el servidor web Kestrel y exponer la API:

```bash
dotnet run --project .\NexusField.API\NexusField.API.csproj
```
La documentación interactiva estará disponible en: `https://localhost:<puerto>/scalar/v1`

---

## Módulos Implementados

### 1. Autenticación y Seguridad (Identity & Access Management)
Módulo encargado del control de acceso, protección de la API y mitigación de vectores de ataque (OWASP Top 10).
*   **Seguridad OWASP Extrema (JWT & CSRF):** El token JWT jamás se expone en el payload; se inyecta exclusivamente vía Cookie `HttpOnly`, `Secure` y `SameSite=None` (mitigando XSS). Para prevenir ataques de falsificación de peticiones (CSRF), se implementó el patrón *Double Submit Cookie* usando la interfaz `IAntiforgery` nativa de .NET, exigiendo el header `X-XSRF-TOKEN` en todo endpoint mutativo.
*   **Protección contra Fuerza Bruta (Rate Limiting):** Implementación de `IMemoryCache` en la capa de Aplicación para rastrear intentos fallidos de login. Al superar los 5 intentos, el sistema bloquea la cuenta temporalmente y devuelve un HTTP 429 (Too Many Requests), mitigando ataques de *Credential Stuffing*.
*   **Políticas de Seguridad de Contenido (CSP):** Desarrollo de un Middleware personalizado que inyecta cabeceras `Content-Security-Policy` estrictas en las respuestas HTTP, bloqueando la ejecución de scripts no autorizados en el navegador del cliente, con excepciones controladas para la interfaz de documentación (Scalar).
*   **Manejo Global de Excepciones (RFC 7807):** Uso de la interfaz moderna `IExceptionHandler` de .NET 8+ (reemplazando los middlewares tradicionales con `try/catch`). Intercepta excepciones de dominio y validación (FluentValidation) para devolver respuestas JSON estandarizadas (`ProblemDetails`), sin exponer *stack traces* sensibles.
*   **Data Seeding Determinista:** Inyección de un usuario administrador base mediante `HasData` en EF Core, utilizando un hash BCrypt pre-calculado estáticamente para evitar bucles infinitos en el motor de migraciones.

---

### 2. Centro de Comando (Dashboard Analytics)
Módulo analítico que provee métricas clave y gráficos sobre el estado financiero y operativo en terreno.
*   **Agregaciones Optimizadas (LINQ to SQL):** Ejecución de cálculos pesados (`SumAsync`, `CountAsync`, `GroupBy`) delegados enteramente al motor de la base de datos. Uso estricto de `.AsNoTracking()` para evitar la sobrecarga del *Change Tracker* de EF Core en consultas de solo lectura.
*   **Seguridad de Hilos (Thread-Safety):** Diseño secuencial de consultas asíncronas para respetar el ciclo de vida `Scoped` del `DbContext`. Esto previene colisiones de concurrencia (`InvalidOperationException`) al procesar múltiples métricas simultáneamente en la misma petición HTTP.
*   **Filtros Temporales Dinámicos:** Uso de *Pattern Matching* avanzado (Switch expressions de C# 14) en la capa de Aplicación para calcular rangos de fechas dinámicos ("hoy", "semana", "mes") y pasarlos como parámetros parametrizados a las consultas SQL.
*   **Infraestructura de Tiempo Real (SignalR):** Configuración de WebSockets mediante `NotificacionesHub`. La tubería bidireccional está orquestada para emitir eventos (`ActualizarDashboard`, `NuevaNotificacion`) directamente desde los servicios transaccionales, permitiendo que la UI reaccione instantáneamente sin necesidad de *Long Polling*.

---

### 3. Gestión de Usuarios (Personal y Roles)
Módulo dinámico para la administración del personal (Despachadores y Técnicos), garantizando la inmutabilidad de la identidad.
*   **Borrado Lógico (Soft Delete):** Implementación estricta de desactivación lógica (`Activo = false`) en lugar de eliminación física (Hard Delete). Esto garantiza que el historial de órdenes y facturas asociadas a un técnico jamás se corrompa o quede huérfano.
*   **Filtros Dinámicos:** Uso de `IQueryable` y LINQ para permitir búsquedas y filtrados dinámicos por Rol desde un único endpoint, optimizando la reutilización de código.
*   **Protección de Mutaciones (CSRF):** Todos los endpoints que alteran el estado de la base de datos (`POST`, `PUT`, `PATCH`) están blindados con el atributo `[RequireAntiforgeryToken]`, exigiendo el patrón *Double Submit Cookie* para prevenir falsificación de peticiones.
*   **Integridad de Datos:** Validación de unicidad para correos y nombres de usuario directamente en la capa de Aplicación, lanzando excepciones de dominio (`UsuarioDuplicadoException`) que el Middleware global traduce a respuestas HTTP 400 estructuradas.

---

### 4. Gestión de Órdenes de Trabajo
El núcleo transaccional del sistema, encargado de orquestar la asignación de tareas, reserva de inventario y facturación.
*   **Transacciones Atómicas (Unit of Work):** Implementación del patrón `IUnitOfWork` para garantizar el principio ACID en bases de datos relacionales. La creación de una orden y la deducción del stock reservado de los repuestos se ejecutan en una única transacción SQL (Commit/Rollback), evitando inconsistencias en el inventario si ocurre un fallo de red.
*   **Auditoría Automática (EF Core Interceptors):** Sobrescritura del método `SaveChangesAsync` en el `DbContext` para interceptar entidades que implementan el contrato `IAuditableEntity`. El sistema estampa automáticamente las fechas de creación/modificación y el ID del usuario responsable (extraído del JWT vía `IHttpContextAccessor`), manteniendo la capa de servicios completamente limpia.
*   **Prevención Estricta de XSS:** Uso de `FluentValidation` con expresiones regulares en la capa de Aplicación para rechazar (HTTP 400) cualquier intento de inyección de etiquetas HTML o scripts maliciosos en los títulos y descripciones de las órdenes.
*   **Paginación Server-Side:** Optimización de memoria y ancho de banda mediante el uso de `.Skip()` y `.Take()` en el repositorio, devolviendo únicamente los registros solicitados junto con metadatos de paginación (`TotalPages`, `CurrentPage`).
*   **Patrón Snapshot:** Almacenamiento histórico del precio de los repuestos y del cliente dueño del activo en el momento exacto de la creación de la orden. Esto blinda las facturas pasadas contra cambios futuros en los catálogos.

---

### 5. Gestión de Inventario (Repuestos y Archivos)
Módulo encargado del control de stock, valoración financiera y almacenamiento de evidencia visual de los componentes.
*   **Abstracción de Almacenamiento (I/O):** Desacoplamiento total del sistema de archivos mediante la interfaz `IFileStorageService`. La capa de Aplicación procesa peticiones `multipart/form-data` recibiendo objetos `IFormFile`, pero delega el guardado físico a la Infraestructura. Las imágenes se almacenan en `wwwroot/images` con nombres ofuscados (GUIDs) para prevenir colisiones y ataques de sobrescritura, guardando únicamente la ruta relativa en SQL Server.
*   **Propiedades Calculadas y Proyección:** Implementación de lógica de dominio pura (`StockDisponible => StockTotal - StockReservado`). Este valor se calcula en tiempo de ejecución y se proyecta directamente en los DTOs de respuesta, evitando la persistencia de datos redundantes en la base de datos y garantizando una única fuente de la verdad.
*   **Borrado Inteligente e Idempotencia:** Algoritmo de eliminación que evalúa la integridad referencial antes de actuar. Si el repuesto tiene historial transaccional en la tabla puente `OrdenRepuesto`, el sistema aplica un Soft Delete (devolviendo un HTTP 409 Conflict controlado). Si es un registro nuevo sin uso, ejecuta un Hard Delete en SQL y purga simultáneamente el archivo físico del disco duro para optimizar almacenamiento.
*   **Bloqueo de Concurrencia Lógica:** Reglas de negocio estrictas que impiden la desactivación o eliminación de un repuesto si su `StockReservado` es mayor a cero, protegiendo la cadena de suministro y evitando que los técnicos en terreno se queden con inventario fantasma.

---

### 6. Gestión de Activos (Maquinaria/Equipos)
Módulo para la administración del parque de equipos físicos, ubicaciones y clientes a los que se les realiza mantenimiento.
*   **Resolución de Colisiones (Fluent API):** Aplicación de técnicas avanzadas de mapeo ORM. Para cumplir con las reglas de C# (una propiedad no puede llamarse igual que su clase), se implementó la propiedad `Activo_` para el Soft Delete. Mediante Fluent API en el `DbContext`, esta propiedad se mapea explícitamente a la columna `Activo` en SQL Server, manteniendo la pureza del código sin sacrificar el estándar de la base de datos.
*   **Consultas Ultraligeras (Combobox Optimization):** Desarrollo de endpoints de búsqueda (`/api/v1/activos/buscar`) diseñados específicamente para autocompletado en tiempo real en el Frontend. Utilizan LINQ dinámico para buscar por nombre o ubicación, proyectan DTOs anémicos (solo `Id` y `Nombre`), aplican `.AsNoTracking()` y limitan la respuesta con `.Take(10)`, logrando latencias de un solo dígito.
*   **Aislamiento Histórico:** La actualización de datos maestros de un activo (ej. cambio de dueño en `ClienteActual`) muta el registro actual, pero la arquitectura garantiza que las órdenes de trabajo cerradas conserven el contexto histórico exacto gracias al patrón Snapshot implementado en el Módulo de Órdenes.
*   **Blindaje Relacional:** Interceptores lógicos en la capa de Aplicación que verifican el estado de vida del activo, rechazando inmediatamente cualquier intento de asignar nuevas órdenes de trabajo a maquinaria que ha sido dada de baja lógicamente.

---

## Módulos del Sistema: Perfil Técnico

---

### 1. Autenticación y Seguridad (Identity & Access Management)
Módulo encargado del control de acceso, protección de la API y mitigación de vectores de ataque (OWASP Top 10), optimizado para arquitecturas desacopladas y túneles de desarrollo.
*   **Seguridad OWASP Extrema (JWT & CSRF):** El token JWT jamás se expone en el payload; se inyecta exclusivamente vía Cookie `HttpOnly`. Para prevenir ataques de falsificación de peticiones (CSRF), se implementó el patrón *Double Submit Cookie* usando la interfaz `IAntiforgery` nativa de .NET, exigiendo el header `X-XSRF-TOKEN` en todo endpoint mutativo.
*   **Adaptación Cross-Origin (Vercel & Cloudflare Tunnels):** Para soportar el despliegue del frontend en Vercel mientras el backend opera detrás de un túnel HTTPS, se configuraron políticas de cookies estrictas (`SameSite=None` y `Secure=true`). Además, se implementó un flujo de entrega de token CSRF vía JSON en el Login, garantizando que el cliente HTTP (Axios) pueda inyectar el header de seguridad sin ser bloqueado por las políticas de privacidad de navegadores modernos (Safari/Chrome).
*   **Protección contra Fuerza Bruta (Rate Limiting):** Implementación de `IMemoryCache` en la capa de Aplicación para rastrear intentos fallidos de login. Al superar los 5 intentos, el sistema bloquea la cuenta temporalmente y devuelve un HTTP 429 (Too Many Requests), mitigando ataques de *Credential Stuffing*.
*   **Políticas de Seguridad de Contenido (CSP):** Desarrollo de un Middleware personalizado que inyecta cabeceras `Content-Security-Policy` estrictas en las respuestas HTTP, bloqueando la ejecución de scripts no autorizados en el navegador del cliente, con excepciones controladas para la interfaz de documentación (Scalar).
*   **Manejo Global de Excepciones (RFC 7807):** Uso de la interfaz moderna `IExceptionHandler` de .NET 8+ (reemplazando los middlewares tradicionales con `try/catch`). Intercepta excepciones de dominio y validación (FluentValidation) para devolver respuestas JSON estandarizadas (`ProblemDetails`), sin exponer *stack traces* sensibles.
*   **Data Seeding Determinista:** Inyección de un usuario administrador base mediante `HasData` en EF Core, utilizando un hash BCrypt pre-calculado estáticamente para evitar bucles infinitos en el motor de migraciones.

---

### 7. Centro de Trabajo (Perfil Técnico - Mobile First)
Módulo diseñado específicamente para el consumo desde dispositivos móviles (PWA), optimizado para conexiones inestables y uso con una sola mano.
*   **Seguridad Cero-Confianza (Prevención BOLA):** Los endpoints del técnico (`/mis-ordenes`, `/iniciar`, `/completar`) jamás reciben el ID del técnico por la URL o el Body. El backend extrae criptográficamente la identidad del usuario directamente desde los *Claims* del token JWT mediante el `ICurrentUserService`. Esto previene vulnerabilidades de tipo *Broken Object Level Authorization*, garantizando que un técnico no pueda ver ni alterar el trabajo de sus compañeros.
*   **Carga Útil Optimizada (BFF Pattern):** Implementación del patrón *Backend For Frontend*. Se crearon DTOs específicos (`OrdenTecnicoDto`, `CatalogoOfflineDto`) que proyectan únicamente los datos estrictamente necesarios para la pantalla del celular, excluyendo información financiera o descripciones largas para ahorrar ancho de banda y memoria en el dispositivo (IndexedDB).
*   **Máquina de Estados Estricta:** Lógica de negocio que valida transiciones de estado legales (ej. de `Pendiente` a `EnProgreso`, o de `EnProgreso` a `Pausada`). Cualquier intento de transición ilegal lanza una `InvalidOperationException` que el middleware traduce a un HTTP 400.
*   **Notificaciones Push (SignalR):** Al iniciar, pausar o rechazar una orden desde el terreno, el controlador emite eventos en tiempo real hacia el `NotificacionesHub`, actualizando instantáneamente el Dashboard de los despachadores en la oficina sin necesidad de *Long Polling*.

---

### 8. Ejecución de Orden y Reconciliación (Motor Transaccional)
El núcleo financiero y logístico del trabajo en terreno. Procesa la finalización de las órdenes, el cálculo de costos y la captura de evidencias físicas.
*   **Reconciliación de Inventario (Rollback Lógico):** Algoritmo transaccional complejo que, al completar una orden, primero "libera" el stock reservado previamente por el despachador, y luego descuenta del `StockTotal` únicamente las cantidades que el técnico declara haber usado realmente. Esto garantiza una precisión absoluta en el inventario físico.
*   **Búsqueda en Memoria O(1):** Para evitar el anti-patrón de *N+1 Queries* al procesar múltiples repuestos usados, el sistema ejecuta una única consulta SQL con la cláusula `IN` (`.Where(r => ids.Contains(r.Id))`), carga los repuestos en memoria y los transforma en un Diccionario (`ToDictionary`). Esto permite validar el stock disponible a velocidad constante `O(1)`.
*   **Cálculo Financiero Dinámico:** El backend asume la responsabilidad total de la facturación. Reconstruye el `CostoTotal` multiplicando la cantidad de repuestos por su precio histórico, y sumando el costo de la mano de obra (`HorasTrabajadas * TarifaPorHora` del técnico), evitando cualquier manipulación de precios desde el cliente web.
*   **Procesamiento de Evidencias (Base64 a Binario):** Expansión del `IFileStorageService` para soportar la decodificación de strings en Base64 provenientes de los elementos `<canvas>` de la PWA (firmas digitales y fotografías comprimidas). El backend limpia los prefijos MIME, convierte la data a arreglos de bytes y persiste los archivos físicos en el servidor, guardando únicamente las URLs relativas en SQL Server.

---

# Guía para Revisión Cruzada

Esta sección está diseñada con un enfoque "Zero-Friction" para facilitar la evaluación del proyecto. Sigue estos pasos para clonar, configurar y auditar la API en menos de 5 minutos.

## Fase 1: Preparación del Entorno
1. **Clonar el repositorio:**
   ```bash
   git clone <url-del-repositorio>
   ```
2. **Requisitos:** Asegúrate de tener instalado **.NET 10 SDK** y **SQL Server** (Express o Developer) en ejecución.
3. **Configuración de Secretos:** Crea un archivo llamado `.env` en la raíz de la carpeta `NexusField.API`. Pega la siguiente configuración (ajusta `User Id` y `Password` según tu instancia local de SQL Server):
   ```env
   CONNECTION_STRING=Server=localhost\SQLEXPRESS;Database=NexusFieldDB;User Id=TuUsuario;Password=TuPassword;TrustServerCertificate=True;
   JWT_SECRET=NexusField_Super_Secret_Key_2026_Para_Firma_Digital_Segura
   JWT_ISSUER=NexusFieldBackend
   JWT_AUDIENCE=NexusFieldFrontend
   ```

## Fase 2: Creación de Base de Datos y Data Seeding
El proyecto utiliza el enfoque Code-First de Entity Framework Core. No es necesario crear la base de datos manualmente en SQL Server Management Studio.
1. Asegúrate de que el servicio de SQL Server esté en ejecución en tu máquina.
2. Abre una terminal en la raíz de la solución y ejecuta el siguiente comando. EF Core se encargará de crear la base de datos física local, construir todas las tablas e inyectar automáticamente los datos de prueba (incluyendo el usuario administrador):
```bash
dotnet ef database update --project .\NexusField.Infrastructure\NexusField.Infrastructure.csproj --startup-project .\NexusField.API\NexusField.API.csproj
```

## Fase 3: Ejecución y Documentación (Scalar)
1. Levanta la API ejecutando en la terminal:
```bash
dotnet run --project .\NexusField.API\NexusField.API.csproj
```
2. **Documentación Interactiva:** Una vez en ejecución, explora y prueba todos los endpoints navegando a:
`https://localhost:7165/scalar/v1`

Para probar los endpoints protegidos en Scalar sin ser bloqueado por las políticas de seguridad, sigue este orden estricto:
1. **Obtener Pase de Seguridad:** Ejecuta `GET /api/v1/auth/csrf` y copia el valor del token devuelto en el JSON.
2. **Autenticación:** Ve a `POST /api/v1/auth/login`. En la sección *Headers*, agrega `X-XSRF-TOKEN` y pega el token. En el *Body*, usa las credenciales del Data Seeding:
   - **Despachador:** Usuario: `DiegoAdmin` | Clave: `Nexus2026!`
   - **Técnico:** Usuario: `Gilmanito` | Clave: `Tecnico2026!`
3. **Probar Endpoints:** El token JWT se inyectará automáticamente en tu navegador como una cookie `HttpOnly`. Para cualquier petición `POST`, `PUT` o `PATCH` (ej. Crear Orden), recuerda siempre incluir el Header `X-XSRF-TOKEN`.

## Fase 4: Exponer la API a Internet (Dev Tunnels)
*Paso exclusivo si deseas conectar con el Frontend (PWA desplegada en Vercel) o un dispositivo móvil físico a esta API local.*

1. En Visual Studio, haz clic en la flecha desplegable junto al botón verde de **Iniciar (Play)**.
2. Ve a **Túneles de desarrollo (Dev Tunnels)** -> **Crear un túnel...**
3. Configúralo con los siguientes parámetros:
   - **Nombre:** `nexusfield-api`
   - **Tipo de túnel:** Persistente
   - **Acceso:** **Público** *(Obligatorio para evitar errores de CORS con el frontend)*.
4. Asegúrate de que el túnel esté seleccionado (con un check ✓) en el menú desplegable.
5. Inicia el proyecto (Play) para que el túnel se registre y luego detenlo.
6. Haz clic nuevamente en la flecha desplegable de **Iniciar (Play)** -> **Túneles de desarrollo** -> **Mostrar ventana de túneles de desarrollo**.
7. En la nueva ventana, localiza la fila con el túnel que acabas de crear (`nexusfield-api`).
8. En la columna **Dirección URL del túnel**, haz clic en el enlace que dice **Vista**.
9. Se abrirá una pestaña detallada. En la columna **Puertos**, haz clic sobre el puerto seguro de la API (usualmente el `7165`).
10. Esto abrirá una ventana en el navegador. Copia la URL completa de la barra de direcciones (ej. `https://abcd-1234.inc1.devtunnels.ms`). copiamos ese link completo y lo guardamos ya que necesitaremos eso para conectar nuestra app de frontend con la api.

## Puntos Clave para la Evaluación Arquitectónica (Code Clarity)
Se invita al evaluador a auditar los siguientes archivos para comprobar la aplicación de patrones de diseño Senior:
*   **Transacciones ACID:** `OrdenTrabajoService.cs` (Capa Application). Revisa la implementación del patrón `IUnitOfWork` y la reconciliación matemática de inventario en memoria `O(1)`.
*   **Seguridad y Middlewares:** `Program.cs` y `GlobalExceptionHandler.cs` (Capa API). Revisa la configuración de CSRF, Cookies SameSite=None y el manejo de excepciones bajo el estándar RFC 7807.
*   **Auditoría Automática:** `NexusDbContext.cs` (Capa Infrastructure). Revisa cómo el `ChangeTracker` intercepta las entidades `IAuditableEntity` para estampar fechas y usuarios automáticamente sin ensuciar los servicios.

---

## Licencia y Derechos

Desarrollado por **Diego Quintero** para el Proyecto Final de Capacitación .NET C# y Vue.js.  
NexusField © 2026 — Todos los derechos reservados.