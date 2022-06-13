using System;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using MessageBasedSockets.Messages;
using NUnit.Framework;

namespace MessageBasedSockets {
    public class UnitTest2 {
        private static readonly int _clientCount = 500;

        private Server   _server;
        private Client[] _clients;

        private TaskCompletionSource _task;

        [OneTimeSetUp]
        public void Setup() {
            Logging.SetActions(
                s => Console.WriteLine(s),
                s => Console.Error.WriteLine(s),
                s => Console.WriteLine($"DEBUG: {s}")
            );
            IMessage.VisibilityMask = TypeAttributes.NotPublic;
            _server = new Server("localhost", 2011).Start();
            TaskCompletionSource mainTask = new TaskCompletionSource();
            int connected = 0;
            _clients = new Client[_clientCount];
            for (int i = 0; i < _clientCount; i++) {
                _clients[i] = new Client("localhost", 2011);
                TaskCompletionSource task = new TaskCompletionSource();

                void OnConnect() {
                    task.SetResult();
                }

                _clients[i].OnConnect += OnConnect;
                int finalI = i;
                task.Task.ContinueWith(
                    t => {
                        connected++;
                        _clients[finalI].OnConnect -= OnConnect;
                        if (connected == _clientCount)
                            mainTask.SetResult();
                    }
                );
                _clients[i].Connect();
            }

            mainTask.Task.Wait();

            Logging.Debug("Setup completed");
        }

        [Test]
        public void Test1ClientsSendOneMessage() {
            int total = 0;

            void OnClientOneMessageServerHandler(ServerClient client, IMessage message) {
                Assert.That(message is MessageHello, Is.True);
                var hello = (MessageHello)message;
                Assert.Multiple(() => {
                                    Assert.That(hello.Author, Is.EqualTo($"Client{hello.Id}"));
                                    Assert.That(hello.Message, Is.EqualTo($"Hello! {hello.Id}"));
                                });
                Interlocked.Increment(ref total);

                if (total != _clientCount)
                    return;

                _task.SetResult();
            }

            _task = new TaskCompletionSource();
            _server.OnClientMessageReceived += OnClientOneMessageServerHandler;
            for (int i = 0; i < _clientCount; i++) {
                _clients[i].Messenger.Send(new MessageHello {
                                                                Id = (ulong)i,
                                                                Author = $"Client{i}",
                                                                Message = $"Hello! {i}"
                                                            });
            }

            if (!_task.Task.Wait(10000))
                Assert.Fail();

            _server.OnClientMessageReceived -= OnClientOneMessageServerHandler;
            Assert.Pass();
        }

        [OneTimeTearDown]
        public void TearDown() {
            foreach (Client client in _clients) {
                client.Disconnect();
            }

            _server.Stop();
        }
    }
}