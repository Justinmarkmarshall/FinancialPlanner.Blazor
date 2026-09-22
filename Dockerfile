# syntax=docker/dockerfile:1
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY . .
# The token exists only for this restore command, not in an image layer or build argument.
RUN --mount=type=secret,id=github_nuget_token,required=true \
    export NuGetPackageSourceCredentials_github="Username=Justinmarkmarshall;Password=$(cat /run/secrets/github_nuget_token);ValidAuthenticationTypes=Basic" && \
    dotnet restore src/FinancialPlanner.Blazor/FinancialPlanner.Blazor/FinancialPlanner.Blazor.csproj --configfile build/nuget.config
RUN dotnet publish src/FinancialPlanner.Blazor/FinancialPlanner.Blazor/FinancialPlanner.Blazor.csproj -c Release --no-restore -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:10.0
WORKDIR /app
COPY --from=build /app/publish .
ENV ASPNETCORE_URLS=http://+:80
EXPOSE 80
ENTRYPOINT ["dotnet","FinancialPlanner.Blazor.dll"]
