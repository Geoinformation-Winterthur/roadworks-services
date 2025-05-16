FROM mcr.microsoft.com/dotnet/aspnet:8.0-alpine
WORKDIR /app
COPY ./bin/Test/net8.0/publish/ .
ENV DOTNET_CLI_TELEMETRY_OPTOUT=true
ENV ASPNETCORE_ENVIRONMENT=Test
USER root
COPY ./resolv.conf /etc/resolv.conf
USER app
ENTRYPOINT ["dotnet", "roadworks-services.dll"]
EXPOSE 8080