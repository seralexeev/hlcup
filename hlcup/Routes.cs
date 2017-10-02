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

        static Task NotFound(HttpContext ctx) {
            ctx.Response.Headers.Clear();
            ctx.Response.StatusCode = (int) HttpStatusCode.NotFound;
            return Task.CompletedTask;
        }

        static Task BadRequest(HttpContext ctx) {
            ctx.Response.Headers.Clear();
            ctx.Response.StatusCode = (int) HttpStatusCode.BadRequest;
            return Task.CompletedTask;
        }

        static Task Json<T>(HttpContext ctx, T obj) {
            ctx.Response.StatusCode = (int) HttpStatusCode.OK;
            ctx.Response.ContentType = "application/json";
            Utf8Json.JsonSerializer.Serialize(ctx.Response.Body, obj);

            return Task.CompletedTask;
        }

        static Task EmptyJson(HttpContext ctx) {
            ctx.Response.StatusCode = (int) HttpStatusCode.OK;
            ctx.Response.ContentType = "application/json";
            return ctx.Response.WriteAsync("{}");
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
                    var visits = (IEnumerable<Visit>) user.Visits;

                    if (ctx.Request.Query.TryGetValue("fromDate", out var fromDateStr)) {
                        if (int.TryParse(fromDateStr, out var fromDate)) {
                            visits = visits.Where(x => x.visited_at > fromDate);
                        }
                        else {
                            return BadRequest(ctx);
                        }
                    }

                    if (ctx.Request.Query.TryGetValue("toDate", out var toDateStr)) {
                        if (int.TryParse(toDateStr, out var toDate)) {
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

                    if (ctx.Request.Query.TryGetValue("toDistance", out var toDistanceStr)) {
                        if (int.TryParse(toDistanceStr, out var toDistance)) {
                            visits = visits.Where(x => x.Location?.distance < toDistance);
                        }
                        else {
                            return BadRequest(ctx);
                        }
                    }


                    return Json(ctx, new VisitsVm {
                        visits = visits.OrderBy(x => x.visited_at).Select(x => new VisitsVm.VisitVm {
                            mark = x.mark.Value,
                            visited_at = x.visited_at.Value,
                            place = x.Location.place
                        })
                    });
                }
            }

            return NotFound(ctx);
        }

        public Task Avg(HttpContext ctx) {
            if (int.TryParse(ctx.GetRouteValue("id").ToString(), out var id)) {
                if (_data.Locations[id] is Location location) {
                    var visits = (IEnumerable<Visit>) location.Visits;

                    var now = DateTime.UtcNow.Subtract(new DateTime(1970, 1, 1)).TotalSeconds;

                    if (ctx.Request.Query.TryGetValue("fromDate", out var fromDateStr)) {
                        if (int.TryParse(fromDateStr, out var fromDate)) {
                            visits = visits.Where(x => x.visited_at > fromDate);
                        }
                        else {
                            return BadRequest(ctx);
                        }
                    }

                    if (ctx.Request.Query.TryGetValue("toDate", out var toDateStr)) {
                        if (int.TryParse(toDateStr, out var toDate)) {
                            visits = visits.Where(x => x.visited_at < toDate);
                        }
                        else {
                            return BadRequest(ctx);
                        }
                    }

                    if (ctx.Request.Query.TryGetValue("fromAge", out var fromAgeStr)) {
                        if (int.TryParse(fromAgeStr, out var fromAge)) {
                            visits = visits.Where(x => {
                                var age = (now - x.User?.birth_date) / 31557600;

                                return age > fromAge;
                            });
                        }
                        else {
                            return BadRequest(ctx);
                        }
                    }

                    if (ctx.Request.Query.TryGetValue("toAge", out var toAgeStr)) {
                        if (int.TryParse(toAgeStr, out var toAge)) {
                            visits = visits.Where(x => {
                                var age = (now - x.User?.birth_date) / 31557600;

                                return age < toAge;
                            });
                        }
                        else {
                            return BadRequest(ctx);
                        }
                    }

                    if (ctx.Request.Query.TryGetValue("gender", out var gender)) {
                        if (gender == "m" || gender == "f") {
                            var gchar = gender[0][0];
                            visits = visits.Where(x => x.User.gender == gchar);
                        }
                        else {
                            return BadRequest(ctx);
                        }
                    }

                    return Json(ctx, new Avg {
                        avg = Math.Round(Average(), 5)
                    });

                    double Average() {
                        using (var enumerator = visits.GetEnumerator()) {
                            if (!enumerator.MoveNext())
                                return 0;
                            long num1 = enumerator.Current.mark.Value;
                            long num2 = 1;
                            while (enumerator.MoveNext()) {
                                checked {
                                    num1 += enumerator.Current.mark.Value;
                                }
                                checked {
                                    ++num2;
                                }
                            }
                            return num1 / (double) num2;
                        }
                    }
                }
            }

            return NotFound(ctx);
        }

        public Task Update(HttpContext ctx) {
            if (int.TryParse(ctx.GetRouteValue("id").ToString(), out var id)) {
                Dictionary<string, JValue> update;
                using (var reader = new StreamReader(ctx.Request.Body))
                using (var jtr = new JsonTextReader(reader)) {
                    update = _jsonSerializer.Deserialize<Dictionary<string, JValue>>(jtr);
                }

                if (update.Any(x => x.Value?.Value == null)) {
                    return BadRequest(ctx);
                }

                switch (ctx.GetRouteValue("entity")) {
                    case "users":
                        if (_data.Users[id] is User user) {
                            user.Update(update, _data);
                            return EmptyJson(ctx);
                        }
                        return NotFound(ctx);

                    case "locations":
                        if (_data.Locations[id] is Location location) {
                            location.Update(update, _data);
                            return EmptyJson(ctx);
                        }

                        return NotFound(ctx);
                    case "visits":
                        if (_data.Visits[id] is Visit visit) {
                            visit.Update(update, _data);
                            return EmptyJson(ctx);
                        }
                        return NotFound(ctx);
                }
            }

            return BadRequest(ctx);
        }

        public Task Create(HttpContext ctx) {
            switch (ctx.GetRouteValue("entity")) {
                case "users":
                    var user = ReadFromBody<User>(ctx);
                    if (user.IsValid()) {
                        _data.Users[user.id.Value] = user;
                        return EmptyJson(ctx);
                    }

                    return BadRequest(ctx);

                case "locations":
                    var location = ReadFromBody<Location>(ctx);
                    if (location.IsValid()) {
                        _data.Locations[location.id.Value] = location;
                        return EmptyJson(ctx);
                    }

                    return BadRequest(ctx);

                case "visits":
                    var visit = ReadFromBody<Visit>(ctx);
                    if (visit.IsValid()) {
                        _data.Visits[visit.id.Value] = visit;

                        if (_data.Locations[visit.location.Value] is Location loc) {
                            visit.Location = loc;
                            visit.Location.Visits.Add(visit);
                        }

                        if (_data.Users[visit.user.Value] is User usr) {
                            usr.Visits.Add(visit);
                            visit.User = usr;
                        }

                        return EmptyJson(ctx);
                    }
                    return BadRequest(ctx);
            }

            return BadRequest(ctx);
        }

//        T ReadFromBody<T>(HttpContext ctx) {
//            using (var reader = new StreamReader(ctx.Request.Body))
//            using (var jtr = new JsonTextReader(reader)) {
//                return _jsonSerializer.Deserialize<T>(jtr);
//            }
//        }

        T ReadFromBody<T>(HttpContext ctx) => Utf8Json.JsonSerializer.Deserialize<T>(ctx.Request.Body);
    }

    public class VisitsVm {
        public IEnumerable<VisitVm> visits;

        public class VisitVm {
            public int mark;
            public int visited_at;
            public string place;
        }
    }

    public class Avg {
        public double avg;
    }
}