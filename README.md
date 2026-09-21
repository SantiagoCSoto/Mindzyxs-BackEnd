# Mindzyxs-API — Guía Back-End (PB-01, PB-02, PB-03, PB-04, PB-05, PB-06, PB-07, PB-08, PB-09, PB-27)

API REST `Mindzyxs 1.1` — solo back-end. Front en `Mindzyxs-FrontEnd` (`develop`) nunca toca BD. Todo pasa por `Controllers/*` → `Services/*` → `Repositories/*` (EF Core a `SANTI/Mindzyxs`).

## 0. Estructura del proyecto

```
Mindzyxs-API/
├── Program.cs                     # Punto de entrada: DI, CORS, EF Core, mapeo de controladores
├── appsettings.json               # Config de producción: ConnectionStrings, Smtp, Storage, Registro, Sesion, Admin
├── appsettings.Development.json   # Overrides de desarrollo
├── Mindzyxs-API.csproj            # net9.0, EF Core SqlServer, OpenApi
├── Mindzyxs-API.http              # Peticiones HTTP de prueba
├── Properties/
│   └── launchSettings.json        # Perfiles de arranque (http en 5015)
│
├── Controllers/                   # Capa HTTP: reciben peticiones y arman respuestas
│   ├── AccountController.cs       # Registro, verificación, login, sesión, recuperación de contraseña
│   ├── AvisoController.cs         # PB-04: mensaje aclaratorio (no diagnóstico clínico) y su log
│   ├── AdminController.cs         # PB-27: respaldos (.bak), descarga y mantenimiento
│   ├── InvitadoController.cs      # PB-06: modo invitado (acceso, panel, bloqueo de exclusivas, salida)
│   ├── CuestionarioController.cs    # PB-07: cuestionario del invitado (pendiente, inicio, finalizar al 100%)
│   ├── RiesgoController.cs          # PB-08: cálculo del riesgo emocional del invitado
│   ├── AyudaController.cs           # PB-08: líneas de ayuda activas
│   ├── RecomendacionController.cs   # PB-09: recomendaciones básicas (lista priorizada, detalle)
│
├── Services/                      # Lógica de negocio (no conoce HTTP)
│   ├── AuthService.cs             # Registro de usuario y envío de código de verificación
│   ├── VerificationService.cs     # Verificación de correo (activa cuenta / pendiente aprobación)
│   ├── LoginService.cs            # Login, creación/validación de sesiones y logout
│   ├── PasswordRecoveryService.cs # Flujo forgot/verify/reset/resend
│   ├── CatalogService.cs          # Catálogos (tipos de cuenta, géneros)
│   ├── AvisoService.cs            # PB-04: contenido del mensaje aclaratorio y log de visualización
│   ├── InvitadoService.cs         # PB-06: panel de invitado y registro de acceso/salida/bloqueos
│   ├── CuestionarioService.cs       # PB-07: cuestionario pendiente, validación 100% y puntaje/nivel
│   ├── RiesgoService.cs             # PB-08: cálculo de riesgo y líneas de ayuda activas
│   ├── RecomendacionService.cs      # PB-09: lista priorizada por nivel y detalle con pasos
│   └── AdminService.cs            # Respaldos nativos SQL (.bak), listado, descarga y mantenimiento
│
├── Repositories/                  # Acceso a datos (EF Core)
│   ├── Interfaces.cs              # Contratos de repositorio + records (RespaldoDto, SesionDto, etc.)
│   ├── EfRepositories.cs          # Implementaciones EF Core de cada interfaz
│   └── Models/
│       ├── MindzyxsDbContext.cs   # DbContext y mapeo de tablas dbo.*
│       └── Rows.cs                # Entidades de tabla (UsuarioRow, CodigoRow, RespaldoRow, ...)
│
├── Dtos/                          # Objetos de transferencia
│   ├── AuthVm.cs                  # RegisterVm, LoginVm, VerifyCodeVm, ForgotVm, ResetPasswordVm
│   ├── AvisoDto.cs                # PB-04: AvisoDto (contenido) y VisualizacionAvisoVm (log)
│   ├── CuestionarioDto.cs           # PB-07: catálogo, inicio/finalizar y resultado con puntaje/nivel
│   ├── RiesgoDto.cs                 # PB-08: cálculo de riesgo, alerta y líneas de ayuda
│   ├── RecomendacionDto.cs          # PB-09: lista priorizada y detalle con pasos/duración
│   ├── AdminDto.cs                # DTOs de administración / respaldos
│   ├── CatalogDto.cs              # Catálogos (tipos de cuenta, géneros)
│   ├── UserDto.cs                 # Datos de usuario expuestos por la API
│   └── OperationResult.cs         # Resultado genérico de operaciones de servicio
│
└── Utilities/                     # Helpers transversales
    ├── CodeHelper.cs              # Generación del código de 6 dígitos
    ├── Constants.cs               # Constantes compartidas (acciones de auth, etc.)
    ├── ContactHelper.cs           # Validación de tipo de cuenta y contacto
    ├── EmailHelper.cs             # Envío SMTP
    ├── EmailTemplate.cs           # Plantillas HTML de los correos
    ├── FileHelper.cs              # Validación PDF ≤5MB y almacenamiento en disco
    ├── SecurityHelper.cs          # Hashing PBKDF2 y política de contraseñas
    └── TokenHelper.cs             # Generación/hashing de tokens de sesión
```

Dependencias principales registradas en `Program.cs`: repositorios EF (`IUserRepository`, `ICatalogRepository`, `ICodigoRepository`, `ISesionRepository`, `IAuthLogRepository`, `IAdminRepository`, `ICuestionarioRepository`, `ILineaAyudaRepository`, `IRecomendacionRepository`), almacenamiento (`ITarjetaStorage`), correo (`IEmailSender`) y servicios de negocio.

## Swagger (documentación interactiva)

Swagger UI siempre disponible al levantar la API:

* **UI interactiva:** `http://localhost:5015/swagger`
* **Spec OpenAPI JSON:** `http://localhost:5015/swagger/v1/swagger.json`

Cada endpoint documenta las HUs que cubre, los mensajes exactos de error y los códigos de respuesta (200/201/400/401/403/409/410/500) con sus esquemas de `request`/`response`.

**Probar un endpoint autenticado desde Swagger:**

1. `POST /api/account/login` con las credenciales (ej. admin real de prueba `sankie453@gmail.com` / `abc123`).
2. Copia el `token` de la respuesta.
3. Click en **Authorize** (botón arriba a la derecha) y pega el token como `Bearer <token>` (el prefijo `Bearer ` se añade automáticamente por el esquema).
4. Ejecuta el endpoint protegido (p.ej. `GET /api/account/me`, o `POST /api/admin/backup` para PB-27).

> Los endpoints de `AdminController` (`/api/admin/*`) requieren una sesión con rol **Administrador**.

