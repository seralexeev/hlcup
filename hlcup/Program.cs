using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.Server.Kestrel.Transport.Abstractions.Internal;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using static hlcup.Extensions;

// ReSharper disable ArrangeTypeMemberModifiers

namespace hlcup {
    public class Program {
        public static void Main(string[] args) {
            var dataPath = args.Length > 0 ? args[0] : "/data";
            var port = args.Length > 1 ? args[1] : "80";

            var program = new Program();
            var data = program.LoadData(dataPath);
            (Routes.Data, Routes.CurrentDate) = data;
            program.GetHostBuilder(port).Build().Run();
        }

        public (AllData, long genTime) LoadData(string dir) {
            var (genTime, isTest) = ReadOptions($"{dir}/options.txt");
            println(isTest ? "TEST" : "RAITING");

            return (ScanDir(dir), genTime);
        }

        public IWebHostBuilder GetHostBuilder(string port = "80") => new WebHostBuilder()
            .UseKestrel(options => {
                options.AllowSynchronousIO = true;
//                options.ApplicationSchedulingMode = SchedulingMode.Inline;
//                options.Limits.KeepAliveTimeout = TimeSpan.FromMilliseconds(1);
                options.Limits.MaxConcurrentConnections = null;
            })
            .UseLibuv(options => {
                options.ThreadCount = 2;
            })
            .UseUrls($"http://*:{port}")
            .ConfigureServices(cfg => cfg.AddRouting())
            .Configure(cfg => {
                cfg.UseResponseBuffering();
                cfg.Use((context, func) => {
                    try {
                        func().Wait();
                    } catch (Exception) {
                        context.Response.StatusCode = (int) HttpStatusCode.BadRequest;
                    }
                    return Task.CompletedTask;
                });
                cfg.UseRouter(new RouteBuilder(cfg)
                    .MapGet("{entity}/{id}", Routes.EntityById)
                    .MapGet("users/{id}/visits", Routes.Visits)
                    .MapGet("locations/{id}/avg", Routes.Avg)
                    .MapGet("stats", Routes.Stats)
                    .MapPost("{entity}/new", Routes.Create)
                    .MapPost("{entity}/{id}", Routes.Update)
                    .Build());
            });

        (long, bool) ReadOptions(string optsFile) {
            var opts = File.ReadLines(optsFile).ToArray();
            return (int.Parse(opts[0]), opts[1] == "0");
        }

        AllData ScanDir(string dir) {
            var users = new User[1_500_200];
            var locations = new Location[1_000_000];
            var visits = new Visit[10_500_000];

            var rusers = 0;
            var rlocations = 0;
            var rvisits = 0;

            foreach (var file in Directory.GetFiles(dir).OrderBy(FilesOrder)) {
                if (file.StartsWith($"{dir}/users")) {
                    foreach (var user in ReadData<User>(file, "users")) {
                        users[user.id.Value] = user;
                        user.UpdateCache();
                        rusers++;
                    }
                } else if (file.StartsWith($"{dir}/locations")) {
                    foreach (var location in ReadData<Location>(file, "locations")) {
                        locations[location.id.Value] = location;
                        location.UpdateCache();
                        rlocations++;
                    }
                } else if (file.StartsWith($"{dir}/visits")) {
                    foreach (var visit in ReadData<Visit>(file, "visits")) {
                        visits[visit.id.Value] = visit;
                        rvisits++;

                        if (locations[visit.location.Value] is Location location) {
                            visit.Location = location;
                            visit.Location.Visits.Add(visit);
                        }

                        if (users[visit.user.Value] is User user) {
                            user.Visits.Add(visit);
                            visit.User = user;
                        }

//                        visit.UpdateCache();
                    }
                }
            }

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

            println($"users={rusers}");
            println($"locations={rlocations}");
            println($"vists={rvisits}");

            GC.Collect(2, GCCollectionMode.Forced);

            return new AllData {
                Users = users,
                Locations = locations,
                Visits = visits
            };
        }

        IEnumerable<T> ReadData<T>(string file, string prop) {
            using (var sr = new StreamReader(File.Open(file, FileMode.Open)))
            using (var jsonTextReader = new JsonTextReader(sr)) {
                return new JsonSerializer()
                    .Deserialize<JObject>(jsonTextReader)[prop].ToObject<IEnumerable<T>>();
            }
        }
    }
}