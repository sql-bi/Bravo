@echo off

dotnet build .\src\Sqlbi.Bravo.sln /nologo
dotnet test .\src\Sqlbi.Bravo.Tests\Sqlbi.Bravo.Tests.csproj