using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

// ReSharper disable ArrangeTypeMemberModifiers

namespace hlcup {
    public class Program {
        public static void Main(string[] args) {
            var dataPath = args.Length > 0 ? args[0] : "/data";
            var port = args.Length > 1 ? args[1] : "80";

            var isTest = ReadOptions($"{dataPath}/options.txt");
            ScanDir(dataPath);
            GetHostBuilder(port).Build().Run();
        }

        public static IWebHostBuilder GetHostBuilder(string port = "80") => new WebHostBuilder()
            .UseKestrel()
            .UseUrls($"http://*:{port}")
            .Configure(cfg => cfg.Run(ctx => HandleRequest(ctx)));

        private static Task HandleRequest(HttpContext ctx) {
            var body = Routes.empty;

            var parts = ctx.Request.Path.Value.Split('/', StringSplitOptions.RemoveEmptyEntries);
            var l = parts.Length;
            if (l < 2 || parts[1].Length < 1 || parts[2].Length < 1) { }

            var p0 = parts[0][0];
            var p1 = parts[1][0];

            switch (ctx.Request.Method) {
                case "GET"
                when l == 2 && (p0 == 'u' || p0 == 'l' || p0 == 'v'):
                    body = Routes.EntityById(ctx, p0, parts[1]);
                    break;

                case "GET"
                when l == 3 && p0 == 'u' && parts[2].Length > 0 && parts[2][0] == 'v':
                    body = Routes.Visits(ctx, parts[1]);
                    break;

                case "GET"
                when l == 3 && p0 == 'l' && parts[2].Length > 0 && parts[2][0] == 'a':
                    body = Routes.Avg(ctx, parts[1]);
                    break;

                case "POST"
                when l == 2 && p1 == 'n' && (p0 == 'u' || p0 == 'l' || p0 == 'v'):
                    body = Routes.Create(ctx, p0);
                    break;

                case "POST"
                when l == 2 && (p0 == 'u' || p0 == 'l' || p0 == 'v'):
                    body = Routes.Update(ctx, p0, parts[1]);
                    break;

                default:
                    ctx.Response.StatusCode = StatusCodes.Status404NotFound;
                    break;
            }


            if (body.Length > 0) {
                ctx.Response.ContentLength = body.Length;
                ctx.Response.Body.Write(body, 0, body.Length);
            }

            return Task.CompletedTask;
        }

        static bool ReadOptions(string optsFile) {
            var opts = File.ReadLines(optsFile).ToArray();
            AllData.currentDate = int.Parse(opts[0]);
            return opts[1] == "0";
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