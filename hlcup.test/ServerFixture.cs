using System;
using System.Net.Http;
using Microsoft.AspNetCore.TestHost;

namespace hlcup.test {
    public class ServerFixture : IDisposable {
        readonly TestServer _server;

        public ServerFixture() {
            var program = new Program();
            var data = program.LoadData("../../../../../../hdata");
            _server = new TestServer(program.GetHostBuilder(new Routes(data)));
            Client = _server.CreateClient();
        }

        public HttpClient Client { get; }

        public void Dispose() {
            Client.Dispose();
            _server.Dispose();
        }
    }
}