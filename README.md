# Mindzyxs-API — Guía Back-End (PB-01 a PB-09, PB-13 a PB-15, PB-27)

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
│   ├── HistorialController.cs       # PB-13: historial emocional (lista con filtro, detalle)
│   ├── ProgresoController.cs         # PB-14: progreso de autorizados (agregados + serie)
│   ├── RecomendacionController.cs   # PB-09: recomendaciones básicas (lista priorizada, detalle)
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
│   ├── HistorialService.cs          # PB-13: historial propio, detalle con nota/respuestas/apoyo
│   ├── ProgresoService.cs           # PB-14: agregados de autorizados (promedio, tendencia, serie)
│   ├── RecomendacionService.cs      # PB-09: lista priorizada por nivel y detalle con pasos
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
│   ├── HistorialDto.cs              # PB-13: lista, detalle con nota/respuestas/apoyo
│   ├── ProgresoDto.cs               # PB-14: agregados de autorizados y serie temporal
│   ├── RecordatorioDto.cs           # PB-15: config, guardado y avisos de hoy
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

Dependencias principales registradas en `Program.cs`: repositorios EF (`IUserRepository`, `ICatalogRepository`, `ICodigoRepository`, `ISesionRepository`, `IAuthLogRepository`, `IAdminRepository`, `ICuestionarioRepository`, `ILineaAyudaRepository`, `IRecomendacionRepository`, `IHistorialRepository`, `IChatRepository`), almacenamiento (`ITarjetaStorage`), correo (`IEmailSender`) y servicios de negocio.

## Swagger (documentación interactiva)

Swagger UI siempre disponible al levantar la API:

* **UI interactiva:** `http://localhost:5015/swagger`
* **Spec OpenAPI JSON:** `http://localhost:5015/swagger/v1/swagger.json`

Cada endpoint documenta las HUs que cubre, los mensajes exactos de error y los códigos de respuesta (200/201/400/401/403/409/410/500) con sus esquemas de `request`/`response`.

**Probar un endpoint autenticado desde Swagger:**

1. `POST /api/account/login` con las credenciales de tu usuario de prueba (ej. `admin@test.local` / `<tu-password>`).
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
| `Especialidades` | int[] (de catalogos) | Solo `Psicologo` (una, varias o ninguna; vacío = ninguna) → `400 "Las especialidades son solo para psicólogos"` / `"Selecciona especialidades válidas"`. Se guarda en `dbo.psicologo_especialidades` |

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
if (tipo === "Psicologo") especialidadesIds.forEach(id => fd.append("Especialidades", id)); // checklist: una, varias o ninguna

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
| `Especialidades` | int[] (de catalogos) | Solo `Psicologo` (una, varias o ninguna; vacío = ninguna) → `400 "Las especialidades son solo para psicólogos"` / `"Selecciona especialidades válidas"`. Se guarda en `dbo.psicologo_especialidades` |

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
if (tipo === "Psicologo") especialidadesIds.forEach(id => fd.append("Especialidades", id)); // checklist: una, varias o ninguna

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

> **Diseño futuro (decisión de producto): el cálculo de riesgo NO es fijo.** Hoy está quemado en `Services/CuestionarioService.cs` (promedio 1-5: ≤2 Bajo, 3 Medio, ≥4 Alto). A futuro el psicólogo debe poder elegir la escala (rango, ej. 1-4/1-5/1-10) y cómo se calcula el riesgo (umbrales por nivel, por cuestionario). No quemar más umbrales en código: toda lógica nueva de riesgo debe salir de configuración (BD), no de constantes.
>
> **Cuestionarios: no todos son de escala.** Los hay de opción múltiple donde el psicólogo define absolutamente todo (preguntas, opciones, valores, rangos). **Jamás se permiten respuestas abiertas.** El front no debe asumir escala fija ni textos de opciones (las opciones siempre vienen del backend por cuestionario).

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

* Rama actual `correccion-base-de-datos`, BD `SANTI/Mindzyxs`.
# Corrección base de datos (triggers + SPs + purga)

Rama `correccion-base-de-datos` (BD y back). Todo objeto SQL lleva encabezado-tabla (`Objeto | Fecha | Autor=Santiago Cortes | Cambio | Hace | Afecta`).

## Purga (`Mindzyxs-BD/sql/09_purga_usuarios_prueba.sql`)
Elimina usuarios de prueba sin hash PBKDF2 (seed 03: ids 1–5) y su rastro: reasigna `cuestionarios.id_creador` al admin real, borra eventos+respuestas, auth, sesiones y códigos. **Corre antes que los triggers** (si no, `TR_usuarios_BloqueoBorrado` la bloquearía).

## Triggers (`Mindzyxs-BD/sql/10_triggers_auth.sql`)
| Trigger | Hace |
|---|---|
| `TR_usuarios_AuditEstado` | UPDATE de estado → `auth.CAMBIO_ESTADO_CUENTA` |
| `TR_sesiones_Limite3` | INSERT conserva las 3 sesiones más nuevas (la 4ta expulsa la más vieja) |
| `TR_sesiones_AuditCreacion` | INSERT → `auth.SESION_CREADA` |
| `TR_codigos_ConsumoUnico` | INSERT expira códigos previos del mismo usuario+tipo |
| `TR_usuarios_PasswordSeguro` | INSTEAD OF: solo `password_hash` con formato `v1$…` (rechaza texto plano) |
| `TR_usuarios_BloqueoBorrado` | INSTEAD OF DELETE: bloquea si tiene historial (sugiere suspender); si está limpio, borra hijas + usuario |

## Store procedures (`Mindzyxs-BD/sql/11_store_procedures.sql`) y su uso en back
| SP | Uso en back |
|---|---|
| `sp_mantenimiento_limpieza` | `EfAdminRepository.LimpiarTodoAsync` (3 limpiezas → 1 EXEC, devuelve conteos) |
| `sp_sesion_crear` | `EfSesionRepository.CrearAsync` (devuelve `id_sesion`; triggers aplican límite+audit) |
| `sp_auth_registrar` | `EfAuthLogRepository.LogAsync` vía `ExecuteSqlRaw` (fecha del servidor) |
| `sp_cuestionario_completo` | `EfCuestionarioRepository.GetCompletoAsync` (preguntas+opciones en 1 round-trip, reader multi-set) |
| `sp_dashboard_admin` | Sin consumidor aún: base de la consola admin (PB-25) |
| `sp_recomendaciones_priorizadas` | `EfRecomendacionRepository.GetPriorizadasAsync` vía `FromSqlRaw` (el `OrderBy` salió del Service) |

