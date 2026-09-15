# Mindzyxs-API — Guía Front-End (PB-01 + PB-02 + PB-03)

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
* `"Ingresa una edad válida"` / `"Ingresa un correo válido"`
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
* Rama actual `PB03-HU01PB3` (base PB1 + PB2 fusionados), BD `SANTI/Mindzyxs` usuario `mindzyxs_app` (`db_datareader/db_datawriter`), `appsettings.json:ConnectionStrings:Mindzyxs`.