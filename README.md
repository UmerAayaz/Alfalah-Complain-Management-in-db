dotnet ef database update --project .\src\BankingPlatform.Infrastructure --startup-project .\src\BankingPlatform.Api



$env:ASPNETCORE_ENVIRONMENT="Development"
dotnet run --project .\src\BankingPlatform.Api --urls http://localhost:5080


$env:ASPNETCORE_ENVIRONMENT="Development"
dotnet run --project .\src\BankingPlatform.WorkflowHost --urls http://localhost:5081


dotnet --version
node -v
npm -v
git --version



dotnet tool install --global dotnet-ef --version 8.0.0
dotnet tool update --global dotnet-ef --version 8.0.0

dotnet restore .\BankingPlatform.slnx

dotnet ef database update --project .\src\BankingPlatform.Infrastructure --startup-project .\src\BankingPlatform.Api

$env:ASPNETCORE_ENVIRONMENT = "Development"
dotnet run --project .\src\BankingPlatform.Api --urls http://localhost:5080

$env:ASPNETCORE_ENVIRONMENT = "Development"
dotnet run --project .\src\BankingPlatform.WorkflowHost --urls http://localhost:5081

cd C:\Projects\Alfalah-Complain-Management-in-db\frontend
npm install
npm run dev
