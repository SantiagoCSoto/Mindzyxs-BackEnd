# Mindzyxs-API — Guía Front-End PB-01 (Registrar usuario)

API REST `Mindzyxs 1.1` — solo back-end. Front en `Mindzyxs-FrontEnd` (`develop`) nunca toca BD. Todo pasa por `Controllers/AccountController.cs` → `Services/AuthService.cs` + `VerificationService.cs` → `Repositories/*` (EF Core a `SANTI/Mindzyxs`).

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
| `Edad` | int 1-120 | → `400 "Ingresa una edad válida"` (API calcula `fecha_nacimiento = Today.AddYears(-Edad)` `Utilities/CodeHelper.cs:AgeHelper`) |
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
fd.append("Edad", edad);
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
curl --location "http://localhost:5015/api/account/register" -F "TipoUsuario=Paciente" -F "Nombre=Camila" -F "Apellido=Rojas" -F "Edad=22" -F "IdGenero=2" -F "Correo=paciente.nuevo@test.com" -F "Password=Abc12345"

curl --location "http://localhost:5015/api/account/register" -F "TipoUsuario=Psicologo" -F "Nombre=Pedro" -F "Apellido=Lopez" -F "Edad=35" -F "IdGenero=1" -F "Correo=psy@test.com" -F "Password=Abc12345" -F "TarjetaProfesional=@C:/ruta/tarjeta.pdf;type=application/pdf"
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
* `"Ingresa una edad válida"` / `"Ingresa un correo válido"`
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
* `Edad` → `fecha_nacimiento`, sin cambio DDL.
* Tarjeta solo en `file_path` (disco `C:\uploads\mindzyxs\tarjetas`), no blob.
* Rama actual `PB1`, BD `SANTI/Mindzyxs` usuario `mindzyxs_app` (`db_datareader/db_datawriter`), `appsettings.json:ConnectionStrings:Mindzyxs`.

# Mindzyxs-API — Guía Front-End PB-02 (Autenticar usuario)

API REST `Mindzyxs 1.1` — solo back-end. Front en `Mindzyxs-FrontEnd` (`develop`) nunca toca BD. Todo pasa por `Controllers/AccountController.cs` → `Services/LoginService.cs` → `Repositories/*` (EF Core a `SANTI/Mindzyxs`).

## 1. Levantar el API

```bash
dotnet run --urls "http://localhost:5015"
# Properties/launchSettings.json: http 5015
# Program.cs: CORS AllowAnyOrigin/Headers/Methods, HttpsRedirection desactivado en dev
```
Base URL dev: `http://localhost:5015`

## 2. Endpoints PB-02

| Método | Ruta | HU | Descripción | Auth |
|--------|------|----|-------------|------|
| `POST` | `/api/account/login` | HU1-PB2 + HU2-PB2 | Login, validaciones, bloqueos, crea sesión | no |
| `GET` | `/api/account/me` | HU2-PB2 | Valida sesión / expiración en segundo plano | Bearer |
| `GET` | `/api/account/echo` | debug | Echo `Authorization` recibido (Postman) | no |

Todos devuelven `400 {error:"..."}` en validación (`Program.cs:10`).

