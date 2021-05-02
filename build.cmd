@echo off

dotnet build Sqlbi.Bravo.sln
dotnet test .\src\Sqlbi.Bravo.Tests\Sqlbi.Bravo.Tests.csproj