# PB-31 Chat psicólogo-paciente (realtime Firebase + SQL)

Híbrido: **RTDB** para mensajes en vivo (`/conversaciones/{id}/meta|mensajes|presencia`), **SQL** para ciclo de vida (`dbo.conversaciones`: solicitud/aceptación/rechazo/bloqueo) y auditoría en `dbo.auth`. Reglas en `Firebase/database.rules.json` (participantes + `Aceptada` + bloqueo + texto 1-1000). Clave serviceAccount **nunca** en el repo (`GOOGLE_APPLICATION_CREDENTIALS` + `.env` local).

| Método | Ruta | Quién | Descripción |
|--------|------|-------|-------------|
| `POST` | `/api/chat/token` | sesión | Token Firebase (`uid`, claim `rol`, 1h) + `databaseUrl` |
| `GET` | `/api/chat/conversaciones` | sesión | Propias (pendientes = notificaciones internas) |
| `GET` | `/api/chat/pacientes?q=&nivel=&minCuestionarios=&tienePsicologo=` | psicólogo | Buscador multifiltro (nombre opcional, exige ≥1) con último riesgo + conteos |
| `POST` | `/api/chat/solicitudes {idPaciente}` | psicólogo aprobado/activo | Ofrece chat (reactiva Rechazada/Cerrada) |
| `POST` | `/api/chat/solicitudes/paciente {idPsicologo}` | paciente | Solicita chat al psicólogo (doble vía); el psicólogo acepta/rechaza |
| `POST` | `/api/chat/solicitudes/{id}/aceptar-psicologo\|rechazar-psicologo` | psicólogo dueño | Responde solicitud del paciente; al aceptar otorga autorización |
| `POST` | `/api/chat/conversaciones/{id}/bloqueo/solicitar {motivo}` | paciente dueño | Solicita bloqueo con motivo: queda `BloqueoPendiente` (mensajes congelados), log `ALERTA_BLOQUEO` |
| `POST` | `/api/chat/conversaciones/{id}/bloqueo/resolver {aceptar}` | psicólogo dueño o admin (paciente cancela con `false`) | Confirma (`Bloqueada`) o rechaza (`Aceptada`) |
| `POST` | `/api/chat/solicitudes/{id}/aceptar\|rechazar` | paciente dueño | Aceptar escribe `meta` Aceptada en RTDB **y otorga autorización al psicólogo** (aceptar el chat cuenta como autorizarlo); rechazar no toca la autorización |
| `POST` | `/api/chat/conversaciones/{id}/bloqueo` | paciente dueño | Bloquea (meta Bloqueada); bloqueado → 403 + aviso |
| `DELETE` | `/api/chat/conversaciones/{id}/bloqueo` | paciente que bloqueó | Desbloquea (vuelve a Aceptada) |
| `POST` | `/api/chat/conversaciones/{id}/mensajes/aviso` | participante | Auditoría best-effort tras el `push()` a RTDB |

Mensajes: 401 sin sesión; 403 bloqueado que no bloqueó; 404 inexistente; resto 400. Acciones `CHAT_SOLICITUD/ACEPTADO/RECHAZADO/BLOQUEO/DESBLOQUEO/MENSAJE` + `AUTORIZACION_OTORGADA` al aceptar. Nota: el envío entre psicólogo y paciente presupone además la autorización PB-17 (se integra en Sprint 4).

### Directorio de psicólogos (solo pacientes)

| Método | Ruta | Quién | Descripción |
|--------|------|-------|-------------|
| `GET` | `/api/psicologos` | paciente | Tarjetas con nombre, especialidades (vacía si ninguna) y pacientes asignados (autorizados) + log `DIRECTORIO_CONSULTA` |

Respuestas: `200` — `[ { "id", "nombre", "especialidades": [], "pacientesAsignados": 0 } ]`; `401` — sin sesión; `403` — sin rol Paciente. Solo psicólogos con cuenta Activa y aprobada (o legacy sin aprobación).
# Mindzyxs-API - Guía PB-13 (Historial emocional del paciente)

API REST `Mindzyxs 1.1` - solo back-end. Lista del paciente ordenada por fecha (recientes primero) con filtro por rango, y detalle con nota propia ("Cómo me sentí"), respuestas del cuestionario y apoyo del psicólogo. Todo vive en `Controllers/HistorialController.cs` → `Services/HistorialService.cs` → `IHistorialRepository` + `IAuthLogRepository` (log en `dbo.auth`). Solo el dueño ve lo suyo (ajeno = 404).

## Endpoints PB-13 (HU1-PB13)

| Método | Ruta | Descripción | Auth |
|--------|------|-------------|------|
| `GET` | `/api/historial?desde=aaaa-MM-dd&hasta=aaaa-MM-dd` | Mi historial (recientes primero) + log | Bearer paciente |
| `GET` | `/api/historial/{id}` | Detalle: nota + respuestas + apoyo + log | Bearer paciente (dueño) |

### 7.1 `GET /api/historial` - lista con filtro

```bash
curl "http://localhost:5015/api/historial?desde=2026-09-01&hasta=2026-09-30" -H "Authorization: Bearer <token>"
```

`200`: `[{ "id": 3, "fecha": ..., "tipo": "Cuestionario", "puntaje": 3, "nivel": "Medio", "cuestionario": "Cuestionario de bienestar emocional", "resumen": "..." }]`. Vacío (`[]`): el front muestra "Aún no tienes registros emocionales" con el botón Registrar estado emocional.

### 7.2 `GET /api/historial/{id}` - detalle

```bash
curl http://localhost:5015/api/historial/3 -H "Authorization: Bearer <token>"
```

