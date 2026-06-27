FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src
COPY ["TodoApp.Api/TodoApp.Api.csproj", "TodoApp.Api/"]
RUN dotnet restore "TodoApp.Api/TodoApp.Api.csproj"
COPY . .
WORKDIR /src/TodoApp.Api
RUN dotnet publish -c Release -o /app/publish --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS runtime
WORKDIR /app
COPY --from=build /app/publish .
ENV ASPNETCORE_URLS=http://+:5000
EXPOSE 5000
ENTRYPOINT ["dotnet", "TodoApp.Api.dll"]