### 2.1 `POST /api/account/login`

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
  // 401 {error:"Correo o contraseña incorrectos"} // genérico HU1-PB2, no revela qué falló
  // 403 {error:"Tu cuenta se encuentra suspendida. Contacta al administrador"} // Suspendida/Inactiva/PendienteVerificacion HU2-PB2
  // 403 {error:"Tu cuenta está pendiente de aprobación por el administrador. Intenta más tarde"} // Psicologo PendienteAprobacion
  showError(data.error);
} else {
  // 200 {mensaje:"Inicio de sesión exitoso", token, expiraEn, usuario:{id, correo, rol, panel, estado}}
  // panel: "Paciente" -> /panel-paciente, "Psicologo" -> /panel-psicologo, "Administrador" -> /admin
  localStorage.setItem("token", data.token);
  localStorage.setItem("rol", data.usuario.rol);
  location.href = data.usuario.panel === "Paciente" ? "/panel-paciente" : data.usuario.panel === "Psicologo" ? "/panel-psicologo" : "/admin";
}
```

`Services/LoginService.cs` busca `dbo.usuarios` case-insensitive, verifica `PBKDF2 v1$` (`Utilities/SecurityHelper.cs`), bloquea por `dbo.estados`, crea `dbo.sesiones` (`token_hash=SHA256(token)`, `expira 8h` `appsettings.json:Sesion:ExpiraHoras`), `ultimo_acceso`, log `dbo.auth` (`LOGIN_ATTEMPT/SUCCESS/FAILED/BLOQUEADO_*`).

**cURL:**

```bash
curl --location "http://localhost:5015/api/account/login" -H "Content-Type: application/json" -d "{\"correo\":\"paciente.nuevo@test.com\",\"password\":\"Abc12345\"}"
```

### 2.2 `GET /api/account/me`

```js
const token = localStorage.getItem("token");
const res = await fetch("http://localhost:5015/api/account/me", {
  headers: { "Authorization": `Bearer ${token}` } // robusto: acepta Bearer/sin Bearer/minúsculas/comillas y ?token=
});
if (res.status === 401) { // {"error":"Tu sesión ha expirado. Inicia sesión nuevamente"}
  localStorage.removeItem("token");
  location.href = "/login";
} else if (res.status === 403) { // {"error":"Tu cuenta se encuentra suspendida..."}
  showError((await res.json()).error);
}
const user = await res.json(); // {idUsuario, correo, nombre, apellido}
```

`Controllers/AccountController.cs:101` lee `Authorization: Bearer <token>` o `?token=` (`Request.Headers` + `Request.Query`). `Services/LoginService.cs:ValidateSessionAsync` verifica `sesiones.token_hash`, `expira_en > UtcNow` (si expiró borra y `401`), estado `Activa` o `403`, log `SESION_EXPIRADA`.

**cURL:**

```bash
curl --location "http://localhost:5015/api/account/me" --header "Authorization: Bearer kpsLqCCh9-VnApQMlPIX9vIbkdY0oCC_GKjU3k9Lrls"
# fallback query
curl --location "http://localhost:5015/api/account/me?token=kpsLqCCh9-VnApQMlPIX9vIbkdY0oCC_GKjU3k9Lrls"
```

Debug header: `GET /api/account/echo` con mismo `Authorization` devuelve `{auth, allHeaders, queryToken}`.

## 3. Mensajes exactos HU (mostrar tal cual)

* `"Este campo es obligatorio"` (400)
* `"Ingresa un correo válido"` (400)
* `"Correo o contraseña incorrectos"` (401)
* `"Tu cuenta se encuentra suspendida. Contacta al administrador"` (403)
* `"Tu cuenta está pendiente de aprobación por el administrador. Intenta más tarde"` (403)
* `"Tu sesión ha expirado. Inicia sesión nuevamente"` (401)

## 4. Postman Agent

* Agent verde arriba a la derecha para `localhost`.
* `POST /login` → `Body → raw → JSON`.
* `GET /me` → `Authorization` tab → `Bearer Token` → pega solo el token (Postman añade `Bearer `) o `Headers` → `Authorization: Bearer <token>`.
* Si `401` con header pero `200` con `?token=`, usa `GET /echo` para ver qué `auth` llega.

## 5. Notas front

* Guarda solo `token` (opaco `Utilities/TokenHelper.cs` Base64Url 32 bytes), nunca `password`.
* Cada `login` crea nueva `sesiones`; las viejas siguen válidas 8h. Para single-sesión se añade `DELETE FROM sesiones WHERE id_usuario=@id` en `LoginService`.
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