`200`: lista + `"nota": "Cómo me sentí…"`, `"respuestas": [{ "pregunta", "valor", "texto" }]`, `"observaciones": [{ "contenido", "firma", "fecha" }]` (apoyo del psicólogo, PB-19). Inexistente o ajeno → `404 "Registro no encontrado."`.

## Mensajes exactos PB-13

* `"Aún no tienes registros emocionales"` (front, lista vacía)
* `"Registro no encontrado."` (404)
* Consulta (lista y detalle) registrada como `HISTORIAL_CONSULTA` en `dbo.auth`

* Rama actual `PB13`, BD `SANTI/Mindzyxs` (DDL + seed `Mindzyxs-BD/sql/16_historial_nota.sql`: `nota VARCHAR(MAX)` + 2 eventos demo con respuestas y apoyo).

# Mindzyxs-API - Guía PB-14 (Progreso de pacientes autorizados)

API REST `Mindzyxs 1.1` — solo back-end. Lista de pacientes que autorizaron al psicólogo + agregados (promedio, tendencia, semanas, distribución) y serie temporal para gráficos. Todo vive en `Controllers/ProgresoController.cs` → `Services/ProgresoService.cs` → `IHistorialRepository` + `IAuthLogRepository` (log en `dbo.auth`). Sin autorización → 403.

## Endpoints PB-14 (HU1-PB14)

| Método | Ruta | Descripción | Auth |
|--------|------|-------------|------|
| `GET` | `/api/progreso/pacientes` | Mis pacientes autorizados + log | Bearer psicólogo |
| `GET` | `/api/progreso/{idPaciente}?desde=&hasta=` | Agregados + serie + log | Bearer psicólogo autorizado |

`200`: `{ "promedio": 2.5, "tendencia": "Sube|Baja|Estable", "totalRegistros": 2, "distribucion": {"Medio":1}, "porSemana": [...], "serie": [...], "suficientes": false }`. Tendencia = 2da mitad vs 1ra (±0.5). `suficientes:false` (menos de 3) → el front muestra "No tienes registros suficientes para generar gráficos".

## Mensajes exactos PB-14

* `"No tienes pacientes autorizados en este momento"` (front, lista vacía)
* `"No tienes registros suficientes para generar gráficos"` (front, `suficientes:false`)
* `"No tienes autorización para ver los registros de este paciente."` (403)
* Consulta registrada como `PROGRESO_CONSULTA` en `dbo.auth`

* Rama actual `PB14` (apilada sobre `PB13`).

---
# Mindzyxs-API - Guía PB-23 (Estadísticas de pacientes autorizados)

Extiende **PB-14**: pantalla **Estadísticas de pacientes** en el panel del psicólogo. Lista pacientes autorizados (selector), controles de rango de fechas (`desde`/`hasta`), y al presionar **Generar** calcula y muestra: **promedio** de estado emocional, **tendencia** (Sube/Baja/Estable), **registros por semana**, **registros por mes**, **distribución por niveles** y **nivel más frecuente**. Si el paciente tiene `< 3` registros → `suficientes:false` y el front muestra "El paciente no tiene datos suficientes para generar estadísticas". Sin autorización → 403. Log `ESTADISTICAS_CONSULTA` en `dbo.auth`.

Mismos endpoints que PB-14 (`/api/progreso`), respuesta extendida con `nivelFrecuente` y `porMes`.

Todo vive en `Controllers/ProgresoController.cs` → `Services/ProgresoService.cs` → `IHistorialRepository` + `IAuthLogRepository` (log `ESTADISTICAS_CONSULTA` en `dbo.auth`).

## Endpoints PB-23 (HU1-PB23)

| Método | Ruta | Descripción | Auth |
|--------|------|-------------|------|
| `GET` | `/api/progreso/pacientes` | Mis pacientes autorizados (selector) | Bearer psicólogo |
| `GET` | `/api/progreso/{idPaciente}?desde=&hasta=` | Estadísticas completas (promedio, tendencia, por semana/mes, distribución, nivel frecuente) | Bearer psicólogo autorizado |

### 12.1 `GET /api/progreso/pacientes` - selector de pacientes autorizados

```bash
curl --location "http://localhost:5015/api/progreso/pacientes" -H "Authorization: Bearer <token-psicologo>"
```

`200 [{id, nombre}]`. Vacía → front muestra "No tienes pacientes autorizados en este momento".

### 12.2 `GET /api/progreso/{idPaciente}?desde=&hasta=` - estadísticas completas

```bash
curl --location "http://localhost:5015/api/progreso/6?desde=2026-09-01&hasta=2026-09-30" -H "Authorization: Bearer <token-psicologo>"
```

`200 { promedio, tendencia, totalRegistros, distribucion, porSemana, porMes, serie, nivelFrecuente, suficientes }`. `suficientes:false` (menos de 3 registros) → front muestra "El paciente no tiene datos suficientes para generar estadísticas". Sin autorización → `403 "No tienes autorización para ver los registros de este paciente."`.

## Mensajes exactos PB-23

* `"No tienes pacientes autorizados en este momento"` (front, lista vacía)
* `"El paciente no tiene datos suficientes para generar estadísticas"` (front, `suficientes:false`)
* `"No tienes autorización para ver los registros de este paciente."` (403)
* Consulta registrada como `ESTADISTICAS_CONSULTA` en `dbo.auth`

---

# Mindzyxs-API - Guía PB-15 (Recordatorios del paciente)

API REST `Mindzyxs 1.1` - solo back-end. Configuración de frecuencia y horario para el registro emocional (upsert por paciente), con activación/desactivación y endpoint de avisos vencidos-hoy para polling del front. Todo vive en `Controllers/RecordatorioController.cs` → `Services/RecordatorioService.cs` → `IRecordatorioRepository` + `IAuthLogRepository` (log en `dbo.auth`).

## Endpoints PB-15 (HU1-PB15)

| Método | Ruta | Descripción | Auth |
|--------|------|-------------|------|
| `GET` | `/api/recordatorios` | Mi configuración (204 si nunca configuré) | Bearer paciente |
| `POST` | `/api/recordatorios` | Guarda/actualiza (upsert) + log | Bearer paciente |
| `GET` | `/api/recordatorios/hoy` | Avisos vencidos hoy (activos, día aplica, hora pasada) | Bearer paciente |

