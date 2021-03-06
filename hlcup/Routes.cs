﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

// ReSharper disable InconsistentNaming
// ReSharper disable PossibleInvalidOperationException
// ReSharper disable GenericEnumeratorNotDisposed
// ReSharper disable FieldCanBeMadeReadOnly.Local
// ReSharper disable ArrangeTypeMemberModifiers

namespace hlcup {
    public static class Routes {
        static JsonSerializer _jsonSerializer = new JsonSerializer();
        static byte[] emptyJson = Encoding.UTF8.GetBytes("{}");
        public static byte[] empty = new byte[0];

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static byte[] EntityById(HttpContext ctx, char entity, string idStr) {
            if (!int.TryParse(idStr, out var id))
                return NotFound(ctx);

            switch (entity) {
                case 'u' when id < AllData.users.Length && AllData.users[id] is User user:
                    return Json(ctx, user);
                case 'l' when id < AllData.locations.Length && AllData.locations[id] is Location location:
                    return Json(ctx, location);
                case 'v' when id < AllData.visits.Length && AllData.visits[id] is Visit visit:
                    return Json(ctx, visit);
                default:
                    return NotFound(ctx);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static byte[] Visits(HttpContext ctx, string idStr) {
            if (!int.TryParse(idStr, out var id) || id >= AllData.users.Length || !(AllData.users[id] is User user))
                return NotFound(ctx);

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

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static byte[] Avg(HttpContext ctx, string idStr) {
            if (!int.TryParse(idStr, out var id))
                return NotFound(ctx);

            if (id >= AllData.locations.Length || !(AllData.locations[id] is Location location))
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
                var enumerator = vs.GetEnumerator();
                if (toAge && fromAge) {
                    while (enumerator.MoveNext() && enumerator.Current is var visit) {
                        if (visit.User.age >= fromAgeValue && visit.User.age < toAgeValue) {
                            yield return visit;
                        }
                    }
                } else if (toAge) {
                    while (enumerator.MoveNext() && enumerator.Current is var visit) {
                        if (visit.User.age < toAgeValue) {
                            yield return visit;
                        }
                    }
                } else {
                    while (enumerator.MoveNext() && enumerator.Current is var visit) {
                        if (visit.User.age >= fromAgeValue) {
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
        public static byte[] Update(HttpContext ctx, char entity, string idStr) {
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

            switch (entity) {
                case 'u':
                    if (id < AllData.users.Length && AllData.users[id] is User user) {
                        user.Update(update);
                        return EmptyJson(ctx);
                    }
                    return NotFound(ctx);
                case 'l':
                    if (id < AllData.locations.Length && AllData.locations[id] is Location location) {
                        location.Update(update);
                        return EmptyJson(ctx);
                    }
                    return NotFound(ctx);
                case 'v':
                    if (id < AllData.visits.Length && AllData.visits[id] is Visit visit) {
                        visit.Update(update);
                        return EmptyJson(ctx);
                    }
                    return NotFound(ctx);
                default:
                    return BadRequest(ctx);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static byte[] Create(HttpContext ctx, char entity) {
            switch (entity) {
                case 'u':
                    var user = ReadFromBody<User>(ctx);
                    if (!user.IsValid())
                        return BadRequest(ctx);

                    AllData.users[user.id.Value] = user;
                    user.CalculateAge();
                    return EmptyJson(ctx);

                case 'l':
                    var location = ReadFromBody<Location>(ctx);
                    if (!location.IsValid())
                        return BadRequest(ctx);

                    AllData.locations[location.id.Value] = location;
                    return EmptyJson(ctx);

                case 'v':
                    var visit = ReadFromBody<Visit>(ctx);
                    if (!visit.IsValid())
                        return BadRequest(ctx);

                    AllData.visits[visit.id.Value] = visit;

                    if (AllData.locations[visit.location.Value] is Location loc) {
                        visit.Location = loc;
                        visit.Location.Visits.Add(visit);
                    }

                    if (AllData.users[visit.user.Value] is User usr) {
                        usr.Visits.Add(visit);
                        visit.User = usr;
                    }

                    return EmptyJson(ctx);
                default:
                    return BadRequest(ctx);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static byte[] NotFound(HttpContext ctx) {
            ctx.Response.StatusCode = StatusCodes.Status404NotFound;
            return empty;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static byte[] BadRequest(HttpContext ctx) {
            ctx.Response.StatusCode = StatusCodes.Status400BadRequest;
            return empty;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static byte[] Json(HttpContext ctx, object obj) {
            ctx.Response.StatusCode = StatusCodes.Status200OK;
            ctx.Response.ContentType = "application/json";
            return Utf8Json.JsonSerializer.Serialize(obj);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static byte[] EmptyJson(HttpContext ctx) {
            ctx.Response.StatusCode = StatusCodes.Status200OK;
            ctx.Response.ContentType = "application/json";
            return emptyJson;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static T ReadFromBody<T>(HttpContext ctx) => Utf8Json.JsonSerializer.Deserialize<T>(ctx.Request.Body);
    }
}