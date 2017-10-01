using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using AutoMapper;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using static Newtonsoft.Json.JsonConvert;

// ReSharper disable FieldCanBeMadeReadOnly.Local
// ReSharper disable ArrangeTypeMemberModifiers

namespace hlcup {
    public class Routes {
        AllData _data;
        JsonSerializer _jsonSerializer = new JsonSerializer();
        IMapper _mapper = Mapper.Instance;

        public Routes(AllData data) {
            _data = data;
        }

        public Task Stats(HttpContext ctx) => ctx.Response.WriteAsync(SerializeObject(new {
            users = _data.Users.Length,
            locations = _data.Locations.Length,
            visits = _data.Visits.Length,
        }));

        Task NotFound(HttpContext ctx) {
            ctx.Response.StatusCode = (int) HttpStatusCode.NotFound;
            return Task.CompletedTask;
        }

        Task BadRequest(HttpContext ctx) {
            ctx.Response.StatusCode = (int) HttpStatusCode.BadRequest;
            return Task.CompletedTask;
        }

        Task Json<T>(HttpContext ctx, T obj) {
            ctx.Response.StatusCode = (int) HttpStatusCode.OK;
            ctx.Response.ContentType = "application/json";
            return ctx.Response.WriteAsync(SerializeObject(obj));
        }

        public Task EntityById(HttpContext ctx) {
            if (int.TryParse(ctx.GetRouteValue("id").ToString(), out var id)) {
                switch (ctx.GetRouteValue("entity")) {
                    case "users":
                        if (_data.Users[id] is User user) {
                            return Json(ctx, user);
                        }
                        break;
                    case "locations":
                        if (_data.Locations[id] is Location location) {
                            return Json(ctx, location);
                        }
                        break;
                    case "visits":
                        if (_data.Visits[id] is Visit visit) {
                            return Json(ctx, visit);
                        }
                        break;
                }
            }

            return NotFound(ctx);
        }

        public Task Visits(HttpContext ctx) {
            if (int.TryParse(ctx.GetRouteValue("id").ToString(), out var id)) {
                if (_data.Users[id] is User user) {
                    var visits = (IEnumerable<Visit>) user.Visits.Values;

                    if (ctx.Request.Query.ContainsKey("fromDate")) {
                        if (int.TryParse(ctx.Request.Query["fromDate"], out var fromDate)) {
                            visits = visits.Where(x => x.visited_at > fromDate);
                        }
                        else {
                            return BadRequest(ctx);
                        }
                    }

                    if (ctx.Request.Query.ContainsKey("toDate")) {
                        if (int.TryParse(ctx.Request.Query["toDate"], out var toDate)) {
                            visits = visits.Where(x => x.visited_at < toDate);
                        }
                        else {
                            return BadRequest(ctx);
                        }
                    }

                    var country = ctx.Request.Query["country"];
                    if (!string.IsNullOrEmpty(country)) {
                        visits = visits.Where(x => x.Location?.country == country);
                    }

                    if (int.TryParse(ctx.Request.Query["toDistance"], out var toDistance)) {
                        visits = visits.Where(x => x.Location?.distance < toDistance);
                    }

                    return Json(ctx, visits);
                }
            }

            return NotFound(ctx);
        }

        public Task Avg(HttpContext ctx) {
            if (int.TryParse(ctx.GetRouteValue("id").ToString(), out var id)) {
                if (_data.Locations[id] is Location location) {
                    var visits = (IEnumerable<Visit>) location.Visits;

                    var now = DateTime.UtcNow.Subtract(new DateTime(1970, 1, 1)).TotalSeconds;

                    if (ctx.Request.Query.ContainsKey("fromDate")) {
                        if (int.TryParse(ctx.Request.Query["fromDate"], out var fromDate)) {
                            visits = visits.Where(x => x.visited_at > fromDate);
                        }
                        else {
                            return BadRequest(ctx);
                        }
                    }

                    if (ctx.Request.Query.ContainsKey("toDate")) {
                        if (int.TryParse(ctx.Request.Query["toDate"], out var toDate)) {
                            visits = visits.Where(x => x.visited_at < toDate);
                        }
                        else {
                            return BadRequest(ctx);
                        }
                    }

                    if (ctx.Request.Query.ContainsKey("fromAge")) {
                        if (int.TryParse(ctx.Request.Query["fromAge"], out var fromAge)) {
                            visits = visits.Where(x => {
                                var age = (now - x.User?.birth_date) / 31557600;

                                return age > fromAge;
                            });
                        }
                        else {
                            return BadRequest(ctx);
                        }
                    }

                    if (ctx.Request.Query.ContainsKey("toAge")) {
                        if (int.TryParse(ctx.Request.Query["toAge"], out var toAge)) {
                            visits = visits.Where(x => {
                                var age = (now - x.User?.birth_date) / 31557600;

                                return age < toAge;
                            });
                        }
                        else {
                            return BadRequest(ctx);
                        }
                    }

                    if (ctx.Request.Query.ContainsKey("gender")) {
                        var gender = ctx.Request.Query["toAge"];
                        if (gender == "m" || gender == "f") {
                            visits = visits.Where(x => x.User.gender == gender);
                        }
                        else {
                            return BadRequest(ctx);
                        }
                    }

                    return Json(ctx, new {
                        avg = $"{visits.Average(x => x.mark):N2}"
                    });
                }
            }

            return NotFound(ctx);
        }

        public Task Update(HttpContext ctx) {
            if (int.TryParse(ctx.GetRouteValue("id").ToString(), out var id)) {
                switch (ctx.GetRouteValue("entity")) {
                    case "users":
                        if (_data.Users[id] is User user) {
                            var update = ReadFromBody<Dictionary<string, JToken>>();
                            user.Update(update, _data);
                        }
                        break;
                    case "locations":
                        if (_data.Locations[id] is Location location) {
                            var update = ReadFromBody<Dictionary<string, JToken>>();
                            location.Update(update, _data);
                        }
                        break;
                    case "visits":
                        if (_data.Visits[id] is Visit visit) {
                            var update = ReadFromBody<Dictionary<string, JToken>>();
                            visit.Update(update, _data);
                        }
                        break;
                }
            }

            return NotFound(ctx);

            T ReadFromBody<T>() {
                using (var reader = new StreamReader(ctx.Request.Body))
                using (var jtr = new JsonTextReader(reader)) {
                    return _jsonSerializer.Deserialize<T>(jtr);
                }
            }
        }

        public Task Create(HttpContext ctx) {
            return NotFound(ctx);
        }
    }
}