### 8.1 `POST /api/recordatorios` - guardar

```bash
curl --location "http://localhost:5015/api/recordatorios" -H "Authorization: Bearer <token>" -H "Content-Type: application/json" -d "{\"frecuencia\":\"Semanal\",\"hora\":\"08:00\",\"dias\":[1,3,5],\"activo\":true}"
```

`200 {mensaje:"Recordatorios configurados correctamente", ...}`. Sin frecuencia/hora (o Semanal sin días) → `400 "Debes seleccionar frecuencia y hora"`. Días 1=Lunes..7=Domingo; en Diaria se ignoran (todos implícitos). Desactivar guarda `activo:false`.

### 8.2 `GET /api/recordatorios/hoy` - polling del front

El front lo consulta cada minuto y dispara el aviso en el momento exacto programado. `200 [{titulo, mensaje, hora}]` (vacío si nada vence).

## Mensajes exactos PB-15

* `"Recordatorios configurados correctamente"` (200)
* `"Debes seleccionar frecuencia y hora"` (400)
* Guardado registrado como `RECORDATORIO_CONFIG` en `dbo.auth`

* Rama actual `PB15`, BD `SANTI/Mindzyxs` (DDL `Mindzyxs-BD/sql/17_recordatorios_frecuencia.sql`: `frecuencia VARCHAR(20) DEFAULT Diaria`).

# Mindzyxs-API - Guía PB-10 (Editar perfil) + reanudar verificación

Solo back-end. Edición de perfil con validaciones, cambio de correo con código y reanudación de registros inconclusos. Todo vive en `Controllers/AccountController.cs` (4 endpoints nuevos) → `Services/PerfilService.cs` + `Services/VerificationService.cs` → repositorios existentes (sin DDL) + `IAuthLogRepository`.

## Endpoints PB-10 (HU1-PB10)

| Método | Ruta | Descripción | Auth |
|--------|------|-------------|------|
| `GET` | `/api/account/perfil` | Devuelve nombres/apellidos/fecha/género/correo | Bearer |
| `PUT` | `/api/account/perfil` | Actualiza nombres/apellidos/fecha/género + log | Bearer |
| `POST` | `/api/account/correo/solicitar` | Código al nuevo correo (409 si existe) | Bearer |
| `POST` | `/api/account/correo/confirmar` | Valida código y cambia el correo + log | Bearer |
| `POST` | `/api/account/verificacion/reanudar` | Si sigue pendiente, reenvía y avisa continuar | no |

Validaciones perfil: obligatorios (`"Este campo es obligatorio"`), fecha no futura y mayor de 14 (`"Ingresa una fecha válida"` / `"Ingresa una edad válida"`), género existente (`"Selecciona un género válido"`). Correo: formato (`"Ingresa un correo válido"`), duplicado → 409 `"El correo ya se encuentra registrado"`, código con expiración de 15 min (410 `"El código expiró. Reenvía el código"`).

Reanudar: `200 {"mensaje", "requiereVerificacion": true}` solo si sigue en `PendienteVerificacion`; si no, mensaje genérico sin revelar estados.

## Mensajes exactos PB-10

* `"Perfil actualizado correctamente"` (200) / `"Correo actualizado correctamente"` (200) / `"Código enviado"` (200)
* `"El correo ya se encuentra registrado"` (409)
* Logs `PERFIL_ACTUALIZADO`, `CORREO_SOLICITADO`, `CORREO_ACTUALIZADO` en `dbo.auth`

## Nota: cookie de riesgo del invitado

Sin cambios de back: `POST /api/cuestionario/invitado/finalizar` ya devuelve `{mensaje, cuestionarioId, puntaje, nivel, totalPreguntas, respondidas}` — el front guarda `puntaje/nivel` en la cookie de sesión del invitado y la reusa en PB-08. Si al reentrar no hay riesgo previo, es que el front no persistió esa respuesta.

* Rama actual `PB10` (sin DDL).

# Mindzyxs-API - Guía PB-11 (Cuestionario emocional del paciente)

API REST `Mindzyxs 1.1` - solo back-end. El paciente autenticado presiona **Responder cuestionario** (panel del paciente), el back le entrega un cuestionario pendiente (del mismo catálogo que el invitado: `Predeterminado` + `Publicado`, sin entregar los que el paciente ya respondió ni devolver nunca el mismo dos veces), valida que responda **el 100%** de las preguntas y al terminar **guarda las respuestas y el resultado en su historial** (`eventos_bienestar` + `respuestas`, los mismos que lee PB-13) y registra el inicio y la finalización en `dbo.auth`. Todo vive en `Controllers/CuestionarioController.cs` → `Services/CuestionarioService.cs` → `ICuestionarioRepository` + `IHistorialRepository` + `IAuthLogRepository`.

## Endpoints PB-11 (HU1-PB11)

| Método | Ruta | Descripción | Auth |
|--------|------|-------------|------|
| `GET`  | `/api/cuestionario/paciente/pendiente` | El siguiente cuestionario que el paciente aún no ha respondido | Bearer paciente |
| `POST` | `/api/cuestionario/paciente/inicio` | Registrar inicio del cuestionario en `dbo.auth` | Bearer paciente |
| `POST` | `/api/cuestionario/paciente/finalizar` | Validar 100%, calcular puntaje/nivel y guardar respuestas + resultado en el historial + log | Bearer paciente |
| `GET`  | `/api/cuestionario/paciente/asignados` | Cuestionarios asignados al paciente (`dbo.asignaciones_cuestionario`, recientes primero) + log | Bearer paciente |
| `GET`  | `/api/cuestionario/paciente/asignados/{idAsignacion}` | Asignado completo (preguntas+opciones) para responderlo + log | Bearer paciente |

### 9.1 `GET /api/cuestionario/paciente/pendiente` - el cuestionario que me toca responder

```bash
curl "http://localhost:5015/api/cuestionario/paciente/pendiente" -H "Authorization: Bearer <token>"
```

