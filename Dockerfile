FROM microsoft/dotnet:latest
WORKDIR /app
COPY ./hlcup .
RUN dotnet build
CMD dotnet run

EXPOSE 5000