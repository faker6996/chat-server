# --- GIAI ĐOẠN 1: BUILD ---
# Sử dụng .NET 9 SDK image để build dự án.
# Đặt tên cho giai đoạn này là 'build'.
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

# Sao chép file .csproj và restore các dependencies trước.
# Điều này tận dụng cơ chế cache của Docker. Nếu các file project không đổi,
# layer này sẽ không cần chạy lại, giúp build nhanh hơn.
# !!! QUAN TRỌNG: Hãy đổi "ChatServer.csproj" thành tên file .csproj thực tế của bạn.
COPY ["ChatServer.csproj", "."]
RUN dotnet restore "./ChatServer.csproj"

# Sao chép toàn bộ source code còn lại và build.
COPY . .
WORKDIR "/src/."
# !!! QUAN TRỌNG: Hãy đổi "ChatServer.csproj" thành tên file .csproj thực tế của bạn.
RUN dotnet build "ChatServer.csproj" -c Release -o /app/build

# --- GIAI ĐOẠN 2: PUBLISH ---
# Tiếp tục từ giai đoạn build để publish ứng dụng.
FROM build AS publish
# !!! QUAN TRỌNG: Hãy đổi "ChatServer.csproj" thành tên file .csproj thực tế của bạn.
RUN dotnet publish "ChatServer.csproj" -c Release -o /app/publish /p:UseAppHost=false

# --- GIAI ĐOẠN 3: FINAL ---
# Sử dụng image ASP.NET runtime nhẹ hơn nhiều cho image cuối cùng.
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS final
WORKDIR /app

# Mở cổng 5077, khớp với biến môi trường ASPNETCORE_URLS trong docker-compose.yml.
EXPOSE 5077

# Sao chép các file đã được publish từ giai đoạn 'publish' vào image cuối cùng.
COPY --from=publish /app/publish .

# Định nghĩa lệnh sẽ chạy khi container khởi động.
# !!! QUAN TRỌNG: Hãy đổi "ChatServer.dll" thành tên file .dll đầu ra của bạn.
ENTRYPOINT ["dotnet", "ChatServer.dll"]
