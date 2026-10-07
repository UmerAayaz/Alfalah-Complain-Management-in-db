dotnet ef database update --project .\src\BankingPlatform.Infrastructure --startup-project .\src\BankingPlatform.Api



$env:ASPNETCORE_ENVIRONMENT="Development"
dotnet run --project .\src\BankingPlatform.Api --urls http://localhost:5080


$env:ASPNETCORE_ENVIRONMENT="Development"
dotnet run --project .\src\BankingPlatform.WorkflowHost --urls http://localhost:5081
