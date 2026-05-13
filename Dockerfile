FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY . .
RUN dotnet publish src/Frontend/AssetFlow.BlazorUI/AssetFlow.BlazorUI.csproj \
    -c Release -o /app/publish \
    && echo "=== CONTENU PUBLISH ===" \
    && ls /app/publish/ \
    && echo "=== CONTENU WWWROOT ===" \
    && ls /app/publish/wwwroot/ \
    && echo "=== FRAMEWORK ===" \
    && ls /app/publish/wwwroot/_framework/ | head -5

FROM nginx:alpine
COPY --from=build /app/publish/wwwroot /usr/share/nginx/html
COPY nginx.conf /etc/nginx/nginx.conf
EXPOSE 80