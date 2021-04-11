@echo off

dotnet build .\Sqlbi.Bravo.sln /nologo
dotnet test .\tests\Sqlbi.Bravo.Tests\Sqlbi.Bravo.Tests.csproj