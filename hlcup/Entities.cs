using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

// ReSharper disable InconsistentNaming
// ReSharper disable MemberCanBePrivate.Global
// ReSharper disable UnassignedField.Global

namespace hlcup {
    public class AllData {
        public User[] Users;
        public Location[] Locations;
        public Visit[] Visits;
    }

    public class User {
        public int id;
        public string email;
        public string first_name;
        public string last_name;
        public char gender;
        public int birth_date;

        public void Update(Dictionary<string, JToken> obj, AllData data) {
            if (obj.TryGetValue(nameof(User.email), out var email)) {
                this.email = email.Value<string>();
            }

            if (obj.TryGetValue(nameof(User.first_name), out var first_name)) {
                this.first_name = first_name.Value<string>();
            }

            if (obj.TryGetValue(nameof(User.last_name), out var last_name)) {
                this.last_name = last_name.Value<string>();
            }

            if (obj.TryGetValue(nameof(User.gender), out var gender)) {
                this.gender = gender.Value<char>();
            }

            if (obj.TryGetValue(nameof(User.birth_date), out var birth_date)) {
                this.birth_date = birth_date.Value<int>();
            }
        }

        [JsonIgnore]
        public List<Visit> Visits { get; } = new List<Visit>();
    }

    public class Location {
        public int id;
        public string place;
        public string country;
        public string city;
        public int distance;

        public void Update(Dictionary<string, JToken> obj, AllData data) {
            if (obj.TryGetValue(nameof(Location.place), out var place)) {
                this.place = place.Value<string>();
            }

            if (obj.TryGetValue(nameof(Location.country), out var country)) {
                this.country = country.Value<string>();
            }

            if (obj.TryGetValue(nameof(Location.city), out var city)) {
                this.city = city.Value<string>();
            }

            if (obj.TryGetValue(nameof(Location.distance), out var distance)) {
                this.distance = distance.Value<int>();
            }
        }

        [JsonIgnore]
        public List<Visit> Visits { get; } = new List<Visit>();
    }

    public class Visit {
        public int id;
        public int location;
        public int user;
        public int visited_at;
        public int mark;

        public void Update(Dictionary<string, JToken> obj, AllData data) {
            if (obj.TryGetValue(nameof(Visit.location), out var jloc) && jloc.Value<int>() is var location &&
                location != this.location) {
                data.Locations[this.location].Visits.Remove(this);
                this.location = location;
                Location = data.Locations[this.location];
                Location.Visits.Add(this);
            }

            if (obj.TryGetValue(nameof(Visit.visited_at), out var visited_at)) {
                this.visited_at = visited_at.Value<int>();
            }

            if (obj.TryGetValue(nameof(Visit.user), out var juser) && juser.Value<int>() is var user &&
                user != this.user) {
                data.Users[this.user].Visits.Remove(this);
                this.user = user;
                User = data.Users[this.user];
                User.Visits.Add(this);
            }

            if (obj.TryGetValue(nameof(Visit.mark), out var mark)) {
                this.mark = mark.Value<int>();
            }
        }

        [JsonIgnore]
        public User User { get; set; }

        [JsonIgnore]
        public Location Location { get; set; }
    }
}