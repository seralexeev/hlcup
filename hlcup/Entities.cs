using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Newtonsoft.Json;
using static hlcup.Helpers;

namespace hlcup {
    class Helpers {
        public const char BEGIN_OBJ = '{';
    }

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

        [JsonIgnore]
        public SortedList<int, Visit> Visits { get; set; } = new SortedList<int, Visit>();
    }

    public class Location {
        public int id;
        public string place;
        public string country;
        public string city;
        public int distance;

        [JsonIgnore]
        public List<Visit> Visits { get; set; } = new List<Visit>();
    }

    public class Visit {
        public int id;
        public int location;
        public int user;
        public int visited_at;
        public int mark;

        [JsonIgnore]
        public User User { get; set; }

        [JsonIgnore]
        public Location Location { get; set; }
    }
}