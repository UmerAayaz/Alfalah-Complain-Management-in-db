# Complaint Management System --- Office PC (.NET 8)

This is the command reference for setting up and running the Complaint
Management System on the office PC.

The office PC uses **.NET 8**. Keep the project on .NET 8 if the project
files contain:

``` xml
<TargetFramework>net8.0</TargetFramework>
```

## 1. First-time checks

Open PowerShell in the repository root and check the installed versions:

``` powershell
dotnet --version
node --version
npm --version
```

The .NET command should show an installed .NET 8 SDK, for example
`8.0.xxx`.

## 2. Entity Framework CLI

Use the EF Core 8 CLI with the .NET 8 project:

``` powershell
dotnet tool update --global dotnet-ef --version 8.*
```

Check it:

``` powershell
dotnet ef --version
```

## 3. Restore backend packages

From the repository root:

``` powershell
dotnet restore src/BankingPlatform.Api/BankingPlatform.Api.csproj
```

This restores the NuGet packages already declared by the project.

## 4. Build the backend

``` powershell
dotnet build src/BankingPlatform.Api/BankingPlatform.Api.csproj
```

Fix any build error before starting the application.

## 5. Apply existing database migrations

``` powershell
dotnet ef database update --project src/BankingPlatform.Infrastructure --startup-project src/BankingPlatform.Api --context AppDbContext
```

Do **not** create a new migration just to run the project.

Only use `dotnet ef migrations add ...` when the EF database model has
actually changed and a new migration needs to be created.

## 6. Email notification package

The workflow email service uses MailKit.

If MailKit has already been added to the Infrastructure project and
committed to source control, you do **not** need to add it again.
`dotnet restore` will restore it.

If it has not been added yet, run this once:

``` powershell
dotnet add src/BankingPlatform.Infrastructure package MailKit
```

Then:

``` powershell
dotnet restore src/BankingPlatform.Api/BankingPlatform.Api.csproj
dotnet build src/BankingPlatform.Api/BankingPlatform.Api.csproj
```

## 7. Configure email credentials on the office PC

Do not put the real SMTP password in source control.

The non-secret configuration can remain in `appsettings.json`:

``` json
"Email": {
  "Host": "YOUR_SMTP_HOST",
  "Port": 587,
  "Username": "complaints@yourcompany.com",
  "Password": "",
  "FromEmail": "complaints@yourcompany.com",
  "FromName": "Complaint Management",
  "EnableSsl": true
}
```

Replace the example host, username, and sender address with the real
office email configuration.

Initialize .NET User Secrets on the office PC if the API project has not
already been initialized for them:

``` powershell
dotnet user-secrets init --project src/BankingPlatform.Api
```

Set the real SMTP password locally:

``` powershell
dotnet user-secrets set "Email:Password" "YOUR_REAL_SMTP_PASSWORD" --project src/BankingPlatform.Api
```

Check the configured secrets:

``` powershell
dotnet user-secrets list --project src/BankingPlatform.Api
```

The password stays on the development PC instead of being committed to
the repository.

## 8. Frontend first-time setup

Open a separate PowerShell terminal:

``` powershell
cd frontend
npm install
```

This installs the packages from the frontend package configuration.

## 9. Daily startup

After the machine is configured, these are the commands normally needed
each day.

### Terminal 1 --- API

From the repository root:

``` powershell
dotnet run --project src/BankingPlatform.Api
```

Keep this terminal open.

### Terminal 2 --- Workflow Host

If the current solution uses the separate Workflow Host:

``` powershell
dotnet run --project src/BankingPlatform.WorkflowHost
```

Keep this terminal open.

If the current branch does not use the separate Workflow Host, skip this
step.

### Terminal 3 --- Frontend

``` powershell
cd frontend
npm run dev
```

Keep the terminal open and use the local URL displayed by Vite.

## 10. After pulling new changes at the office

After getting the latest code, run from the repository root:

``` powershell
dotnet restore src/BankingPlatform.Api/BankingPlatform.Api.csproj
dotnet build src/BankingPlatform.Api/BankingPlatform.Api.csproj
```

Apply any new committed migrations:

``` powershell
dotnet ef database update --project src/BankingPlatform.Infrastructure --startup-project src/BankingPlatform.Api --context AppDbContext
```

Update frontend packages:

``` powershell
cd frontend
npm install
```

Then start the application normally.

### API

``` powershell
dotnet run --project src/BankingPlatform.Api
```

### Workflow Host, if used

``` powershell
dotnet run --project src/BankingPlatform.WorkflowHost
```

### Frontend

``` powershell
cd frontend
npm run dev
```

## 11. SQL Server

The development SQL Server instance previously used for this project is:

``` text
ARISH\SQLEXPRESS
```

When it appears inside JSON, the backslash must be escaped:

``` text
Server=ARISH\\SQLEXPRESS;...
```

If the office PC uses a different SQL Server instance, use that
machine's correct connection string instead.

## 12. Useful troubleshooting commands

Check the complete .NET installation:

``` powershell
dotnet --info
```

Clean and rebuild:

``` powershell
dotnet clean
dotnet restore src/BankingPlatform.Api/BankingPlatform.Api.csproj
dotnet build src/BankingPlatform.Api/BankingPlatform.Api.csproj
```

Check EF:

