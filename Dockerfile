FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

COPY src/BooksApi/BooksApi.csproj src/BooksApi/
RUN dotnet restore src/BooksApi/BooksApi.csproj

COPY src/BooksApi/ src/BooksApi/
RUN dotnet publish src/BooksApi/BooksApi.csproj -c Release -o /app /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS runtime
WORKDIR /app
COPY --from=build /app .

ENV ASPNETCORE_URLS=http://+:8080
ENV ASPNETCORE_ENVIRONMENT=Production
EXPOSE 8080

ENTRYPOINT ["dotnet", "BooksApi.dll"]
