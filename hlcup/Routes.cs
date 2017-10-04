using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using static Newtonsoft.Json.JsonConvert;

// ReSharper disable PossibleInvalidOperationException
// ReSharper disable GenericEnumeratorNotDisposed

// ReSharper disable FieldCanBeMadeReadOnly.Local
// ReSharper disable ArrangeTypeMemberModifiers

namespace hlcup {
    public static class Routes {
        public static AllData Data;
        static JsonSerializer _jsonSerializer = new JsonSerializer();
        public static long CurrentDate;

        public static Task Stats(HttpContext ctx) => ctx.Response.WriteAsync(SerializeObject(new {
            users = Data.Users.Length,
            locations = Data.Locations.Length,
            visits = Data.Visits.Length,
        }));

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static Task NotFound(HttpContext ctx) {
            ctx.Response.StatusCode = (int) HttpStatusCode.NotFound;
            return Task.CompletedTask;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static Task BadRequest(HttpContext ctx) {
            ctx.Response.StatusCode = (int) HttpStatusCode.BadRequest;
            return Task.CompletedTask;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static Task Json<T>(HttpContext ctx, T obj) where T : Entity {
            ctx.Response.StatusCode = (int) HttpStatusCode.OK;
            ctx.Response.ContentType = "application/json";

            if (obj.bytes != null) {
                ctx.Response.Body.Write(obj.bytes, 0, obj.bytes.Length);
            } else {
                Utf8Json.JsonSerializer.Serialize(ctx.Response.Body, obj);
            }

            return Task.CompletedTask;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static Task EmptyJson(HttpContext ctx) {
            ctx.Response.StatusCode = (int) HttpStatusCode.OK;
            ctx.Response.ContentType = "application/json";
            return ctx.Response.WriteAsync("{}");
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Task EntityById(HttpContext ctx, string entity, string idStr) {
            if (!int.TryParse(idStr, out var id))
                return NotFound(ctx);

            switch (entity) {
                case "users" when id < Data.Users.Length && Data.Users[id] is User user:
                    return Json(ctx, user);
                case "locations" when id < Data.Locations.Length && Data.Locations[id] is Location location:
                    return Json(ctx, location);
                case "visits" when id < Data.Visits.Length && Data.Visits[id] is Visit visit:
                    return Json(ctx, visit);
                default:
                    return NotFound(ctx);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Task Visits(HttpContext ctx, string idStr) {
            if (int.TryParse(idStr, out var id)
                && id < Data.Users.Length && Data.Users[id] is User user) {
                var visits = (IEnumerable<Visit>) user.Visits;

                if (ctx.Request.Query.TryGetValue("fromDate", out var fromDateStr)) {
                    if (int.TryParse(fromDateStr, out var fromDate)) {
                        visits = visits.Where(x => x.visited_at > fromDate);
                    } else {
                        return BadRequest(ctx);
                    }
                }

                if (ctx.Request.Query.TryGetValue("toDate", out var toDateStr)) {
                    if (int.TryParse(toDateStr, out var toDate)) {
                        visits = visits.Where(x => x.visited_at < toDate);
                    } else {
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
                    } else {
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

            return NotFound(ctx);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Task Avg(HttpContext ctx, string idStr) {
            if (!int.TryParse(idStr, out var id))
                return NotFound(ctx);

            if (id >= Data.Locations.Length || !(Data.Locations[id] is Location location))
                return NotFound(ctx);

            var visits = (IEnumerable<Visit>) location.Visits;

            if (ctx.Request.Query.TryGetValue("fromDate", out var fromDateStr)) {
                if (int.TryParse(fromDateStr, out var fromDate)) {
                    visits = visits.Where(x => x.visited_at > fromDate);
                } else {
                    return BadRequest(ctx);
                }
            }

            if (ctx.Request.Query.TryGetValue("toDate", out var toDateStr)) {
                if (int.TryParse(toDateStr, out var toDate)) {
                    visits = visits.Where(x => x.visited_at < toDate);
                } else {
                    return BadRequest(ctx);
                }
            }

            if (ctx.Request.Query.TryGetValue("gender", out var gender)) {
                var gchar = gender[0]?[0];
                if (gchar == 'm' || gchar == 'f') {
                    visits = visits.Where(x => x.User.gender == gchar);
                } else {
                    return BadRequest(ctx);
                }
            }

            var fromAgeValue = 0;
            var fromAge = false;
            var toAgeValue = 0;
            var toAge = false;

            if (ctx.Request.Query.TryGetValue("fromAge", out var fromAgeStr)) {
                if (int.TryParse(fromAgeStr, out fromAgeValue)) {
                    fromAge = true;
                } else {
                    return BadRequest(ctx);
                }
            }

            if (ctx.Request.Query.TryGetValue("toAge", out var toAgeStr)) {
                if (int.TryParse(toAgeStr, out toAgeValue)) {
                    toAge = true;
                } else {
                    return BadRequest(ctx);
                }
            }

            if (toAge || fromAge) {
                visits = Filter(visits);
            }

            return Json(ctx, new Avg {
                avg = Math.Round(Average(), 5, MidpointRounding.AwayFromZero)
            });

            IEnumerable<Visit> Filter(IEnumerable<Visit> vs) {
                var now = CurrentDate;

                var enumerator = vs.GetEnumerator();
                if (toAge && fromAge) {
                    while (enumerator.MoveNext() && enumerator.Current is var visit) {
                        var age = ((now - visit.User?.birth_date) / 31557600).Value;
                        if (age >= fromAgeValue && age < toAgeValue) {
                            yield return visit;
                        }
                    }
                } else if (toAge) {
                    while (enumerator.MoveNext() && enumerator.Current is var visit) {
                        var age = ((now - visit.User?.birth_date) / 31557600).Value;
                        if (age < toAgeValue) {
                            yield return visit;
                        }
                    }
                } else {
                    while (enumerator.MoveNext() && enumerator.Current is var visit) {
                        var age = ((now - visit.User?.birth_date) / 31557600).Value;
                        if (age >= fromAgeValue) {
                            yield return visit;
                        }
                    }
                }
            }

            double Average() {
                var enumerator = visits.GetEnumerator();
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

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Task Update(HttpContext ctx, string idStr) {
            if (!int.TryParse(idStr, out var id))
                return BadRequest(ctx);

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
                    if (id < Data.Users.Length && Data.Users[id] is User user) {
                        user.Update(update, Data);
                        return EmptyJson(ctx);
                    }
                    return NotFound(ctx);
                case "locations":
                    if (id < Data.Locations.Length && Data.Locations[id] is Location location) {
                        location.Update(update, Data);
                        return EmptyJson(ctx);
                    }
                    return NotFound(ctx);
                case "visits":
                    if (id < Data.Visits.Length && Data.Visits[id] is Visit visit) {
                        visit.Update(update, Data);
                        return EmptyJson(ctx);
                    }
                    return NotFound(ctx);
                default:
                    return BadRequest(ctx);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Task Create(HttpContext ctx, string entity) {
            switch (entity) {
                case "users":
                    var user = ReadFromBody<User>(ctx);
                    if (!user.IsValid())
                        return BadRequest(ctx);

                    Data.Users[user.id.Value] = user;
//                    user.UpdateCache();
                    return EmptyJson(ctx);

                case "locations":
                    var location = ReadFromBody<Location>(ctx);
                    if (!location.IsValid())
                        return BadRequest(ctx);

                    Data.Locations[location.id.Value] = location;
//                    location.UpdateCache();
                    return EmptyJson(ctx);

                case "visits":
                    var visit = ReadFromBody<Visit>(ctx);
                    if (!visit.IsValid())
                        return BadRequest(ctx);

                    Data.Visits[visit.id.Value] = visit;

                    if (Data.Locations[visit.location.Value] is Location loc) {
                        visit.Location = loc;
                        visit.Location.Visits.Add(visit);
                    }

                    if (Data.Users[visit.user.Value] is User usr) {
                        usr.Visits.Add(visit);
                        visit.User = usr;
                    }

//                    visit.UpdateCache();
                    return EmptyJson(ctx);

                default:
                    return BadRequest(ctx);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static T ReadFromBody<T>(HttpContext ctx) => Utf8Json.JsonSerializer.Deserialize<T>(ctx.Request.Body);
    }

    public class VisitsVm : Entity {
        public IEnumerable<VisitVm> visits;

        public class VisitVm {
            public int mark;
            public int visited_at;
            public string place;
        }
    }

    public class Avg : Entity {
        public double avg;
    }
}