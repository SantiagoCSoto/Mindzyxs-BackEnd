# Mindzyxs-API — Guía Front-End (PB-01 + PB-02)

API REST para `Mindzyxs 1.1` — solo back-end. El front vive en `Mindzyxs-FrontEnd` (`develop`) y nunca toca la BD directo. Todo pasa por `Controllers/AccountController.cs` → `Services/*` → `Repositories/*` (EF Core a `SANTI/Mindzyxs`).

## 1. Levantar el API

```bash
dotnet run --urls "http://localhost:5015"
# Properties/launchSettings.json: http 5015, https 7039
# Program.cs: CORS AllowAnyOrigin/Headers/Methods, HttpsRedirection desactivado en dev
```

Base URL dev: `http://localhost:5015`

## 2. Endpoints

| Método | Ruta | PB/HU | Descripción | Auth |
|--------|------|-------|-------------|------|
| `GET` | `/api/account/catalogos` | PB-01 HU1+HU2 | Tipos de cuenta + géneros | no |
| `POST` | `/api/account/register` | PB-01 HU1-3 | Registro `multipart/form-data` | no |
| `POST` | `/api/account/verify` | PB-01 HU4 | Verificar código | no |
| `POST` | `/api/account/resend` | PB-01 HU4 | Reenviar código | no |
| `POST` | `/api/account/login` | PB-02 HU1-2 | Login + bloqueos + sesión | no |
| `GET` | `/api/account/me` | PB-02 HU2 | Validar sesión (expiración) | Bearer |
| `GET` | `/api/account/echo` | debug | Echo `Authorization` recibido | no |

Todos devuelven JSON `{error:"..."}` en fallo. Ver `Program.cs:10` `InvalidModelState` → `400 {error:"..."}`.

### 2.1 `GET /api/account/catalogos`

```bash
curl http://localhost:5015/api/account/catalogos
# -> 200 {"tiposCuenta":[{"id":1,"codigo":"Paciente"},{"id":2,"codigo":"Psicologo"}],"generos":[{"id":1,"codigo":"Masculino"},...]}
```

Usa `tiposCuenta` para el `ComboBox Tipo de usuario` y `generos` para el `select Género`. `requiereTarjeta = tipo==="Psicologo"`.

### 2.2 `POST /api/account/register` — `multipart/form-data`

Campos (nombres exactos `Dtos/AuthVm.cs:RegisterVm`):

| Campo | Tipo | Regla HU |
|-------|------|----------|
| `TipoUsuario` | `Paciente` \| `Psicologo` | `HU1` → `400 "Debes seleccionar un tipo de cuenta: Paciente o Psicólogo"` |
| `Nombre`, `Apellido` | string | `HU2` → `400 "Este campo es obligatorio"` |
| `Edad` | int 1-120 | → `400 "Ingresa una edad válida"` (API calcula `fecha_nacimiento = Today.AddYears(-Edad)` `Utilities/CodeHelper.cs:AgeHelper`) |
| `IdGenero` | int (de catalogos) | → `400 "Este campo es obligatorio"` |
| `Correo` | string | → `400 "Este campo es obligatorio"` / `400 "Ingresa un correo válido"` / `409 "El correo ya se encuentra registrado"` |
| `Password` | string | `HU2` → `400 "La contraseña debe tener mínimo 6 caracteres e incluir al menos una letra y un número"` (`Utilities/SecurityHelper.cs:PasswordHelper`) |
| `TarjetaProfesional` | `File` PDF ≤5MB | `HU3` solo `Psicologo` → `400 "La tarjeta profesional es obligatoria..."` / `"El archivo debe estar en formato PDF"` / `"El archivo supera el tamaño máximo permitido de 5MB"` (`Utilities/FileHelper.cs` verifica `%PDF` y guarda en `Storage:TarjetasPath` + `dbo.usuarios.file_path`) |

**JS front (fetch + FormData):**

```js
const fd = new FormData();
fd.append("TipoUsuario", tipo); // "Paciente" | "Psicologo"
fd.append("Nombre", nombres);
fd.append("Apellido", apellidos);
fd.append("Edad", edad); // número
fd.append("IdGenero", generoId);
fd.append("Correo", correo);
fd.append("Password", password);
if (tipo === "Psicologo") fd.append("TarjetaProfesional", fileInput.files[0]);

const res = await fetch("http://localhost:5015/api/account/register", { method:"POST", body: fd });
// 201 {idUsuario, mensaje:"Registro exitoso. Verifica tu cuenta."}
// 400/409 {error:"..."} -> mostrar debajo del campo
```

### 2.3 `POST /api/account/verify` + `resend` — `HU4`

```js
// verify
await fetch("http://localhost:5015/api/account/verify", {
  method:"POST", headers:{"Content-Type":"application/json"},
  body: JSON.stringify({ correo, codigo })
});
// 200 {estado:"Activa", mensaje:"Registro exitoso. Ya puedes iniciar sesión"} // Paciente
// 200 {estado:"PendienteAprobacion", mensaje:"Registro exitoso. Tu cuenta está pendiente de aprobación..."} // Psicologo
// 400 {error:"Código incorrecto. Verifica e intenta nuevamente"}
// 410 {error:"El código expiró. Reenvía el código"}

// resend
await fetch("http://localhost:5015/api/account/resend", {
  method:"POST", headers:{"Content-Type":"application/json"},
  body: JSON.stringify({ correo })
});
// 200 {mensaje:"Código reenviado"}
```