## 1. Levantar el API

```bash
dotnet run --urls "http://localhost:5015"
# Properties/launchSettings.json: http 5015
# Program.cs: CORS AllowAnyOrigin/Headers/Methods, HttpsRedirection desactivado en dev
```
Base URL dev: `http://localhost:5015`

## 2. Endpoints PB-01

| Método | Ruta | HU | Descripción | Auth |
|--------|------|----|-------------|------|
| `GET` | `/api/account/catalogos` | HU1+HU2 | Tipos de cuenta + géneros | no |
| `POST` | `/api/account/register` | HU1-3 | Registro `multipart/form-data` | no |
| `POST` | `/api/account/verify` | HU4 | Verificar código | no |
| `POST` | `/api/account/resend` | HU4 | Reenviar código | no |

Todos devuelven `400 {error:"..."}` en validación (`Program.cs:10`).

### 2.1 `GET /api/account/catalogos` — HU1 ComboBox + HU2 Género

```bash
curl http://localhost:5015/api/account/catalogos
# -> 200 {"tiposCuenta":[{"id":1,"codigo":"Paciente"},{"id":2,"codigo":"Psicologo"}],"generos":[{"id":1,"codigo":"Masculino"},...]}
```

Usa `tiposCuenta` para el `ComboBox Tipo de usuario` (solo `Paciente`/`Psicologo`) y `generos` para el `select Género`. `requiereTarjeta = tipo==="Psicologo"` (`Repositories/EfRepositories.cs:EfCatalogRepository`).

### 2.2 `POST /api/account/register` — HU1 + HU2 + HU3 `multipart/form-data`

Campos exactos `Dtos/AuthVm.cs:RegisterVm`:

| Campo | Tipo | Regla HU |
|-------|------|----------|
| `TipoUsuario` | `Paciente` \| `Psicologo` | HU1 → `400 "Debes seleccionar un tipo de cuenta: Paciente o Psicólogo"` (`Utilities/ContactHelper.cs`) |
| `Nombre`, `Apellido` | string | HU2 → `400 "Este campo es obligatorio"` |
| `FechaNacimiento` | string `yyyy-MM-dd` | → `400 "Ingresa una fecha de nacimiento válida"` / `400 "La fecha de nacimiento no puede ser en el futuro"` / `400 "Debes tener al menos 14 años para registrarte"` (validación manual en `Services/AuthService.cs:RegisterAsync`) |
| `IdGenero` | int (de catalogos) | → `400 "Este campo es obligatorio"` |
| `Correo` | string | → `400 "Este campo es obligatorio"` / `400 "Ingresa un correo válido"` / `409 "El correo ya se encuentra registrado"` |
| `Password` | string | HU2 → `400 "La contraseña debe tener mínimo 6 caracteres e incluir al menos una letra y un número"` (`Utilities/SecurityHelper.cs:PasswordHelper`) |
| `TarjetaProfesional` | `File` PDF ≤5MB | HU3 solo `Psicologo` → `400 "La tarjeta profesional es obligatoria para registrarse como psicólogo"` / `"El archivo debe estar en formato PDF"` / `"El archivo supera el tamaño máximo permitido de 5MB"` (`Utilities/FileHelper.cs` verifica `%PDF`, guarda en `Storage:TarjetasPath` + `dbo.usuarios.file_path`) |

**JS front (fetch + FormData):**

```js
const fd = new FormData();
fd.append("TipoUsuario", tipo); // "Paciente" | "Psicologo"
fd.append("Nombre", nombres);
fd.append("Apellido", apellidos);
fd.append("FechaNacimiento", fechaNacimiento); // "yyyy-MM-dd"
fd.append("IdGenero", generoId);
fd.append("Correo", correo);
fd.append("Password", password);
if (tipo === "Psicologo") fd.append("TarjetaProfesional", fileInput.files[0]);

const res = await fetch("http://localhost:5015/api/account/register", { method:"POST", body: fd });
const data = await res.json();
if (!res.ok) showError(data.error); // 400/409
else {
  // 201 {idUsuario, mensaje:"Registro exitoso. Verifica tu cuenta."}
  location.href = `/verify?correo=${encodeURIComponent(correo)}`;
}
```

`Services/AuthService.cs:RegisterAsync` crea `dbo.usuarios` en `PendienteVerificacion` (`PendienteAprobacion` si Psicologo), guarda `file_path`, genera código 6 dígitos `Utilities/CodeHelper.cs:NewCode6()` expira 15 min (`appsettings.json:Registro:CodigoExpiraMinutos`), mail `Utilities/EmailTemplate.cs:VerificationCode`.

**cURL Paciente / Psicólogo:**

```bash
curl --location "http://localhost:5015/api/account/register" -F "TipoUsuario=Paciente" -F "Nombre=Camila" -F "Apellido=Rojas" -F "FechaNacimiento=2004-05-10" -F "IdGenero=2" -F "Correo=paciente.nuevo@test.com" -F "Password=Abc12345"

curl --location "http://localhost:5015/api/account/register" -F "TipoUsuario=Psicologo" -F "Nombre=Pedro" -F "Apellido=Lopez" -F "FechaNacimiento=1991-03-22" -F "IdGenero=1" -F "Correo=psy@test.com" -F "Password=Abc12345" -F "TarjetaProfesional=@C:/ruta/tarjeta.pdf;type=application/pdf"
```

### 2.3 `POST /api/account/verify` + `resend` — HU4

```js
// verify
await fetch("http://localhost:5015/api/account/verify", {
  method:"POST", headers:{"Content-Type":"application/json"},
  body: JSON.stringify({ correo, codigo })
});
// 200 {estado:"Activa", mensaje:"Registro exitoso. Ya puedes iniciar sesión"} // Paciente
// 200 {estado:"PendienteAprobacion", mensaje:"Registro exitoso. Tu cuenta está pendiente de aprobación por el administrador"} // Psicologo
// 400 {error:"Código incorrecto. Verifica e intenta nuevamente"}
// 410 {error:"El código expiró. Reenvía el código"}

// resend
await fetch("http://localhost:5015/api/account/resend", {
  method:"POST", headers:{"Content-Type":"application/json"},
  body: JSON.stringify({ correo })
});
// 200 {mensaje:"Código reenviado"}
```

`Services/VerificationService.cs` usa `dbo.codigos_verificacion` (`Repositories/Models/Rows.cs:CodigoRow`), consume código, cambia `id_estado_cuenta` a `Activa` o `PendienteAprobacion`.

**cURL:**

```bash
curl --location "http://localhost:5015/api/account/verify" -H "Content-Type: application/json" -d "{\"correo\":\"paciente.nuevo@test.com\",\"codigo\":\"123456\"}"
curl --location "http://localhost:5015/api/account/resend" -H "Content-Type: application/json" -d "{\"correo\":\"paciente.nuevo@test.com\"}"
```