`200` con pendiente: `{ "disponible": true, "id": 1, "titulo": "Cuestionario Base Prueba", "avisoObligatorio": true, "preguntas": [{ "id": 1, "texto": "En las ultimas 2 semanas, me he sentido nervioso/a?", "orden": 1, "opciones": [{ "id": 1, "texto": "Nunca", "valor": 0, "orden": 1 }, ...] }] }`. Sin pendientes: `{ "disponible": false, "mensaje": "No tienes cuestionarios pendientes por responder." }`. Sin sesión → `401 "Sesión requerida"`.

### 9.2 `POST /api/cuestionario/paciente/inicio` - registrar inicio

```bash
curl --location "http://localhost:5015/api/cuestionario/paciente/inicio" -H "Authorization: Bearer <token>" -H "Content-Type: application/json" -d "{\"cuestionarioId\":1}"
```

`200 {mensaje:"Inicio registrado"}` → log `CUESTIONARIO_PACIENTE_INICIO`. No bloquea la UI.

### 9.3 `POST /api/cuestionario/paciente/finalizar` - validar 100% + guardar en historial

El front envía TODAS las respuestas (avance secuencial con Siguiente + Finalizar). El back valida que cada pregunta tenga respuesta y que cada opción pertenezca a su pregunta, calcula `puntaje = promedio(valores de escala, redondeado)` y `nivel` (1-2 Bajo, 3 Medio, 4-5 Alto, helper compartido `CuestionarioService.CalcularPuntaje`), **inserta el evento en `eventos_bienestar` (tipo Cuestionario) y cada respuesta en `respuestas`** en una transacción, y registra `CUESTIONARIO_PACIENTE_FIN`. La fila queda visible en `GET /api/historial` (PB-13) para alertas, recomendaciones y seguimiento.

```bash
curl --location "http://localhost:5015/api/cuestionario/paciente/finalizar" -H "Authorization: Bearer <token>" -H "Content-Type: application/json" -d "{\"cuestionarioId\":1,\"respuestas\":[{\"preguntaId\":1,\"opcionId\":3}]}"
```

`200 {mensaje:"Cuestionario completado", idEvento, cuestionarioId, puntaje, nivel, totalPreguntas, respondidas}`.

## Mensajes exactos PB-11

* `"Cuestionario completado"` (200) / `"Inicio registrado"` (200) / `"Sesión requerida"` (401)
* `"Debes seleccionar una respuesta para continuar"` (400) si falta alguna (todas son obligatorias)
* `"La respuesta seleccionada no es válida"` (400) si la opción no pertenece a su pregunta
* `"Cuestionario no disponible."` (404) si no está publicado
* `"Este campo es obligatorio"` (400) si el body llega vacío/inválido
* `"No tienes cuestionarios pendientes por responder."` (200 con `disponible:false`)
* Logs `CUESTIONARIO_PACIENTE_INICIO` y `CUESTIONARIO_PACIENTE_FIN` en `dbo.auth`

* Rama actual `PB11`, BD `SANTI/Mindzyxs` (seed `Minzyxs-DB/sql/03_seed_datos_prueba.sql`: cuestionario `Cuestionario Base Prueba`, Predeterminado/Publicado, 3 preguntas `opcion_multiple` 0-3). El resultado persiste con el seed de pruebas; el aviso aclaratorio (configurable) se sirve desde `GET /api/aviso` (PB-04) con `avisoObligatorio:true`.

# Mindzyxs-API - Guía PB-12 (Registrar estado emocional)

API REST `Mindzyxs 1.1` - solo back-end. El paciente autenticado presiona **Registrar estado emocional** (panel del paciente), el back recibe la escala seleccionada (1-5) y una nota opcional, valida que exista la escala, guarda el registro con fecha y hora **automáticas** en `eventos_bienestar` (visible luego en `GET /api/historial`, PB-13) y registra la creación en `dbo.auth`. Todo vive en `Controllers/HistorialController.cs` → `Services/HistorialService.cs` → `IHistorialRepository` + `IAuthLogRepository`.

El formulario (según mockup del PB) es una escala 1-5 con etiquetas: `1 Muy mal`, `2 Mal`, `3 Regular`, `4 Bien`, `5 Excelente`, un campo **Notas** opcional y los botones **Guardar** y **Cancelar / Volver** (cancelar es front y no guarda nada).

## Endpoints PB-12 (HU1-PB12)

| Método | Ruta | Descripción | Auth |
|--------|------|-------------|------|
| `POST` | `/api/historial` | Guarda el estado emocional (escala 1-5 + nota opcional) con fecha/hora automática + log | Bearer paciente |

### 10.1 `POST /api/historial` - guardar registro emocional

```bash
curl --location "http://localhost:5015/api/historial" -H "Authorization: Bearer <token-paciente>" -H "Content-Type: application/json" -d "{\"puntaje\":4,\"nota\":\"Me sentí bien hoy\"}"
```

`200 {mensaje:"Registro guardado correctamente", idEvento, fecha, puntaje, nivel, nota}`. `nivel` se deriva igual que el cuestionario (1-2 Bajo, 3 Medio, 4-5 Alto) y se guarda con el riesgo en el evento (lo usan PB-14 y PB-31). Sin escala o con escala inválida → `400 "Debes seleccionar un estado emocional para guardar"` y **no guarda**. El registro aparece al instante en `GET /api/historial`.

## Mensajes exactos PB-12

* `"Registro guardado correctamente"` (200)
* `"Debes seleccionar un estado emocional para guardar"` (400, si falta o es inválida; no guarda)
* `"Este campo es obligatorio"` (400, body vacío/inválido)
* `"Sesión requerida"` (401)
* Log `REGISTRO_EMOCIONAL_CREADO` en `dbo.auth`

* Rama actual `PB12` (apilada sobre `PB11`), BD `SANTI/Mindzyxs`. Sin DDL nuevo: la columna `nota` de `eventos_bienestar` ya existe (PB-13).

# Mindzyxs-API - Guía PB-16 (Alerta de riesgo alto)

