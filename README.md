# oai
OCR - AI - Invoice

## Database migrations

Run these commands from the repository root:

```powershell
cd OAI
dotnet ef migrations add ConfigureMoneyPrecision --project OAI.Infrastructure\OAI.Infrastructure.csproj --startup-project OAI.Web\OAI.Web.csproj
dotnet ef database update --project OAI.Infrastructure\OAI.Infrastructure.csproj --startup-project OAI.Web\OAI.Web.csproj
```