## 3. Mensajes exactos HU (mostrar tal cual bajo el campo)

* `"Debes seleccionar un tipo de cuenta: Paciente o Psicólogo"`
* `"Este campo es obligatorio"`
* `"Ingresa una fecha de nacimiento válida"` / `"La fecha de nacimiento no puede ser en el futuro"` / `"Debes tener al menos 14 años para registrarte"`
* `"Ingresa un correo válido"`
* `"La contraseña debe tener mínimo 6 caracteres e incluir al menos una letra y un número"`
* `"El correo ya se encuentra registrado"` (409)
* `"La tarjeta profesional es obligatoria para registrarse como psicólogo"` / `"El archivo debe estar en formato PDF"` / `"El archivo supera el tamaño máximo permitido de 5MB"`
* `"Código incorrecto. Verifica e intenta nuevamente"` / `"El código expiró. Reenvía el código"` / `"Código reenviado"`

## 4. Postman

* `GET /catalogos` → no auth.
* `POST /register` → `Body → form-data` (no `raw`), keys exactas `TipoUsuario` etc., `TarjetaProfesional` tipo `File`.
* `POST /verify` y `resend` → `Body → raw → JSON`.

## 5. Notas front

* El front no hace `Cancelar/Volver` en API — es solo modal `¿Deseas cancelar el registro?`.
* `FechaNacimiento` (string `yyyy-MM-dd`) se guarda directo en `dbo.usuarios.fecha_nacimiento`, sin cálculo de edad. Validaciones: obligatoria, no futura y ≥14 años.
* Tarjeta solo en `file_path` (disco `C:\uploads\mindzyxs\tarjetas`), no blob.
* Rama actual `PB1`, BD `SANTI/Mindzyxs` usuario `mindzyxs_app` (`db_datareader/db_datawriter`), `appsettings.json:ConnectionStrings:Mindzyxs`.

# Mindzyxs-API — Guía Front-End PB-02 (Autenticar usuario)

API REST `Mindzyxs 1.1` — solo back-end. Front en `Mindzyxs-FrontEnd` (`develop`) nunca toca BD. Todo pasa por `Controllers/AccountController.cs` → `Services/*` → `Repositories/*` (EF Core a `SANTI/Mindzyxs`).

## 1. Levantar el API

```bash
dotnet run --urls "http://localhost:5015"
# Properties/launchSettings.json: http 5015
# Program.cs: CORS AllowAnyOrigin/Headers/Methods, HttpsRedirection desactivado en dev
```
Base URL dev: `http://localhost:5015`

## 2. Endpoints

| Método | Ruta | HU | Descripción | Auth |
|--------|------|----|-------------|------|
| `GET` | `/api/account/catalogos` | HU1+HU2 PB1 | Tipos de cuenta + géneros | no |
| `POST` | `/api/account/register` | HU1-3 PB1 | Registro `multipart/form-data` | no |
| `POST` | `/api/account/verify` | HU4 PB1 | Verificar código | no |
| `POST` | `/api/account/resend` | HU4 PB1 | Reenviar código | no |
| `POST` | `/api/account/login` | HU1-2 PB2 | Login, bloqueos, crea sesión | no |
| `GET` | `/api/account/me` | HU2 PB2 | Valida sesión / expiración | Bearer |
| `POST` | `/api/account/logout` | HU1-PB3 | Cerrar sesión: invalida token y loguea | Bearer |
| `GET` | `/api/account/echo` | debug | Echo `Authorization` recibido (Postman) | no |

Todos devuelven `400 {error:"..."}` en validación (`Program.cs:InvalidModelStateResponseFactory`).

### 2.1 `GET /api/account/catalogos` — HU1 ComboBox + HU2 Género

```bash
curl http://localhost:5015/api/account/catalogos
# -> 200 {"tiposCuenta":[{"id":1,"codigo":"Paciente"},{"id":2,"codigo":"Psicologo"}],"generos":[{"id":1,"codigo":"Masculino"},...]}
```

Usa `tiposCuenta` para el `ComboBox Tipo de usuario` (solo `Paciente`/`Psicologo`) y `generos` para el `select Género`. `requiereTarjeta = tipo==="Psicologo"` (`Repositories/EfRepositories.cs:EfCatalogRepository`).

### 2.2 `POST /api/account/register` — HU1 + HU2 + HU3 `multipart/form-data`

Campos exactos `Dtos/AuthVm.cs:RegisterVm`:

| Campo | Tipo | Regla HU |
|-------|------|----------|
| `TipoUsuario` | `Paciente` \| `Psicologo` | HU1 → `400 "Debes seleccionar un tipo de cuenta: Paciente o Psicólogo"` (`Utilities/ContactHelper.cs`) |
| `Nombre`, `Apellido` | string | HU2 → `400 "Este campo es obligatorio"` |
| `FechaNacimiento` | string `yyyy-MM-dd` | → `400 "Ingresa una fecha de nacimiento válida"` / `400 "La fecha de nacimiento no puede ser en el futuro"` / `400 "Debes tener al menos 14 años para registrarte"` (validación manual en `Services/AuthService.cs:RegisterAsync`) |
| `IdGenero` | int (de catalogos) | → `400 "Este campo es obligatorio"` |
| `Correo` | string | → `400 "Este campo es obligatorio"` / `400 "Ingresa un correo válido"` / `409 "El correo ya se encuentra registrado"` |
| `Password` | string | HU2 → `400 "La contraseña debe tener mínimo 6 caracteres e incluir al menos una letra y un número"` (`Utilities/SecurityHelper.cs:PasswordHelper`) |
| `TarjetaProfesional` | `File` PDF ≤5MB | HU3 solo `Psicologo` → `400 "La tarjeta profesional es obligatoria para registrarse como psicólogo"` / `"El archivo debe estar en formato PDF"` / `"El archivo supera el tamaño máximo permitido de 5MB"` (`Utilities/FileHelper.cs` verifica `%PDF`, guarda en `Storage:TarjetasPath` + `dbo.usuarios.file_path`) |

**JS front (fetch + FormData):**

```js
const fd = new FormData();
fd.append("TipoUsuario", tipo); // "Paciente" | "Psicologo"
fd.append("Nombre", nombres);
fd.append("Apellido", apellidos);
fd.append("FechaNacimiento", fechaNacimiento); // "yyyy-MM-dd"
fd.append("IdGenero", generoId);
fd.append("Correo", correo);
fd.append("Password", password);
if (tipo === "Psicologo") fd.append("TarjetaProfesional", fileInput.files[0]);

const res = await fetch("http://localhost:5015/api/account/register", { method:"POST", body: fd });
const data = await res.json();
if (!res.ok) showError(data.error); // 400/409
else {
  // 201 {idUsuario, mensaje:"Registro exitoso. Verifica tu cuenta."}
  location.href = `/verify?correo=${encodeURIComponent(correo)}`;
}
```

