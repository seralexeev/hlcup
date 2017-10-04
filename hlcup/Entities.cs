using System.Collections.Generic;
using System.Runtime.Serialization;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

// ReSharper disable InconsistentNaming
// ReSharper disable MemberCanBePrivate.Global
// ReSharper disable UnassignedField.Global

namespace hlcup {
    public static class AllData {
        public static User[] users;
        public static Location[] locations;
        public static Visit[] visits;
        public static long currentDate;
    }

    public abstract class Entity {
        [JsonIgnore, IgnoreDataMember] public byte[] bytes;
    }

    public class User : Entity {
        public int? id;
        public string email;
        public string first_name;
        public string last_name;
        public char? gender;
        public int? birth_date;

        [JsonIgnore, IgnoreDataMember]
        public int age;

        public void CalculateAge() => age = (int)((AllData.currentDate - birth_date) / 31557600).Value;

        public bool IsValid() => id != null && email != null && first_name != null && last_name != null &&
                                 gender != null && birth_date != null;

        public void Update(Dictionary<string, JValue> obj) {
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
                this.birth_date = birth_date.Value<int?>();
            }

            CalculateAge();
        }

        [JsonIgnore, IgnoreDataMember]
        public List<Visit> Visits { get; } = new List<Visit>();
    }

    public class Location : Entity {
        public int? id;
        public string place;
        public string country;
        public string city;
        public int? distance;

        public bool IsValid() => id != null && place != null && country != null && city != null && distance != null;

        public void Update(Dictionary<string, JValue> obj) {
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
                this.distance = distance.Value<int?>();
            }
        }

        [JsonIgnore, IgnoreDataMember]
        public List<Visit> Visits { get; } = new List<Visit>();
    }

    public class Visit : Entity {
        public int? id;
        public int? location;
        public int? user;
        public int? visited_at;
        public int? mark;

        public bool IsValid() => id != null && location != null && user != null && visited_at != null && mark != null;

        public void Update(Dictionary<string, JValue> obj) {
            if (obj.TryGetValue(nameof(Visit.location), out var jloc) && jloc.Value<int?>() is var location &&
                location != this.location) {
                AllData.locations[this.location.Value].Visits.Remove(this);
                this.location = location;
                Location = AllData.locations[this.location.Value];
                Location.Visits.Add(this);
            }

            if (obj.TryGetValue(nameof(Visit.visited_at), out var visited_at)) {
                this.visited_at = visited_at.Value<int?>();
            }

            if (obj.TryGetValue(nameof(Visit.user), out var juser) && juser.Value<int?>() is var user &&
                user != this.user) {
                AllData.users[this.user.Value].Visits.Remove(this);
                this.user = user;
                User = AllData.users[this.user.Value];
                User.Visits.Add(this);
            }

            if (obj.TryGetValue(nameof(Visit.mark), out var mark)) {
                this.mark = mark.Value<int?>();
            }
        }

        [JsonIgnore, IgnoreDataMember]
        public User User { get; set; }

        [JsonIgnore, IgnoreDataMember]
        public Location Location { get; set; }
    }
}