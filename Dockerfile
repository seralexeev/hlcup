FROM microsoft/dotnet:latest
WORKDIR /build
COPY ./hlcup .
RUN dotnet publish -c release -o out

FROM microsoft/dotnet:runtime
RUN apt-get update && apt-get install unzip
WORKDIR /app
COPY --from=0 /build/run.sh .
RUN ["chmod", "+x", "run.sh"]
COPY --from=0 /build/out .
CMD ["./run.sh"]
EXPOSE 80