**cURL Paciente / Psicólogo:**

```bash
curl --location "http://localhost:5015/api/account/register" -F "TipoUsuario=Paciente" -F "Nombre=Camila" -F "Apellido=Rojas" -F "FechaNacimiento=2004-05-10" -F "IdGenero=2" -F "Correo=paciente.nuevo@test.com" -F "Password=Abc12345"

curl --location "http://localhost:5015/api/account/register" -F "TipoUsuario=Psicologo" -F "Nombre=Pedro" -F "Apellido=Lopez" -F "FechaNacimiento=1991-03-22" -F "IdGenero=1" -F "Correo=psy@test.com" -F "Password=Abc12345" -F "TarjetaProfesional=@C:/ruta/tarjeta.pdf;type=application/pdf"
```

### 2.3 `POST /api/account/verify` + `resend` — HU4

```js
// verify
await fetch("http://localhost:5015/api/account/verify", {
  method:"POST", headers:{"Content-Type":"application/json"},
  body: JSON.stringify({ correo, codigo })
});
// 200 {estado:"Activa", mensaje:"Registro exitoso. Ya puedes iniciar sesión"} // Paciente
// 200 {estado:"PendienteAprobacion", mensaje:"Registro exitoso. Tu cuenta está pendiente de aprobación por el administrador"} // Psicologo
// 400 {error:"Código incorrecto. Verifica e intenta nuevamente"}
// 410 {error:"El código expiró. Reenvía el código"}

// resend
await fetch("http://localhost:5015/api/account/resend", {
  method:"POST", headers:{"Content-Type":"application/json"},
  body: JSON.stringify({ correo })
});
// 200 {mensaje:"Código reenviado"}
```

### 2.4 `POST /api/account/login` — HU1-PB2 + HU2-PB2

`Dtos/AuthVm.cs:LoginVm` → `{correo, password}`

```js
const res = await fetch("http://localhost:5015/api/account/login", {
  method:"POST", headers:{"Content-Type":"application/json"},
  body: JSON.stringify({ correo, password })
});
const data = await res.json();
if (!res.ok) {
  // 400 {error:"Este campo es obligatorio"} // correo o password vacío
  // 400 {error:"Ingresa un correo válido"} // formato correo
  // 401 {error:"Correo o contraseña incorrectos"} // genérico, no revela qué falló
  // 403 {error:"Tu cuenta se encuentra suspendida. Contacta al administrador"}
  // 403 {error:"Tu cuenta está pendiente de aprobación por el administrador. Intenta más tarde"}
  showError(data.error);
} else {
  // 200 {mensaje:"Inicio de sesión exitoso", token, expiraEn, usuario:{id, correo, rol, panel, estado}}
  // panel: "Paciente" -> /panel-paciente, "Psicologo" -> /panel-psicologo, "Administrador" -> /admin
  localStorage.setItem("token", data.token);
  localStorage.setItem("rol", data.usuario.rol);
  location.href = data.usuario.panel === "Paciente" ? "/panel-paciente" : data.usuario.panel === "Psicologo" ? "/panel-psicologo" : "/admin";
}
```

`Services/LoginService.cs` busca `dbo.usuarios` case-insensitive, verifica `PBKDF2 v1$` (`Utilities/SecurityHelper.cs`), bloquea por `dbo.estados`, crea `dbo.sesiones` (`token_hash=SHA256(token)`, expira `Sesion:ExpiraHoras` 8h), actualiza `ultimo_acceso` y loguea `dbo.auth` (`LOGIN_ATTEMPT/SUCCESS/FAILED/BLOQUEADO_*`).

**cURL:**

```bash
curl --location "http://localhost:5015/api/account/login" -H "Content-Type: application/json" -d "{\"correo\":\"paciente.nuevo@test.com\",\"password\":\"Abc12345\"}"
```

### 2.5 `GET /api/account/me` — HU2-PB2 valida sesión

```js
const token = localStorage.getItem("token");
const res = await fetch("http://localhost:5015/api/account/me", {
  headers: { "Authorization": `Bearer ${token}` } // también acepta sin Bearer, minúsculas, comillas y ?token=
});
if (res.status === 401) { // {"error":"Tu sesión ha expirado. Inicia sesión nuevamente"}
  localStorage.removeItem("token");
  location.href = "/login";
} else if (res.status === 403) { // {"error":"Tu cuenta se encuentra suspendida..."}
  showError((await res.json()).error);
}
const user = await res.json(); // 200 {idUsuario, correo, nombre, apellido}
```

**cURL:**

```bash
curl --location "http://localhost:5015/api/account/me" --header "Authorization: Bearer kpsLqCCh9-VnApQMlPIX9vIbkdY0oCC_GKjU3k9Lrls"
# fallback query
curl --location "http://localhost:5015/api/account/me?token=kpsLqCCh9-VnApQMlPIX9vIbkdY0oCC_GKjU3k9Lrls"
```

Debug header: `GET /api/account/echo` con mismo `Authorization` devuelve `{auth, allHeaders, queryToken}`.

### 2.6 `POST /api/account/logout` — HU1-PB3 cerrar sesión

Confirma el modal `¿Deseas cerrar sesión?` → `Sí, cerrar sesión`:

```js
const token = localStorage.getItem("token");
const res = await fetch("http://localhost:5015/api/account/logout", {
  method:"POST",
  headers: { "Authorization": `Bearer ${token}` }
});
const data = await res.json();
if (res.ok) {
  // 200 {mensaje:"Sesión cerrada exitosamente"}
  localStorage.removeItem("token");
  localStorage.removeItem("rol");
  location.href = "/"; // Landing Page pública
} else {
  // 500 {error:"No fue posible cerrar sesión. Intenta nuevamente"} -> conserva la sesión
  showError(data.error);
}
// Si el usuario elige "No, permanecer": no llamar el endpoint, cerrar el modal y seguir en la pantalla.
```

`Services/LoginService.cs:LogoutAsync` calcula `SHA256(token)` (`Utilities/TokenHelper.cs`), borra la sesión de `dbo.sesiones` (invalida el token) y registra `LOGOUT` en `dbo.auth` con `actor/target` = usuario e IP. Idempotente: sin token o sesión inexistente devuelve `200` igual para que el front siempre limpie el token local. Eventual error interno → `500` con el mensaje exacto y el usuario continúa con sesión activa.

**cURL:**

```bash
curl --location --request POST "http://localhost:5015/api/account/logout" --header "Authorization: Bearer kpsLqCCh9-VnApQMlPIX9vIbkdY0oCC_GKjU3k9Lrls"
# -> 200 {"mensaje":"Sesión cerrada exitosamente"}
# tras esto GET /me con el mismo token -> 401
```