API REST `Mindzyxs 1.1` - solo back-end. Cuando un paciente finaliza un cuestionario emocional (PB-11) o registra un estado emocional (PB-12), el back ya determina su nivel de riesgo (`nivel` en la respuesta). Si es **Alto**, el front muestra una alerta visible (modal/banner) con el mensaje fijo de la HU — "Riesgo alto detectado. Busca ayuda profesional. Si te sientes en peligro, contacta una línea de ayuda" — y los botones **Líneas de ayuda**, **Ver recomendaciones** y **Entendido**. El back aporta aquí el **registro en logs** de la **generación y visualización** de la alerta y de la **acción** que eligió el paciente. Con nivel Bajo o Medio la alerta no se muestra (el front solo invoca el endpoint cuando `nivel == "Alto"`). Todo vive en `Controllers/RiesgoController.cs` → `Services/RiesgoService.cs` → `IAuthLogRepository` + `IUserRepository`.

Líneas de ayuda y recomendaciones ya tienen sus endpoints: `GET /api/ayuda/lineas` (público, PB-08) y `GET /api/recomendaciones/invitado...` (PB-09). La redirección a esas secciones es del front: "Líneas de ayuda" y "Ver recomendaciones" navegan, "Entendido" cierra el modal y el paciente continúa.

## Endpoints PB-16 (HU1-PB16)

| Método | Ruta | Descripción | Auth |
|--------|------|-------------|------|
| `POST` | `/api/riesgo/paciente/alerta` | Registra la alerta: `"vista"` (se generó y visualizó) o la acción elegida (`lineas`, `recomendaciones`, `entendido`) + log en `dbo.auth` | Bearer paciente |

### 11.1 `POST /api/riesgo/paciente/alerta` - registro de la alerta en logs

```bash
# Generación + visualización del modal (nivel Alto)
curl --location "http://localhost:5015/api/riesgo/paciente/alerta" -H "Authorization: Bearer <token-paciente>" -H "Content-Type: application/json" -d "{\"accion\":\"vista\",\"origen\":\"cuestionario\"}"

# Acción: botón Líneas de ayuda / Ver recomendaciones / Entendido
curl --location "http://localhost:5015/api/riesgo/paciente/alerta" -H "Authorization: Bearer <token-paciente>" -H "Content-Type: application/json" -d "{\"accion\":\"lineas\",\"origen\":\"cuestionario\"}"
curl --location "http://localhost:5015/api/riesgo/paciente/alerta" -H "Authorization: Bearer <token-paciente>" -H "Content-Type: application/json" -d "{\"accion\":\"recomendaciones\",\"origen\":\"registro\"}"
curl --location "http://localhost:5015/api/riesgo/paciente/alerta" -H "Authorization: Bearer <token-paciente>" -H "Content-Type: application/json" -d "{\"accion\":\"entendido\",\"origen\":\"cuestionario\"}"
```

`origen` es opcional (solo describe qué disparó la alerta: `cuestionario` o `registro`) y va en el detalle del log. Respuesta `200 {mensaje:"Alerta registrada"}`. Guarda en `dbo.auth`: `ALERTA_RIESGO_ALTO_VISTA` (cuando `accion = "vista"`) o `ALERTA_RIESGO_ALTO_ACCION` (para `lineas`/`recomendaciones`/`entendido`).

## Mensajes exactos PB-16

* `"Alerta registrada"` (200)
* `"Debes seleccionar una opción de la alerta de riesgo"` (400, falta `accion`)
* `"Acción de la alerta no válida"` (400, `accion` fuera de `vista|lineas|recomendaciones|entendido`)
* `"Sesión requerida"` (401)
* Logs `ALERTA_RIESGO_ALTO_VISTA` y `ALERTA_RIESGO_ALTO_ACCION` en `dbo.auth`

* Rama actual `PB16`, BD `SANTI/Mindzyxs`. Sin DDL nuevo (el nivel de riesgo ya se guarda en el evento con PB-11/PB-12).

# Mindzyxs-API - Guía PB-21 (Gestión de cuestionarios del psicólogo)

API REST `Mindzyxs 1.1` - solo back-end. El psicólogo autenticado toca **Cuestionarios** en su panel → pantalla **Gestión de cuestionarios** con la lista de los suyos (Borrador o Publicado) en orden y con **búsqueda** (`GET` con `q`). Selecciona uno para ver su **detalle** (título, descripción, estado y preguntas). Si está **Publicado** y quiere editarlo, el front pide **confirmación** y `POST .../{id}/despublicar` lo pasa a **Borrador** antes de habilitar el editor. Al **Guardar como borrador** o **Publicar**, el back valida que quede **al menos una pregunta** (`El cuestionario debe tener al menos una pregunta`), guarda los cambios y responde `Cuestionario actualizado correctamente`. Puede **eliminar** con confirmación extra (baja lógica `Eliminado`, fuera de los análisis; se eliminan también las observaciones asociadas). Las ediciones/eliminaciones quedan en `dbo.auth`.

Todo vive en `Controllers/PsicologoCuestionariosController.cs` → `Services/PsicologoCuestionariosService.cs` → `ICuestionarioRepository` (`EfCuestionarioRepository`) + `IAuthLogRepository`.

**DDL necesario** (`Minzyxs-DB/sql/04_pb21_editar_cuestionarios.sql`): columna `preguntas.eliminada` (baja lógica de preguntas en uso) y recrea `sp_cuestionario_completo` filtrando preguntas eliminadas (el orden de columnas no cambia).

## Endpoints PB-21 (HU1-PB21)

| Método | Ruta | Descripción | Auth |
|--------|------|-------------|------|
| `GET` | `/api/psicologo/cuestionarios?q=` | Mis cuestionarios (Borrador/Publicado) con búsqueda, recientes primero | Bearer psicólogo |
| `GET` | `/api/psicologo/cuestionarios/{id}` | Detalle editable (preguntas/opciones con ids) | Bearer psicólogo (dueño) |
| `POST` | `/api/psicologo/cuestionarios/{id}/despublicar` | Confirmación: Publicado → Borrador + log | Bearer psicólogo (dueño) |
| `PUT` | `/api/psicologo/cuestionarios/{id}` | Guarda edición (Borrador o Publicado) + log | Bearer psicólogo (dueño) |
| `DELETE` | `/api/psicologo/cuestionarios/{id}?confirmar=true` | Elimina (baja lógica + observaciones) + log | Bearer psicólogo (dueño) |

### 12.1 `GET /api/psicologo/cuestionarios` - lista con búsqueda

