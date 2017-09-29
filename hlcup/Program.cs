using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using static hlcup.Extensions;

// ReSharper disable ArrangeTypeMemberModifiers

namespace hlcup {
    public class Program {
        public static void Main(string[] args) {
            var program = new Program();
            var data = program.LoadData("../data");
            program.GetHostBuilder(new Routes(data)).Build().Run();
        }

        public AllData LoadData(string dir) {
            var (genTime, isTest) = ReadOptions($"{dir}/options.txt");
            println(isTest ? "TEST" : "RAITING");

            return ScanDir(dir);
        }

        public IWebHostBuilder GetHostBuilder(Routes routes) => new WebHostBuilder()
            .UseKestrel()
            .UseUrls("http://*:5000")
            .ConfigureServices(cfg => cfg.AddRouting())
            .Configure(cfg => cfg.UseRouter(new RouteBuilder(cfg)
                .MapGet("{entity}/{id}", routes.EntityById)
                .MapGet("users/{id}/visits", routes.Visits)
                .MapGet("locations/{id}/avg", routes.Avg)
                .MapGet("stats", routes.Stats)
                .Build()));

        (long, bool) ReadOptions(string optsFile) {
            var opts = File.ReadLines(optsFile).ToArray();
            return (int.Parse(opts[0]), opts[1] == "0" ? true : false);
        }

        AllData ScanDir(string dir) {
            var users = new ConcurrentDictionary<int, User>();
            var locations = new ConcurrentDictionary<int, Location>();
            var visits = new ConcurrentDictionary<int, Visit>();

            foreach (var file in Directory.GetFiles(dir).OrderBy(s => {
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
            })) {
                if (file.StartsWith($"{dir}/users")) {
                    foreach (var user in ReadData<User>(file, "users")) {
                        users.AddOrUpdate(user.id, user, (x, u) => u);
                    }
                }
                else if (file.StartsWith($"{dir}/locations")) {
                    foreach (var location in ReadData<Location>(file, "locations")) {
                        locations.AddOrUpdate(location.id, location, (x, u) => u);
                    }
                }
                else if (file.StartsWith($"{dir}/visits")) {
                    foreach (var visit in ReadData<Visit>(file, "visits")) {
                        visits.AddOrUpdate(visit.id, visit, (x, u) => u);

                        if (locations.TryGetValue(visit.location, out var location)) {
                            visit.Location = location;
                            visit.Location.Visits.Add(visit);
                        }

                        if (users.TryGetValue(visit.user, out var user)) {
                            user.Visits.Add(visit.visited_at, visit);
                            visit.User = user;
                        }
                    }
                }
            }

            return new AllData {
                Users = users,
                Locations = locations,
                Visits = visits
            };
        }

        IEnumerable<T> ReadData<T>(string file, string prop) {
            using (var sr = new StreamReader(File.Open(file, FileMode.Open)))
            using (var jsonTextReader = new JsonTextReader(sr)) {
                return (new JsonSerializer()
                    .Deserialize<JObject>(jsonTextReader)[prop]).ToObject<IEnumerable<T>>();
            }
        }
    }
}