## 3. Mensajes exactos HU (mostrar tal cual)

* `"Debes seleccionar un tipo de cuenta: Paciente o Psicólogo"`
* `"Este campo es obligatorio"`
* `"Ingresa una fecha de nacimiento válida"` / `"La fecha de nacimiento no puede ser en el futuro"` / `"Debes tener al menos 14 años para registrarte"`
* `"Ingresa un correo válido"`
* `"La contraseña debe tener mínimo 6 caracteres e incluir al menos una letra y un número"`
* `"El correo ya se encuentra registrado"` (409)
* `"La tarjeta profesional es obligatoria para registrarse como psicólogo"` / `"El archivo debe estar en formato PDF"` / `"El archivo supera el tamaño máximo permitido de 5MB"`
* `"Código incorrecto. Verifica e intenta nuevamente"` / `"El código expiró. Reenvía el código"` / `"Código reenviado"`
* `"Correo o contraseña incorrectos"` (401)
* `"Tu cuenta se encuentra suspendida. Contacta al administrador"` (403)
* `"Tu cuenta está pendiente de aprobación por el administrador. Intenta más tarde"` (403)
* `"Tu sesión ha expirado. Inicia sesión nuevamente"` (401)
* `"Sesión cerrada exitosamente"` (200)
* `"No fue posible cerrar sesión. Intenta nuevamente"` (500)

## 4. Postman Agent

* Agent verde arriba a la derecha para `localhost`.
* `POST /register` → `Body → form-data` (no `raw`), keys exactas `TipoUsuario` etc., `TarjetaProfesional` tipo `File`.
* `POST /login`, `/verify`, `/resend` → `Body → raw → JSON`.
* `GET /me` y `POST /logout` → `Authorization` tab → `Bearer Token` (Postman añade `Bearer `) o `Headers` → `Authorization: Bearer <token>`.
* Si da `401` con header pero `200` con `?token=`, usa `GET /echo` para ver qué `Authorization` llega.

## 5. Notas front

* Guarda solo `token` (opaco `Utilities/TokenHelper.cs` Base64Url 32 bytes), nunca `password`.
* Cada `login` crea nueva `sesiones`; las viejas siguen válidas 8h. `logout` borra solo la sesión actual (por token).
* `¿Deseas cerrar sesión?` es front (modal): `Sí, cerrar sesión` → `POST /logout`; `No, permanecer` → cerrar modal sin llamar API.
* CORS ya permite todo. No uses `?token=` en prod, solo debug.
* Rama actual `PB2`, BD `SANTI/Mindzyxs` usuario `mindzyxs_app` (`db_datareader/db_datawriter`).

# Mindzyxs-API — Guía Front-End PB-05 (Recuperar contraseña)

API REST `Mindzyxs 1.1` — solo back-end. Todo pasa por `Controllers/AccountController.cs` → `Services/PasswordRecoveryService.cs` → `Repositories/*`. Usa `dbo.codigos_verificacion` con tipo `Recuperar` y `dbo.auth` para los logs (`PASSWORD_RESET`).

## Endpoints PB-05

| Método | Ruta | HU | Descripción | Auth |
|--------|------|----|-------------|------|
| `POST` | `/api/account/forgot` | HU1-PB5 | Solicitar código de recuperación | no |
| `POST` | `/api/account/forgot/verify` | HU2-PB5 | Validar el código antes de mostrar Nueva contraseña | no |
| `POST` | `/api/account/forgot/reset` | HU2-PB5 | Guardar la nueva contraseña | no |
| `POST` | `/api/account/forgot/resend` | HU2-PB5 | Reenviar código de recuperación | no |

### 2.1 `POST /api/account/forgot` — pantalla Recuperar contraseña (HU1-PB5)

`Dtos/AuthVm.cs:ForgotVm` → `{correo}`

```js
const res = await fetch("http://localhost:5015/api/account/forgot", {
  method:"POST", headers:{"Content-Type":"application/json"},
  body: JSON.stringify({ correo })
});
const data = await res.json();
if (!res.ok) {
  // 400 {error:"Este campo es obligatorio"}  // vacío
  // 400 {error:"Ingresa un correo válido"}   // formato inválido
  showError(data.error); // no envía código
} else {
  // 200 {mensaje:"Código enviado a tu correo"} // correo registrado -> pantalla Verificar código
  // 200 {mensaje:"Si el correo está registrado, recibirás un código"} // genérico si NO existe (no confirma la cuenta)
  location.href = "/recuperar/verificar?correo=" + encodeURIComponent(correo);
}
```

`Services/PasswordRecoveryService.cs:ForgotAsync`: valida vacío/formato, busca el usuario; si NO existe devuelve `200` con mensaje genérico (seguridad, no revela si la cuenta existe); si existe genera código 6 dígitos tipo `Recuperar`, expira `Registro:CodigoExpiraMinutos`, y envía `Utilities/EmailTemplate.cs:RecoveryCode`.

**cURL:**

```bash
curl --location "http://localhost:5015/api/account/forgot" -H "Content-Type: application/json" -d "{\"correo\":\"paciente.nuevo@test.com\"}"
```

### 2.2 `POST /api/account/forgot/verify` — pantalla Verificar código (HU2-PB5)

`Dtos/AuthVm.cs:VerifyCodeVm` → `{correo, codigo}`

```js
const res = await fetch("http://localhost:5015/api/account/forgot/verify", {
  method:"POST", headers:{"Content-Type":"application/json"},
  body: JSON.stringify({ correo, codigo })
});
const data = await res.json();
if (!res.ok) {
  // 400 {error:"Código incorrecto. Verifica e intenta nuevamente"} // incorrecto -> reintentar
  // 410 {error:"El código expiró. Reenvía el código"}               // expiró -> enlace Reenviar código
  showError(data.error);
} else {
  // 200 {mensaje:"Código verificado. Define tu nueva contraseña"} -> pantalla Nueva contraseña
  location.href = "/recuperar/nueva?correo=" + encodeURIComponent(correo);
}
```

**cURL:**

```bash
curl --location "http://localhost:5015/api/account/forgot/verify" -H "Content-Type: application/json" -d "{\"correo\":\"paciente.nuevo@test.com\",\"codigo\":\"123456\"}"
```

### 2.3 `POST /api/account/forgot/reset` — pantalla Nueva contraseña (HU2-PB5)

`Dtos/AuthVm.cs:ResetPasswordVm` → `{correo, codigo, password}`

```js
const res = await fetch("http://localhost:5015/api/account/forgot/reset", {
  method:"POST", headers:{"Content-Type":"application/json"},
  body: JSON.stringify({ correo, codigo, password })
});
const data = await res.json();
if (!res.ok) {
  // 400 {error:"Código incorrecto. Verifica e intenta nuevamente"}
  // 410 {error:"El código expiró. Reenvía el código"}
  // 400 {error:"La contraseña debe tener mínimo 6 caracteres e incluir al menos una letra y un número"}
  showError(data.error);
} else {
  // 200 {mensaje:"Contraseña actualizada exitosamente"} -> botón Iniciar sesión
}
```

