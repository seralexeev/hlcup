using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using FluentAssertions;
using Xunit;
using static Newtonsoft.Json.JsonConvert;

// ReSharper disable ArrangeTypeMemberModifiers

namespace hlcup.test {
    public class ServerTests : IClassFixture<ServerFixture> {
        readonly HttpClient _client;

        public ServerTests(ServerFixture server) => _client = server.Client;

        async Task<T> GetRequest<T>(string path) =>
            DeserializeObject<T>(await (await _client.GetAsync(path))
                .EnsureSuccessStatusCode().Content.ReadAsStringAsync());

        async Task<HttpStatusCode> GetStatusCode(string path) => (await _client.GetAsync(path)).StatusCode;

        [Fact]
        public async Task DataLoads() {
            var res = await GetRequest<Dictionary<string, int>>("/stats");

            res["users"].Should().BePositive();
            res["locations"].Should().BePositive();
            res["visits"].Should().BePositive();
        }

        [Fact]
        public async Task UserById() {
            var res = await GetRequest<User>("/users/1");
            var expected = DeserializeObject<User>(
                @"{""first_name"": ""Ксения"", ""last_name"": ""Фетушуко"", ""birth_date"": 316656000, ""gender"": ""f"", ""id"": 1, ""email"": ""sasotrehemilroharo@inbox.ru""}");

            res.ShouldBeEquivalentTo(expected);
        }

        [Fact]
        public async Task LocationById() {
            var res = await GetRequest<User>("/locations/1");
            var expected = DeserializeObject<User>(
                @"{""distance"": 37, ""city"": ""Муратск"", ""place"": ""Забор"", ""id"": 1, ""country"": ""Египет""}");

            res.ShouldBeEquivalentTo(expected);
        }

        [Fact]
        public async Task VisitById() {
            var res = await GetRequest<User>("/visits/1");
            var expected = DeserializeObject<User>(
                @"{""user"": 56, ""location"": 78, ""visited_at"": 1173467226, ""id"": 1, ""mark"": 4}");

            res.ShouldBeEquivalentTo(expected);
        }

        [Fact]
        public async Task VisitsByUser() {
            var res = await GetRequest<Visit[]>("/users/1/visits");

            res.Length.Should().Be(51);
        }

        [Theory]
        [InlineData("/users/1/visits?fromDate=")]
        [InlineData("/users/1/visits?fromDate=abracadbra")]
        public async Task VisitsByUserBadRequest(string path) {
            var code = await GetStatusCode(path);

            code.ShouldBeEquivalentTo(HttpStatusCode.BadRequest);
        }

        [Theory]
        [InlineData("/users/100000/visits")]
        public async Task VisitsByUserNotFound(string path) {
            var code = await GetStatusCode(path);

            code.ShouldBeEquivalentTo(HttpStatusCode.NotFound);
        }
    }
}