```bash
curl --location "http://localhost:5015/api/psicologo/cuestionarios?q=ansiedad" -H "Authorization: Bearer <token-psicologo>"
```

`200 [{id, titulo, descripcion, estado, numPreguntas, fechaCreacion}]` ordenado por `fechaCreacion DESC`. `q` filtra por título. No salen los `Eliminado`.

### 12.2 `GET /api/psicologo/cuestionarios/{id}` - detalle (prellena el editor)

`200 {id, titulo, descripcion, estado, modoCalculo, umbralBajoMax, umbralMedioMax, fechaCreacion, preguntas:[{id, texto, orden, tipo, opciones:[{id, texto, valor, orden}]}]}`. Los `id` de preguntas/opciones permiten mapear la edición. `404 "Cuestionario no encontrado."` si no existe o no es mío.

### 12.3 `POST /api/psicologo/cuestionarios/{id}/despublicar` - confirmación de editar un publicado

```bash
curl --location -X POST "http://localhost:5015/api/psicologo/cuestionarios/7/despublicar" -H "Authorization: Bearer <token-psicologo>"
```

`200 {mensaje:"El cuestionario ahora es un borrador"}`. Si ya está Borrador → `400 "El cuestionario no está publicado."`. Guardo `CUESTIONARIO_EDITADO`. Con esto el front habilita el editor.

### 12.4 `PUT /api/psicologo/cuestionarios/{id}` - guardar borrador/publicar

```bash
curl --location -X PUT "http://localhost:5015/api/psicologo/cuestionarios/7" -H "Authorization: Bearer <token-psicologo>" -H "Content-Type: application/json" -d '{"titulo":"Ansiedad semanal (revisado)","publicar":false,"umbrales":{"bajoMax":6,"medioMax":12},"preguntas":[{"id":10,"texto":"¿Con qué frecuencia se sintió nervioso/a?","tipo":"opcion_multiple","opciones":[{"texto":"Nunca","valor":0},{"texto":"A veces","valor":2},{"texto":"Siempre","valor":4}]},{"texto":"¿Le costó conciliar el sueño?","tipo":"opcion_multiple","opciones":[{"texto":"No","valor":0},{"texto":"A veces","valor":2},{"texto":"Casi siempre","valor":4}]}]}'
```

Mismo cuerpo que crear + `id` por pregunta (nulo = nueva; el `id` de una pregunta que ya no venga se da de baja con `eliminada`). `200 {id, mensaje:"Cuestionario actualizado correctamente"}`. Cero preguntas → `400 "El cuestionario debe tener al menos una pregunta"`. Publicar (`publicar:true`) lo deja **disponible para asignar a pacientes**. Si el cuestionario está Publicado → `400 "Para modificar un cuestionario publicado pásalo primero a borrador."`.

### 12.5 `DELETE /api/psicologo/cuestionarios/{id}?confirmar=true` - eliminar con confirmación

```bash
curl --location -X DELETE "http://localhost:5015/api/psicologo/cuestionarios/7?confirmar=true" -H "Authorization: Bearer <token-psicologo>"
```

`200 {mensaje:"Cuestionario eliminado correctamente"}`. Sin `confirmar=true` → `400 "Debes confirmar la eliminación del cuestionario."`. Pasa a `Estado = Eliminado` (sale de las listas y de los análisis; los eventos ya respondidos se conservan por FK) y borra las **observaciones asociadas** a sus eventos.

## Mensajes exactos PB-21

* `"El cuestionario ahora es un borrador"` (200, despublicar)
* `"El cuestionario no está publicado."` (400, despublicar algo no publicado)
* `"Cuestionario actualizado correctamente"` (200, guardar edición)
* `"El cuestionario debe tener al menos una pregunta"` (400, al guardar sin preguntas)
* `"Para modificar un cuestionario publicado pásalo primero a borrador."` (400, editar un publicado sin despublicar)
* `"Debes confirmar la eliminación del cuestionario."` (400, borrar sin `confirmar=true`)
* `"Cuestionario eliminado correctamente"` (200, eliminar)
* `"Cuestionario no encontrado."` (404, no existe o no es mío)
* Validaciones compartidas con crear: `"Cada pregunta debe tener texto."`, `"Cada pregunta debe tener al menos dos opciones."`, `"Cada opción debe tener texto."`, `"El valor de cada opción debe ser mayor o igual a cero."`, `"El título es obligatorio."`
* Logs `CUESTIONARIO_EDITADO` y `CUESTIONARIO_ELIMINADO` en `dbo.auth`

* Rama actual `PB-21-EditarCuestionario`, BD `SANTI/Mindzyxs`. DDL nuevo: `Minzyxs-DB/sql/04_pb21_editar_cuestionarios.sql` (columna `preguntas.eliminada` + `sp_cuestionario_completo` filtrado).

---
# Mindzyxs-API - Guía PB-22 (Asignación de cuestionarios a pacientes)

El psicólogo autorizado toca **Cuestionarios** en su panel → elige **Asignar** en un cuestionario **Publicado** → pantalla **Asignar cuestionario** con la lista de **pacientes que le hayan autorizado** con casillas de verificación (los que ya tienen ese cuestionario asignado pendiente salen deshabilitados). Al marcar uno o varios y presionar **Asignar cuestionario**, el back registra las asignaciones (una fila por paciente en `asignaciones_cuestionario`, estado `Pendiente`), lo que hace el cuestionario **disponible de inmediato** en la cuenta del paciente (`GET /api/cuestionario/paciente/asignados`). Responde `Cuestionario asignado correctamente`, guarda `CUESTIONARIO_ASIGNADO` en `dbo.auth` y loguea en el mensaje los pacientes asignados.

Todo vive en `Controllers/PsicologoCuestionariosController.cs` → `Services/PsicologoCuestionariosService.cs` → `ICuestionarioRepository` (`EfCuestionarioRepository`, método `AsignarAsync`) + `IHistorialRepository` (pacientes autorizados, `GetPacientesAutorizadosAsync`/`GetEstadoIdAsync("Autorizado")`) + `IAuthLogRepository`.

