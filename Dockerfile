FROM mcr.microsoft.com/dotnet/core/sdk:3.0.100-preview8

COPY SmartGarbageCollection/bin/Release/netcoreapp3.0/publish/ app/

ENTRYPOINT ["dotnet", "app/SmartGarbageCollection.dll"]