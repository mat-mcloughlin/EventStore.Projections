@echo Off

echo %~nx0: Building...
dotnet build --configuration Release /nologo || goto :error

echo %~nx0: Testing...
dotnet test --configuration Release --no-build /nologo || goto :error
