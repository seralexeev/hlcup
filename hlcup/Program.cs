using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using static hlcup.Extensions;

// ReSharper disable ArrangeTypeMemberModifiers

namespace hlcup {
    public class Program {
        public static void Main(string[] args) {
            var dataPath = args.Length > 0 ? args[0] : "/data";
            var port = args.Length > 1 ? args[1] : "80";

            LoadData(dataPath);
            
            GetHostBuilder(port).Build().Run();
        }

        public static void LoadData(string dir) => (AllData.currentDate, _) = ReadOptions($"{dir}/options.txt");

        public static IWebHostBuilder GetHostBuilder(string port = "80") => new WebHostBuilder()
            .UseKestrel(options => {
                options.AllowSynchronousIO = true;
                options.Limits.MaxConcurrentConnections = null;
            })
            .UseLibuv(options => { options.ThreadCount = 2; })
            .UseUrls($"http://*:{port}")
            .Configure(cfg => {
                cfg.UseResponseBuffering();
                cfg.Use((context, _) => {
                    try {
                        return HandleRequest();
                    } catch {
                        context.Response.StatusCode = (int) HttpStatusCode.BadRequest;
                        return Task.CompletedTask;
                    }

                    Task HandleRequest() {
                        var parts = context.Request.Path.Value.Trim('/').ToLower().Split('/');
                        switch (context.Request.Method) {
                            case "GET"
                            when parts.Length == 2 &&
                                 (parts[0] == "users" || parts[0] == "locations" || parts[0] == "visits"):
                                return Routes.EntityById(context, parts[0], parts[1]);
                            case "GET"
                            when parts.Length == 3 && parts[0] == "users" && parts[2] == "visits":
                                return Routes.Visits(context, parts[1]);
                            case "GET"
                            when parts.Length == 3 && parts[0] == "locations" && parts[2] == "avg":
                                return Routes.Avg(context, parts[1]);
                            case "POST"
                            when parts.Length == 2 &&
                                 (parts[0] == "users" || parts[0] == "locations" || parts[0] == "visits") &&
                                 parts[1] == "new":
                                return Routes.Create(context, parts[0]);
                            case "POST"
                            when parts.Length == 2 &&
                                 (parts[0] == "users" || parts[0] == "locations" || parts[0] == "visits"):
                                return Routes.Update(context, parts[0], parts[1]);
                            default:
                                context.Response.StatusCode = (int) HttpStatusCode.NotFound;
                                return Task.CompletedTask;
                        }
                    }
                });
            });

        static (long, bool) ReadOptions(string optsFile) {
            var opts = File.ReadLines(optsFile).ToArray();
            return (int.Parse(opts[0]), opts[1] == "0");
        }

        static void ScanDir(string dir) {
            AllData.users = new User[1_500_200];
            AllData.locations = new Location[1_000_000];
            AllData.visits = new Visit[10_500_000];

            foreach (var file in Directory.GetFiles(dir).OrderBy(FilesOrder)) {
                if (file.StartsWith($"{dir}/users")) {
                    foreach (var user in ReadData<User>(file, "users")) {
                        AllData.users[user.id.Value] = user;
                        user.CalculateAge();
                    }
                } else if (file.StartsWith($"{dir}/locations")) {
                    foreach (var location in ReadData<Location>(file, "locations")) {
                        AllData.locations[location.id.Value] = location;
                    }
                } else if (file.StartsWith($"{dir}/visits")) {
                    foreach (var visit in ReadData<Visit>(file, "visits")) {
                        AllData.visits[visit.id.Value] = visit;

                        if (AllData.locations[visit.location.Value] is Location location) {
                            visit.Location = location;
                            visit.Location.Visits.Add(visit);
                        }

                        if (AllData.users[visit.user.Value] is User user) {
                            user.Visits.Add(visit);
                            visit.User = user;
                        }
                    }
                }
            }

            GC.Collect(2, GCCollectionMode.Forced);

            int FilesOrder(string s) {
                if (s.StartsWith($"{dir}/users")) {
                    return 0;
                }

                if (s.StartsWith($"{dir}/locations")) {
                    return 1;
                }

                if (s.StartsWith($"{dir}/visits")) {
                    return 2;
                }

                return 10;
            }
        }

        static IEnumerable<T> ReadData<T>(string file, string prop) {
            using (var sr = new StreamReader(File.Open(file, FileMode.Open)))
            using (var jsonTextReader = new JsonTextReader(sr)) {
                return new JsonSerializer()
                    .Deserialize<JObject>(jsonTextReader)[prop].ToObject<IEnumerable<T>>();
            }
        }
    }
}