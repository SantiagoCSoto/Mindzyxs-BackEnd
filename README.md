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
