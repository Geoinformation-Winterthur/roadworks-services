FROM mcr.microsoft.com/dotnet/aspnet:6.0
WORKDIR /app
COPY ./bin/Test/net6.0/publish/ .
ENV DOTNET_CLI_TELEMETRY_OPTOUT=true
ENV ASPNETCORE_ENVIRONMENT=Test
COPY ./resolv.conf /etc/resolv.conf.override
CMD cp /etc/resolv.conf.override /etc/resolv.conf && dotnet roadworks-services.dll
EXPOSE 80