Código de 6 dígitos `Utilities/CodeHelper.cs:NewCode6()`, expira 15 min (`appsettings.json:Registro:CodigoExpiraMinutos`), mail `Utilities/EmailTemplate.cs:VerificationCode`.

### 2.4 `POST /api/account/login` — `PB-02 HU1-2`

```js
const res = await fetch("http://localhost:5015/api/account/login", {
  method:"POST", headers:{"Content-Type":"application/json"},
  body: JSON.stringify({ correo, password })
});
const data = await res.json();
if (!res.ok) {
  // 400 {error:"Este campo es obligatorio"} | {error:"Ingresa un correo válido"}
  // 401 {error:"Correo o contraseña incorrectos"} // genérico, no revela qué falló
  // 403 {error:"Tu cuenta se encuentra suspendida. Contacta al administrador"} // Suspendida/Inactiva/PendienteVerificacion
  // 403 {error:"Tu cuenta está pendiente de aprobación por el administrador. Intenta más tarde"} // Psicologo PendienteAprobacion
  showError(data.error);
} else {
  // 200 {mensaje:"Inicio de sesión exitoso", token, expiraEn, usuario:{id, correo, rol, panel, estado}}
  // panel: "Paciente" | "Psicologo" | "Administrador" -> redirige ahí
  localStorage.setItem("token", data.token);
  localStorage.setItem("rol", data.usuario.rol);
  location.href = data.usuario.panel === "Paciente" ? "/panel-paciente" : data.usuario.panel === "Psicologo" ? "/panel-psicologo" : "/admin";
}
```

`Services/LoginService.cs` valida vacíos/formato, busca `dbo.usuarios` case-insensitive, verifica `PBKDF2 v1$` (`Utilities/SecurityHelper.cs`), bloquea por `dbo.estados`, crea `dbo.sesiones` (`token_hash = SHA256(token)`, `expira 8h` `appsettings.json:Sesion:ExpiraHoras`), actualiza `ultimo_acceso`, log `dbo.auth` (`LOGIN_ATTEMPT/SUCCESS/FAILED/BLOQUEADO_*`).

### 2.5 `GET /api/account/me` — validar sesión / expiración en segundo plano `HU2-PB2`

```js
const token = localStorage.getItem("token");
const res = await fetch("http://localhost:5015/api/account/me", {
  headers: { "Authorization": `Bearer ${token}` } // también acepta sin "Bearer", con comillas, y ?token= para debug
  // Alternativa: fetch(`http://localhost:5015/api/account/me?token=${token}`)
});
if (res.status === 401) {
  // {"error":"Tu sesión ha expirado. Inicia sesión nuevamente"} -> sesion borrada en BD
  localStorage.removeItem("token");
  location.href = "/login";
} else if (res.status === 403) {
  // {"error":"Tu cuenta se encuentra suspendida..."}
}
const user = await res.json(); // {idUsuario, correo, nombre, apellido}
```

`Controllers/AccountController.cs:101` lee `Authorization: Bearer <token>` (robusto: `Bearer`, sin `Bearer`, minúsculas, comillas) o `?token=`. `Services/LoginService.cs:ValidateSessionAsync` verifica `sesiones.token_hash`, `expira_en > UtcNow`, estado `Activa`, log `SESION_EXPIRADA`.

**Debug header:** `GET /api/account/echo` con mismo `Authorization` devuelve `{auth, allHeaders, queryToken}` para ver qué envía Postman.

## 3. Manejo de errores front (mensajes exactos HU)

Mostrar el `error` tal cual bajo el campo:

* `"Debes seleccionar un tipo de cuenta: Paciente o Psicólogo"`
* `"Este campo es obligatorio"`
* `"Ingresa una edad válida"` / `"Ingresa un correo válido"`
* `"La contraseña debe tener mínimo 6 caracteres e incluir al menos una letra y un número"`
* `"El correo ya se encuentra registrado"` (409)
* `"El archivo debe estar en formato PDF"` / `"El archivo supera el tamaño máximo permitido de 5MB"`
* `"Código incorrecto. Verifica e intenta nuevamente"` / `"El código expiró. Reenvía el código"`
* `"Correo o contraseña incorrectos"` (401 genérico)
* `"Tu cuenta se encuentra suspendida. Contacta al administrador"` (403)
* `"Tu cuenta está pendiente de aprobación por el administrador. Intenta más tarde"` (403)
* `"Tu sesión ha expirado. Inicia sesión nuevamente"` (401)

## 4. Postman Agent

* Agent verde arriba a la derecha para `localhost`.
* `POST /login` → `Body → raw → JSON` (no `form-data`).
* `GET /me` → `Authorization` tab → `Bearer Token` → pega solo el token (Postman añade `Bearer `) o `Headers` → `Authorization: Bearer <token>`.

## 5. Notas front

* No guardar `password`, solo `token` (opaco, `Utilities/TokenHelper.cs` Base64Url 32 bytes).
* Cada `login` crea nueva sesión; las viejas siguen válidas 8h hasta `expira_en` o logout (PB-03 las borrará). Si quieres single-sesión, avisa y se añade `DELETE FROM sesiones WHERE id_usuario=@id` en `LoginService`.
* CORS ya permite `AllowAnyOrigin/Header/Method`.
* El front nunca muestra `?token=` en prod — solo para debug.

Ramas: `PB1` (registro) → `PB2` (login/sesión) → `main`. BD `SANTI/Mindzyxs` con usuario `mindzyxs_app` solo `db_datareader/db_datawriter`.