`ResetPasswordAsync` revalida el código, valida la política (`Utilities/SecurityHelper.cs:PasswordHelper.IsStrongEnough`), actualiza `password_hash` (`Repositories/EfRepositories.cs:SetPasswordAsync`), consume el código y registra `PASSWORD_RESET` en `dbo.auth` con la IP. La comprobación de "las contraseñas coinciden" es del front (comparar `password` vs confirmación antes de llamar).

**cURL:**

```bash
curl --location "http://localhost:5015/api/account/forgot/reset" -H "Content-Type: application/json" -d "{\"correo\":\"paciente.nuevo@test.com\",\"codigo\":\"123456\",\"password\":\"Nueva12345\"}"
```

### 2.4 `POST /api/account/forgot/resend` — enlace Reenviar código (HU2-PB5)

`{correo}` → `200 {mensaje:"Código reenviado"}` (genérico si no existe, para no revelar cuentas).

## Mensajes exactos PB-05

* `"Este campo es obligatorio"` / `"Ingresa un correo válido"` (400)
* `"Código enviado a tu correo"` (200)
* `"Si el correo está registrado, recibirás un código"` (200, correo no registrado — no confirma si existe)
* `"Código incorrecto. Verifica e intenta nuevamente"` (400)
* `"El código expiró. Reenvía el código"` (410)
* `"La contraseña debe tener mínimo 6 caracteres e incluir al menos una letra y un número"` (400)
* `"Contraseña actualizada exitosamente"` (200)
* `"Las contraseñas no coinciden"` (validación front, no llama API)
* `"Código reenviado"` (200)

* Rama actual `PB5`, BD `SANTI/Mindzyxs`, retoma con `Paciente` y `Psicologo` los corridos en base. La tabla `dbo.codigos_verificacion.tipo` ya soporta `Recuperar` (no requiere DDL).
* Rama actual `PB03-HU01PB3` (base PB1 + PB2 fusionados), BD `SANTI/Mindzyxs` usuario `mindzyxs_app` (`db_datareader/db_datawriter`), `appsettings.json:ConnectionStrings:Mindzyxs`.

# Mindzyxs-API — Guía PB-06 (Modo invitado)

API REST `Mindzyxs 1.1` — solo back-end. El invitado NO tiene cuenta ni sesión: el front usa estos endpoints públicos para entrar sin registrarse, pintar el Panel de invitado, registrar accesos/salidas e intentos bloqueados sobre funciones exclusivas. Todo vive en `Controllers/InvitadoController.cs` → `Services/InvitadoService.cs` → `IAuthLogRepository` (log en `dbo.auth`).

## Endpoints PB-06 (HU1-PB6)

| Método | Ruta | Descripción | Auth |
|--------|------|-------------|------|
| `POST` | `/api/invitado/acceso` | Registrar acceso al modo invitado (o error de carga) en `dbo.auth` | no |
| `GET`  | `/api/invitado/panel` | Configuración del Panel de invitado (disponibles + exclusivas) | no |
| `POST` | `/api/invitado/bloqueo` | Registrar intento bloqueado de una función exclusiva | no |
| `POST` | `/api/invitado/salida` | Registrar salida del modo invitado (vuelta a la Landing) | no |

### 2.1 `POST /api/invitado/acceso` — entrar como invitado

`Dtos/InvitadoDto.cs:AccesoInvitadoVm` → `{origen, resultado}` (ambos opcionales).

```js
await fetch("http://localhost:5015/api/invitado/acceso", {
  method:"POST", headers:{"Content-Type":"application/json"},
  body: JSON.stringify({ origen: "landing", resultado: "ok" })
});
// 200 {mensaje:"Acceso registrado"}
// resultado:"error" -> log INVITADO_ACCESO_ERROR; "ok" (o vacío) -> INVITADO_ACCESO
```

El front lo llama al presionar "Modo invitado" en la Landing sin bloquear la UI. Si el endpoint no responde (red/back caído), mostrar `"No fue posible acceder en modo invitado. Intente nuevamente"`.

**cURL:**

```bash
curl --location "http://localhost:5015/api/invitado/acceso" -H "Content-Type: application/json" -d "{\"origen\":\"landing\",\"resultado\":\"ok\"}"
```

### 2.2 `GET /api/invitado/panel` — configuración del panel

Devuelve las 3 opciones disponibles del invitado y las 4 funciones exclusivas (bloqueadas) de usuarios registrados, más `avisoObligatorio` (flag que el front usa para mostrar el mensaje aclaratorio de no diagnóstico antes de funciones sensibles):

```bash
curl http://localhost:5015/api/invitado/panel
```

```json
{
  "avisoObligatorio": true,
  "disponibles": [
    { "codigo": "cuestionario", "titulo": "Responder cuestionario", "descripcion": "Cuestionario de bienestar emocional" },
    { "codigo": "recomendaciones", "titulo": "Ver recomendaciones básicas", "descripcion": "Consejos generales de cuidado" },
    { "codigo": "lineas-ayuda", "titulo": "Líneas de ayuda", "descripcion": "Contactos de emergencia y apoyo" }
  ],
  "exclusivas": [
    { "codigo": "historial", "titulo": "Historial emocional", "descripcion": "Registro de tus estados" },
    { "codigo": "graficos", "titulo": "Gráficos de progreso", "descripcion": "Evolución por tiempo" },
    { "codigo": "recordatorios", "titulo": "Recordatorios", "descripcion": "Avisos programados" },
    { "codigo": "psicologo", "titulo": "Autorización de psicólogo", "descripcion": "Vincular un profesional" }
  ]
}
```

### 2.3 `POST /api/invitado/bloqueo` — intento sobre función exclusiva

```js
await fetch("http://localhost:5015/api/invitado/bloqueo", {
  method:"POST", headers:{"Content-Type":"application/json"},
  body: JSON.stringify({ funcion: "Historial emocional" })
});
// 200 {mensaje:"Intento registrado"} -> log INVITADO_BLOQUEO en dbo.auth
```

El invitado recibe en el front el aviso `"🔒 ... requiere crear una cuenta o iniciar sesión"` (UI) y el back registra el intento.

### 2.4 `POST /api/invitado/salida` — volver a la Landing

```js
await fetch("http://localhost:5015/api/invitado/salida", {
  method:"POST", headers:{"Content-Type":"application/json"},
  body: JSON.stringify({ origen: "panel" })
});
// 200 {mensaje:"Salida registrada"} -> log INVITADO_SALIDA en dbo.auth
```

## Mensajes exactos PB-06