``` powershell
dotnet ef --version
```

List migrations:

``` powershell
dotnet ef migrations list --project src/BankingPlatform.Infrastructure --startup-project src/BankingPlatform.Api --context AppDbContext
```

Check Node/npm:

``` powershell
node --version
npm --version
```

Restore frontend packages:

``` powershell
cd frontend
npm install
```

## Quick daily reference

Normally, once everything is configured, use three terminals:

**Terminal 1**

``` powershell
dotnet run --project src/BankingPlatform.Api
```

**Terminal 2 --- only if Workflow Host is required**

``` powershell
dotnet run --project src/BankingPlatform.WorkflowHost
```

**Terminal 3**

``` powershell
cd frontend
npm run dev
```

## Important

-   This setup is for **.NET 8**.
-   Keep `dotnet-ef` aligned with the EF Core major version used by the
    project.
-   Do not commit SMTP passwords or other secrets.
-   Do not create a new migration every time the application starts.
-   Use `dotnet ef database update` to apply migrations that already
    exist.
-   `npm install` is normally required after cloning or when frontend
    dependencies change.
-   The workflow's email toggle is stored with the workflow step
    configuration.
-   The assigned officer's recipient email should be fetched from the
    database rather than hardcoded.
    ////////////////////////////////////////////////




    . Migration apply karo — YE MAIN COMMAND HAI
powershell
cd C:\Projects\Alfalah-Complain-Management
powershell
dotnet ef database update --project .\src\BankingPlatform.Infrastructure --startup-project .\src\BankingPlatform.Api
Kya hota hai:

Saari migrations (Initial + Dynamic Fields + Labels + Attachments + Config + Content) apply hongi

Database bilkul ready ho jayega

Expected output:

text
Build started...
Build succeeded.
Applying migration '20260817184042_InitialBusinessSchema'.
Applying migration '20260922211834_AddWorkflowDynamicFields'.
Applying migration '20260923162627_AddLabelToWorkflowFieldResponses'.
Applying migration '20260926093232_AddWorkflowFieldAttachments'.
Applying migration '20260926175042_AddAttachmentConfigToWorkflowNodeFields'.
Applying migration '20260926203710_MoveAttachmentConfigToSeparateTable'.
Applying migration '20260927075502_StoreAttachmentBytesInDatabase'.
Done.
Done. aa gaya → database ready.

5. Verify karo
powershell
sqlcmd -S 'localhost\SQLEXPRESS04' -C -d BankingPlatform2 -Q "SELECT name FROM sys.tables ORDER BY name"
15 tables dikhni chahiye:

Complaints

Users

WorkflowDefinitions

WorkflowNodeFieldAttachmentConfigs

WorkflowFieldAttachments

aur baaki

6. API chalao
powershell
$env:ASPNETCORE_ENVIRONMENT="Development"
dotnet run --project .\src\BankingPlatform.Api\BankingPlatform.Api.csproj --urls http://localhost:5080
Output: Now listening on: http://localhost:5080

7. Frontend chalao (naya terminal)
powershell
cd <frontend-folder>
npm install
npm run dev
Output: Local: http://localhost:5173

8. Browser me test
http://localhost:5173 kholo → login → complaint banao → workflow designer me field add karo → task complete karo.

Summary — sirf 3 commands
Naye laptop pe ye 3 commands zaroori hain:

powershell
# 1. Connection string check karo (appsettings.json)
# 2. Database banao
sqlcmd -S 'localhost\SQLEXPRESS04' -C -Q "CREATE DATABASE BankingPlatform2"

# 3. Migration apply karo
dotnet ef database update --project .\src\BankingPlatform.Infrastructure --startup-project .\src\BankingPlatform.Api
Bas. Yehi migration ki command hai.

Agar error aaye — 4 common issues
Error	Fix
Could not open a connection to SQL Server	SQL Server ka instance name galat. Get-Service se check karo
SSL Provider: certificate chain...	sqlcmd me -C flag, ya SQLCMDTRUSTSERVERCERTIFICATE=yes set karo
A network-related or instance-specific error	SQL Server service Stopped hai — Start-Service 'MSSQL$SQLEXPRESS04'
Cannot open database "BankingPlatform2"	Pehle CREATE DATABASE chalao
Ek important baat
Agar naye laptop pe SQL Server ka instance naam alag hai (jaise SQLEXPRESS bajaye SQLEXPRESS04), to appsettings.json me connection string update karna zaroori hai:

json
"DefaultConnection": "Server=localhost\\SQLEXPRESS;Database=BankingPlatform2;Trusted_Connection=True;TrustServerCertificate=True;"
Warna migration nahi chalegi.

Ab ye 4 commands chalao — order me
powershell
cd C:\Projects
git clone https://github.com/UmerAayaz/Alfalah-Complain-Management.git
cd Alfalah-Complain-Management
powershell
Get-Service | Where-Object { $_.Name -like "MSSQL*" } | Select-Object Name, Status
powershell
sqlcmd -S 'localhost\SQLEXPRESS04' -C -Q "CREATE DATABASE BankingPlatform2"
powershell
dotnet ef database update --project .\src\BankingPlatform.Infrastructure --startup-project .\src\BankingPlatform.Api
Har output paste karo — agar koi error aaye to exact message bhej do, main fix bata dunga
