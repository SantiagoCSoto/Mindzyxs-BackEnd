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