* `"No fue posible acceder en modo invitado. Intente nuevamente"` (muestra el front si el acceso/panel falla; se loguea como `INVITADO_ACCESO_ERROR`)
* `"Acceso registrado"` (200) / `"Intento registrado"` (200) / `"Salida registrada"` (200)
* `"Este campo es obligatorio"` (400) si el body llega vacío/inválido (los 3 POST)

* Rama actual `PB6`, BD `SANTI/Mindzyxs`; los logs quedan en `dbo.auth` con actor/target nulos (invitado no tiene cuenta). Si llega un `Bearer` válido de forma accidental, se asocia el `actorId` del usuario para trazabilidad.

## 3. Endpoints PB-04 (Mensaje aclaratorio)

PB-04 HU1-PB4: aviso obligatorio de que Mindzyx **no realiza diagnósticos clínicos** y no sustituye la atención profesional. Endpoints públicos (sin autenticación): accesibles para invitado, paciente y psicólogo.

| Método | Endpoint | HU | Descripción | Autenticación |
|--------|----------|----|-------------|---------------|
| `GET` | `/api/aviso` | HU1-PB4 | Contenido del mensaje aclaratorio | no |
| `POST` | `/api/aviso/visualizacion` | HU1-PB4 | Registra la visualización (o error de carga) en `dbo.auth` | no (asocia usuario si viene Bearer) |

La lógica vive en `Services/AvisoService.cs`; los registros van a `dbo.auth` vía `IAuthLogRepository` con acciones `AVISO_ACLARATORIO` y `AVISO_ACLARATORIO_ERROR` (`Utilities/Constants.cs:LogAcciones`).

### 3.1 `GET /api/aviso` — contenido del mensaje

Devuelve el texto completo que el front muestra en la modal "Mensaje aclaratorio" (o "Aviso importante"). Si este endpoint falla o el back no responde, el front debe mostrar `No fue posible cargar el mensaje aclaratorio. Intente nuevamente`.

```bash
curl --location "http://localhost:5015/api/aviso"
```

Respuesta `200`:

```json
{
  "titulo": "Mensaje aclaratorio",
  "texto": "Mindzyx es una plataforma diseñada exclusivamente para el monitoreo y apoyo del bienestar emocional.\nEste sistema no realiza diagnósticos clínicos y no sustituye la atención de un profesional de la salud mental.\n\nAnte un riesgo alto o una situación de urgencia, busca ayuda profesional o utiliza las líneas de ayuda disponibles en tu país (por ejemplo, servicios de emergencia o líneas de crisis locales).\n\nMindzyx es solo una herramienta de apoyo y monitoreo emocional y no reemplaza la atención profesional."
}
```

### 3.2 `POST /api/aviso/visualizacion` — registro de visualización

El front lo invoca (sin bloquear la UI) cuando se muestra la modal: por el enlace "Aviso: no diagnóstico" de la barra inferior, por el aviso automático antes de una funcionalidad sensible, o cuando la carga del contenido falló (`resultado: "error"`). Si llega `Authorization: Bearer <token>`, el registro se asocia al usuario autenticado; si no, se registra como invitado/anónimo.

```bash
curl --location "http://localhost:5015/api/aviso/visualizacion" \
  -H "Content-Type: application/json" \
  -d "{\"origen\":\"enlace\",\"resultado\":\"ok\"}"
```

Respuestas:

* `200` — `{ "mensaje": "Visualización registrada" }`
* `400` — `{ "error": "Este campo es obligatorio" }` (body inválido/vacío)

## Mensajes exactos PB-04

* `"Mensaje aclaratorio"` (título de la modal)
* `"No fue posible cargar el mensaje aclaratorio. Intente nuevamente"` (error de carga, se registra con `AVISO_ACLARATORIO_ERROR`)
* `"Visualización registrada"` (200)

# Mindzyxs-API — Guía PB-07 (Cuestionario emocional del invitado)

API REST `Mindzyxs 1.1` — solo back-end. Catálogo **dinámico desde BD** (`dbo.cuestionarios` + `dbo.preguntas` + `dbo.opciones_pregunta` vía `ICuestionarioRepository`): sirve N preguntas con M opciones, sin nada hardcodeado. Solo `Predeterminado` + `Publicado` (los `Personalizado` de psicólogos van por `asignaciones_cuestionario`, PB-22). Todo vive en `Controllers/CuestionarioController.cs` → `Services/CuestionarioService.cs` → `ICuestionarioRepository` + `IAuthLogRepository` (log en `dbo.auth`).

## Endpoints PB-07 (HU1-PB7)

| Método | Ruta | Descripción | Auth |
|--------|------|-------------|------|
| `GET`  | `/api/cuestionario/invitado?excluidos=1,2` | Siguiente cuestionario pendiente (rotación; excluye los `cuest_*` de la cookie) | no |
| `POST` | `/api/cuestionario/invitado/inicio` | Registrar inicio del cuestionario en `dbo.auth` | no |
| `POST` | `/api/cuestionario/invitado/finalizar` | Validar 100%, calcular puntaje/nivel y registrar fin en `dbo.auth` | no |

### 4.1 `GET /api/cuestionario/invitado` — pendiente con rotación

```bash
curl "http://localhost:5015/api/cuestionario/invitado?excluidos=1"
```

`200` con pendiente: `{ "disponible": true, "id": 2, "titulo": "Cuestionario de bienestar emocional", "avisoObligatorio": true, "preguntas": [{ "id": 4, "texto": "...", "orden": 1, "opciones": [{ "id": 13, "texto": "Nunca", "valor": 1, "orden": 1 }, ...] }] }`. Sin pendientes: `{ "disponible": false, "mensaje": "No tienes cuestionarios pendientes por responder." }`.

### 4.2 `POST /api/cuestionario/invitado/inicio` — registrar inicio

```bash
curl --location "http://localhost:5015/api/cuestionario/invitado/inicio" -H "Content-Type: application/json" -d "{\"cuestionarioId\":2}"
```

`200 {mensaje:"Inicio registrado"}` → log `CUESTIONARIO_INVITADO_INICIO`. No bloquea la UI.

### 4.3 `POST /api/cuestionario/invitado/finalizar` — validar 100% + puntaje

El front envía TODAS las respuestas de su sesión temporal. El back valida que cada pregunta tenga respuesta y que cada opción pertenezca a su pregunta, calcula `puntaje = promedio(valores de escala, redondeado)` y `nivel` (1-2 Bajo, 3 Medio, 4-5 Alto, helper `CuestionarioService.CalcularPuntaje` compartido con PB-08) y registra `CUESTIONARIO_INVITADO_FIN`. **No persiste respuestas** (`respuestas`→`evento`→`paciente NOT NULL`; el invitado no tiene cuenta): el front marca `cuest_{id}` como hecho en cookies y reusa las respuestas en PB-08.

