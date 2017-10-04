using System;
using System.Net.Http;
using Microsoft.AspNetCore.TestHost;

namespace hlcup.test {
    public class ServerFixture : IDisposable {
        readonly TestServer _server;

        public ServerFixture() {
//            var data = Program.LoadData("../../../../../../hdata");
//            (Routes.Data, Routes.Data.currentDate) = data;
            _server = new TestServer(Program.GetHostBuilder());
            Client = _server.CreateClient();
        }

        public HttpClient Client { get; }

        public void Dispose() {
            Client.Dispose();
            _server.Dispose();
        }
    }
}