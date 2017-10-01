﻿using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using AutoMapper;
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
        public Program() {
            Mapper.Initialize(cfg => {
                cfg.CreateMap<User, User>()
                    .ForMember(x => x.id, x => x.Ignore())
                    .ForMember(x => x.Visits, x => x.Ignore());

                cfg.CreateMap<Location, Location>()
                    .ForMember(x => x.id, x => x.Ignore())
                    .ForMember(x => x.Visits, x => x.Ignore());

                cfg.CreateMap<Visit, Visit>()
                    .ForMember(x => x.id, x => x.Ignore())
                    .ForMember(x => x.Location, x => x.Ignore())
                    .ForMember(x => x.User, x => x.Ignore());
            });
        }

        public static void Main(string[] args) {
            var dataPath = args.Length > 0 ? args[0] : "/data";

            var program = new Program();
            var data = program.LoadData(dataPath);
            program.GetHostBuilder(new Routes(data)).Build().Run();
        }

        public AllData LoadData(string dir) {
            var (genTime, isTest) = ReadOptions($"{dir}/options.txt");
            println(isTest ? "TEST" : "RAITING");

            return ScanDir(dir);
        }

        public IWebHostBuilder GetHostBuilder(Routes routes) => new WebHostBuilder()
            .UseKestrel()
            .UseUrls("http://*:80")
            .ConfigureServices(cfg => cfg.AddRouting())
            .Configure(cfg => {
                cfg.UseResponseBuffering();
                cfg.UseRouter(new RouteBuilder(cfg)
                    .MapGet("{entity}/{id}", routes.EntityById)
                    .MapGet("users/{id}/visits", routes.Visits)
                    .MapGet("locations/{id}/avg", routes.Avg)
                    .MapGet("stats", routes.Stats)
                    .MapPost("{entity}/{id}", routes.Update)
                    .MapPost("{entity}/new", routes.Create)
                    .Build());
            });

        (long, bool) ReadOptions(string optsFile) {
            var opts = File.ReadLines(optsFile).ToArray();
            return (int.Parse(opts[0]), opts[1] == "0");
        }

        AllData ScanDir(string dir) {
            var users = new User[2_000_000];
            var locations = new Location[1_000_000];
            var visits = new Visit[11_000_000];

            var rusers = 0;
            var rlocations = 0;
            var rvisits = 0;

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
                        users[user.id] = user;
                        rusers++;
                    }
                }
                else if (file.StartsWith($"{dir}/locations")) {
                    foreach (var location in ReadData<Location>(file, "locations")) {
                        locations[location.id] = location;
                        rlocations++;
                    }
                }
                else if (file.StartsWith($"{dir}/visits")) {
                    foreach (var visit in ReadData<Visit>(file, "visits")) {
                        visits[visit.id] = visit;
                        rvisits++;

                        if (locations[visit.location] is Location location) {
                            visit.Location = location;
                            visit.Location.Visits.Add(visit);
                        }

                        if (users[visit.user] is User user) {
                            user.Visits.Add(visit.visited_at, visit);
                            visit.User = user;
                        }
                    }
                }
            }

            println($"users={rusers}");
            println($"locations={rlocations}");
            println($"vists={rvisits}");

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