```bash
curl --location "http://localhost:5015/api/cuestionario/invitado/finalizar" -H "Content-Type: application/json" -d "{\"cuestionarioId\":2,\"respuestas\":[{\"preguntaId\":4,\"opcionId\":15}]}"
```

`200 {mensaje:"Cuestionario completado", cuestionarioId, puntaje, nivel, totalPreguntas, respondidas}`.

## Mensajes exactos PB-07

* `"Cuestionario completado"` (200) / `"Inicio registrado"` (200)
* `"Debes seleccionar una respuesta para continuar"` (400) si falta alguna (todas son obligatorias)
* `"La respuesta seleccionada no es válida"` (400) si la opción no pertenece a su pregunta
* `"Cuestionario no disponible."` (404) si no está publicado
* `"Este campo es obligatorio"` (400) si el body llega vacío/inválido
* `"No tienes cuestionarios pendientes por responder."` (200 con `disponible:false`)

* Rama actual `PB7`, BD `SANTI/Mindzyxs` (seed `Mindzyxs-BD/sql/06_seed_cuestionarios_invitado.sql`: 3 publicados, escala 1-4; `q4` binaria excluida del promedio).

# Mindzyxs-API — Guía PB-08 (Riesgo emocional del invitado)

API REST `Mindzyxs 1.1` — solo back-end. El invitado presiona "Ver resultado" y el back valida que existan las respuestas completas en su sesión, calcula el nivel (Bajo/Medio/Alto) con el helper compartido `CuestionarioService.CalcularPuntaje` y arma la pantalla Resultado del riesgo. Todo vive en `Controllers/RiesgoController.cs` + `Controllers/AyudaController.cs` → `Services/RiesgoService.cs` → `ICuestionarioRepository` + `ILineaAyudaRepository` + `IAuthLogRepository` (log en `dbo.auth`).

## Endpoints PB-08 (HU1-PB8)

| Método | Ruta | Descripción | Auth |
|--------|------|-------------|------|
| `POST` | `/api/riesgo/invitado/calcular` | Calcular nivel desde las respuestas de la sesión + log | no |
| `GET`  | `/api/ayuda/lineas` | Líneas de ayuda en estado `Activa` (botón/modal, resaltadas si Alto) | no |

### 5.1 `POST /api/riesgo/invitado/calcular` — Ver resultado

```bash
curl --location "http://localhost:5015/api/riesgo/invitado/calcular" -H "Content-Type: application/json" -d "{\"cuestionarioId\":2,\"respuestas\":[{\"preguntaId\":4,\"opcionId\":15}]}"
```

`200`: `{ "nivel": "Medio", "puntaje": 3, "explicacion": "...", "alerta": { "mostrar": false, "mensaje": "" }, "mostrarLineasAyuda": true, "mostrarRecomendaciones": true, "avisoObligatorio": true }`. Si el nivel es Alto, `alerta.mostrar=true` con `"Riesgo alto detectado. Busca ayuda profesional o comunícate con una línea de ayuda"` y el front resalta "🚨 Líneas de ayuda (urgente)". Antes o junto al resultado, el front presenta el mensaje aclaratorio (`GET /api/aviso`) si está configurado.

### 5.2 `GET /api/ayuda/lineas` — líneas activas

```bash
curl http://localhost:5015/api/ayuda/lineas
```

`200`: `[ { "id": 1, "nombre": "Línea Prueba 123", "telefono": "123", "url": "...", "horario": ..., "cobertura": ..., "descripcion": ... } ]`. El contenido real lo administra el admin (PB-29).

## Mensajes exactos PB-08

* `"No se encontraron respuestas para calcular el riesgo. Por favor responde el cuestionario nuevamente"` (400; el front redirige al panel/cuestionario)
* `"Riesgo alto detectado. Busca ayuda profesional o comunícate con una línea de ayuda"` (alerta, solo Alto)
* `"La respuesta seleccionada no es válida"` (400) / `"Cuestionario no disponible."` (404) / `"Este campo es obligatorio"` (400)

* Rama actual `PB8`, BD `SANTI/Mindzyxs` (seed `Mindzyxs-BD/sql/07_seed_recomendaciones_invitado.sql`: 1 recomendación por nivel para PB-09).

# Mindzyxs-API — Guía PB-09 (Recomendaciones básicas del invitado)

API REST `Mindzyxs 1.1` — solo back-end. Lista de autocuidado básico priorizada por el nivel de riesgo de la sesión (Bajo/Medio/Alto) + detalle con pasos, duración sugerida e instrucciones. Contenido administrable (el admin lo crea/edita/desactiva en PB-28). Todo vive en `Controllers/RecomendacionController.cs` → `Services/RecomendacionService.cs` → `IRecomendacionRepository` + `IAuthLogRepository` (log en `dbo.auth`).

## Endpoints PB-09 (HU1-PB9)

| Método | Ruta | Descripción | Auth |
|--------|------|-------------|------|
| `GET`  | `/api/recomendaciones/invitado?nivel=Medio` | Lista priorizada (nivel primero, luego transversales, luego resto) + log | no |
| `GET`  | `/api/recomendaciones/invitado/{id}` | Detalle con pasos/duración/instrucciones + log | no |

### 6.1 `GET /api/recomendaciones/invitado` — lista priorizada

```bash
curl "http://localhost:5015/api/recomendaciones/invitado?nivel=Medio"
```

`200`: `{ "disponible": true, "nivel": "Medio", "recomendaciones": [{ "id": 4, "titulo": "Relajación muscular", "intro": "...", "icono": "💆", "nivel": "Medio", "prioritaria": true }, ...] }`. Sin `nivel`, orden estándar por id. Sin contenido: `{ "disponible": false, "mensaje": "No hay recomendaciones disponibles en este momento" }` (el front muestra el estado vacío con Volver al inicio).

### 6.2 `GET /api/recomendaciones/invitado/{id}` — detalle

```bash
curl http://localhost:5015/api/recomendaciones/invitado/4
```

`200`: `{ "id": 4, "titulo": "Relajación muscular", "duracion": "Duración sugerida: 10 minutos", "pasos": [...], "instrucciones": "...", "avisoObligatorio": true }` (inexistente → `404 "Recomendación no disponible."`). El front muestra el aviso de no diagnóstico junto al contenido y los botones "Volver a recomendaciones" / "Volver al inicio (Invitado)".

## Mensajes exactos PB-09

* `"No hay recomendaciones disponibles en este momento"` (200 con `disponible:false`)
* `"Recomendación no disponible."` (404)
* Consulta (lista y detalle) registrada como `RECOMENDACION_INVITADO_CONSULTA` en `dbo.auth`

* Rama actual `PB9`, BD `SANTI/Mindzyxs` (DDL + seed `Mindzyxs-BD/sql/08_recomendaciones_detalle.sql`: 6 filas con detalle; `nivel_riesgo NULL` = transversal).