**Sin DDL nuevo**: la tabla `dbo.asignaciones_cuestionario` y el lado paciente (PB-22) ya existen en `Minzyxs-DB/sql/01_create_tables_sqlserver.sql`.

## Endpoints PB-22 (HU1-PB22)

| Método | Ruta | Descripción | Auth |
|--------|------|-------------|------|
| `GET` | `/api/psicologo/cuestionarios/{id}/asignar` | Pantalla: cuestionario + pacientes autorizados (`yaAsignado` deshabilita casilla) | Bearer psicólogo (dueño) |
| `POST` | `/api/psicologo/cuestionarios/{id}/asignar` | Asigna a uno o varios pacientes autorizados (solo Publicado) + log | Bearer psicólogo (dueño) |

### 13.1 `GET /api/psicologo/cuestionarios/{id}/asignar` - pantalla de asignación

```bash
curl --location "http://localhost:5015/api/psicologo/cuestionarios/7/asignar" -H "Authorization: Bearer <token-psicologo>"
```

`200 {id, titulo, estado, pacientes:[{id, nombre, yaAsignado}]}`. Solo salen los pacientes que **autorizaron** al psicólogo (PB-18). `404 "Cuestionario no encontrado."` si no existe o no es mío.

### 13.2 `POST /api/psicologo/cuestionarios/{id}/asignar` - registrar la asignación

```bash
curl --location -X POST "http://localhost:5015/api/psicologo/cuestionarios/7/asignar" -H "Authorization: Bearer <token-psicologo>" -H "Content-Type: application/json" -d '{"pacientesIds":[2,3]}'
```

`200 {mensaje:"Cuestionario asignado correctamente", asignados:2}`. La asignación queda `Pendiente` y el paciente la ve de inmediato. No se duplica si el paciente ya tenía ese cuestionario asignado pendiente.

## Mensajes exactos PB-22

* `"Cuestionario asignado correctamente"` (200, asignación registrada)
* `"Debes seleccionar al menos un paciente"` (400, sin pacientes o solo no autorizados)
* `"Solo puedes asignar cuestionarios publicados."` (400, cuestionario no Publicado)
* `"Solo puedes asignar a pacientes que te hayan autorizado."` (400, algún id no autorizado)
* `"Cuestionario no encontrado."` (404, no existe o no es mío)
* Log `CUESTIONARIO_ASIGNADO` en `dbo.auth`

---
# Mindzyxs-API - Guía PB-24 (Perfil profesional del psicólogo)

El psicólogo autenticado y aprobado toca **Perfil profesional** en su panel → pantalla **Perfil profesional** con su información actual (especialidades, años de experiencia, descripción, datos de contacto, tarjeta profesional) y botón **Editar**. Al presionar **Editar** se habilita el formulario con los datos actuales. Al modificar y presionar **Guardar cambios**, el sistema valida:
- Años de experiencia ≥ 0
- Correo válido y único (si se modifica)
- Especialidades existentes
Si todo es válido, guarda los cambios, registra `PERFIL_PROFESIONAL_ACTUALIZADO` en `dbo.auth` y responde `Información profesional actualizada correctamente`. Al presionar **Cancelar** se descartan los cambios (lógica de front, el back no recibe petición).

Todo vive en `Controllers/PsicologoPerfilController.cs` → `Services/PsicologoService.cs` → `IUserRepository` (`EfUserRepository`) + `IAuthLogRepository`. Requiere psicólogo **activo** y **aprobado**. Sin DDL nuevo (DDL en `Minzyxs-DB/sql/24_pb24_perfil_profesional.sql`: columnas en `dbo.usuarios`).

## Endpoints PB-24 (HU1-PB24)

| Método | Ruta | Descripción | Auth |
|--------|------|-------------|------|
| `GET` | `/api/psicologo/perfil-profesional` | Ver perfil profesional (vista) | Bearer psicólogo (activo y aprobado) |
| `PUT` | `/api/psicologo/perfil-profesional` | Actualizar perfil profesional + log | Bearer psicólogo (activo y aprobado) |

### 14.1 `GET /api/psicologo/perfil-profesional` - ver perfil profesional

```bash
curl --location "http://localhost:5015/api/psicologo/perfil-profesional" -H "Authorization: Bearer <token-psicologo>"
```

`200 { id, nombre, correo, añosExperiencia, descripcionProfesional, datosContacto, tarjetaProfesionalVerificada, especialidades, tieneTarjetaArchivo }`.

### 14.2 `PUT /api/psicologo/perfil-profesional` - actualizar perfil profesional

```bash
curl --location -X PUT "http://localhost:5015/api/psicologo/perfil-profesional" -H "Authorization: Bearer <token-psicologo>" -H "Content-Type: application/json" -d '{"añosExperiencia":10,"descripcionProfesional":"Psicólogo clínico con enfoque en ansiedad y trauma.","datosContacto":"Tel: 3001234567, Clínica Central, Bogotá","tarjetaProfesionalVerificada":true,"especialidades":[1,3],"correo":"nuevo@correo.com"}'
```

`200 { mensaje: "Información profesional actualizada correctamente" }`. Guarda `PERFIL_PROFESIONAL_ACTUALIZADO` en `dbo.auth`.

Validaciones:
- `añosExperiencia` ≥ 0 → si no: `400 "Los años de experiencia no pueden ser negativos"`
- `correo` válido y único → si no: `400 "Ingresa un correo válido"` / `400 "El correo ya se encuentra registrado"`
- `especialidades` deben existir → si no: `400 "La especialidad X no existe"`

## Mensajes exactos PB-24

* `"Información profesional actualizada correctamente"` (200)
* `"Los años de experiencia no pueden ser negativos"` (400)
* `"Ingresa un correo válido"` (400, formato inválido)
* `"El correo ya se encuentra registrado"` (400, duplicado)
* `"La especialidad X no existe"` (400, ID no existe)
* Log `PERFIL_PROFESIONAL_ACTUALIZADO` en `dbo.auth`

* DDL nuevo: `Minzyxs-DB/sql/24_pb24_perfil_profesional.sql` (columnas `años_experiencia`, `descripcion_profesional`, `datos_contacto`, `tarjeta_profesional_verificada` en `dbo.usuarios`).
