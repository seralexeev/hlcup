using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using static Newtonsoft.Json.JsonConvert;

namespace hlcup {
    public class Routes {
        private readonly AllData _data;

        public Routes(AllData data) {
            _data = data;
        }

        public Task Stats(HttpContext ctx) => ctx.Response.WriteAsync(SerializeObject(new {
            users = _data.Users.Count,
            locations = _data.Locations.Count,
            visits = _data.Visits.Count,
        }));

        Task NotFound(HttpContext ctx) {
            ctx.Response.StatusCode = (int) HttpStatusCode.NotFound;
            return Task.CompletedTask;
        }

        Task BadRequest(HttpContext ctx) {
            ctx.Response.StatusCode = (int) HttpStatusCode.BadRequest;
            return Task.CompletedTask;
        }

        public Task EntityById(HttpContext ctx) {
            if (int.TryParse(ctx.GetRouteValue("id").ToString(), out var id)) {
                switch (ctx.GetRouteValue("entity")) {
                    case "users":
                        if (_data.Users.TryGetValue(id, out var user)) {
                            return ctx.Response.WriteAsync(SerializeObject(user));
                        }
                        break;
                    case "locations":
                        if (_data.Locations.TryGetValue(id, out var location)) {
                            return ctx.Response.WriteAsync(SerializeObject(location));
                        }
                        break;
                    case "visits":
                        if (_data.Visits.TryGetValue(id, out var visit)) {
                            return ctx.Response.WriteAsync(SerializeObject(visit));
                        }
                        break;
                }
            }

            return NotFound(ctx);
        }

        public Task Visits(HttpContext ctx) {
            if (int.TryParse(ctx.GetRouteValue("id").ToString(), out var id)) {
                if (_data.Users.TryGetValue(id, out var user)) {
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

                    return ctx.Response.WriteAsync(SerializeObject(visits));
                }
            }

            return NotFound(ctx);
        }

        public Task Avg(HttpContext ctx) {
            if (int.TryParse(ctx.GetRouteValue("id").ToString(), out var id)) {
                if (_data.Locations.TryGetValue(id, out var location)) {
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

                    return ctx.Response.WriteAsync($"{visits.Average(x => x.mark):N2}");
                }
            }

            return NotFound(ctx);
        }
    }
}