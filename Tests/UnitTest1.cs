using System;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using MessageBasedSockets.Messages;
using MessageBasedSockets.Types;
using NUnit.Framework;

namespace MessageBasedSockets {
    public class UnitTest1 {
        private static readonly string MessageHelloText = "Test message with id 0 and author set to \"Cloudy\".";

        private Server _server;
        private Client _client;

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
            _client = new Client("localhost", 2011);
            _task = new TaskCompletionSource();

            void OnConnectClientHandler() {
                _task.SetResult();
            }

            _client.OnConnect += OnConnectClientHandler;
            _client.Connect();
            _task.Task.Wait();
            _client.OnConnect -= OnConnectClientHandler;
            Logging.Debug("Setup completed");
        }


        [Test]
        // [Ignore("")]
        public void Test1ClientSendOneMessage() {
            void OnMessageReceivedServerHandler(ServerClient client, IMessage message) {
                Assert.That(message is MessageHello, Is.True);
                var hello = (MessageHello)message;
                Assert.Multiple(() => {
                                    Assert.That(hello.Id, Is.EqualTo(0));
                                    Assert.That(hello.Author, Is.EqualTo("Cloudy"));
                                    Assert.That(hello.Message.Length, Is.EqualTo(MessageHelloText.Length));
                                });
                _task.SetResult();
            }

            _task = new TaskCompletionSource();
            _server.OnClientMessageReceived += OnMessageReceivedServerHandler;
            _client.Messenger.Send(new MessageHello {
                                                        Id = 0,
                                                        Author = "Cloudy",
                                                        Message = MessageHelloText
                                                    });
            if (!_task.Task.Wait(10000))
                Assert.Fail();

            _server.OnClientMessageReceived -= OnMessageReceivedServerHandler;
            Assert.Pass();
        }

        [Test]
        // [Ignore("")]
        public void Test2ClientSendThousandMessages() {
            void OnThousandMessageReceivedServerHandler(ServerClient client, IMessage message) {
                Assert.That(message is MessageData, Is.True);
                var data = (MessageData)message;
                Assert.Multiple(
                    () => {
                        Assert.That(data.X, Is.EqualTo(data.Index * 5f + .5f * data.Index));
                        Assert.That(data.Y, Is.EqualTo(data.Index * 5f + .6f * data.Index));
                        Assert.That(data.Z, Is.EqualTo(data.Index * 5f + .75f * data.Index));
                        Assert.That(data.DoubleArray, Has.Length.EqualTo((int)data.Index));
                        Assert.That(data.SByte, Is.EqualTo((sbyte)data.Index));
                        Assert.That(data.Char, Is.EqualTo('\\'));
                        Assert.That(data.StringArray, Has.Length.EqualTo((int)data.Index));
                        foreach (var d in data.DoubleArray) {
                            Assert.That(d, Is.EqualTo(5d));
                        }

                        foreach (var s in data.StringArray) {
                            Assert.That(s, Is.EqualTo("abcdefghijklmnopqrstuvwxyz"));
                        }
                    }
                );

                if (data.Index != 999)
                    return;

                _task.SetResult();
            }

            _task = new TaskCompletionSource();
            _server.OnClientMessageReceived += OnThousandMessageReceivedServerHandler;
            // This will mainly test the behavior of the engine if an overflow occurs.
            // Sending lot of big messages at once can easily overflow recipient's buffer.
            // This will as well test if signed and unsigned data types are cast correctly,
            //      because the last message sent is bigger than max value of short
            for (int i = 0; i < 1000; i++) {
                double[] doubleArray = new double[i];
                Array.Fill(doubleArray, 5d);
                string[] stringArray = new string[i];
                Array.Fill(stringArray, "abcdefghijklmnopqrstuvwxyz");
                _client.Messenger.Send(new MessageData {
                                                           Index = (uint)i,
                                                           X = i * 5f + .5f * i,
                                                           Y = i * 5f + .6f * i,
                                                           Z = i * 5f + .75f * i,
                                                           DoubleArray = doubleArray,
                                                           SByte = (sbyte)i,
                                                           Char = '\\',
                                                           StringArray = stringArray
                                                       });
                Logging.Debug($"Sending data index {i}");
            }

            if (!_task.Task.Wait(60000))
                Assert.Fail();

            _server.OnClientMessageReceived -= OnThousandMessageReceivedServerHandler;
            Assert.Pass();
        }

        [Test]
        // [Ignore("")]
        public void Test3ClientSendMessagesWithStruct() {
            void OnMessageWithStructReceivedServerHandler(ServerClient client, IMessage message) {
                Assert.That(message is MessageDataStruct, Is.True);
                var data = (MessageDataStruct)message;
                Assert.Multiple(
                    () => {
                        Assert.That(data.Data.X, Is.EqualTo(data.Data.Index * 5f));
                        Assert.That(data.Data.Y, Is.EqualTo(data.Data.Index * 5f));
                        Assert.That(data.Data.Z, Is.EqualTo(data.Data.Index * 5f));
                        Assert.That(data.Data.DoubleArray, Has.Length.EqualTo((int)data.Data.Index));
                        Assert.That(data.Data.StringArray, Has.Length.EqualTo((int)data.Data.Index));
                        Assert.That(data.Vector.X, Is.EqualTo(data.Data.Index * .5f));
                        Assert.That(data.Vector.Y, Is.EqualTo(data.Data.Index * .5f));
                        Assert.That(data.Vector.Z, Is.EqualTo(data.Data.Index * .5f));
                        Assert.That(data.DataArray, Has.Length.EqualTo((int)data.Data.Index));
                        Assert.That(data.VectorArray, Has.Length.EqualTo((int)data.Data.Index));
                        Assert.That(data.NestedData.Vector.X, Is.EqualTo(data.Data.Index * .6f));
                        Assert.That(data.NestedData.Vector.Y, Is.EqualTo(data.Data.Index * .6f));
                        Assert.That(data.NestedData.Vector.Z, Is.EqualTo(data.Data.Index * .6f));
                        Assert.That(data.NestedData.VectorArray, Has.Length.EqualTo((int)data.Data.Index));
                    }
                );
                if (data.Data.Index != 499)
                    return;

                _task.SetResult();
            }

            _task = new TaskCompletionSource();
            _server.OnClientMessageReceived += OnMessageWithStructReceivedServerHandler;

            for (int i = 0; i < 500; i++) {
                var dataArray = new MessageData[i];
                var vectorArray = new TestVector[i];
                _client.Messenger.Send(
                    new MessageDataStruct {
                                              Data = new MessageData {
                                                                         Index = (ulong)i,
                                                                         X = i * 5f,
                                                                         Y = i * 5f,
                                                                         Z = i * 5f,
                                                                         DoubleArray = new double[i],
                                                                         Char = '\0',
                                                                         SByte = (sbyte)i,
                                                                         StringArray = new string[i]
                                                                     },
                                              Vector = new TestVector {
                                                                          X = i * .5f,
                                                                          Y = i * .5f,
                                                                          Z = i * .5f
                                                                      },
                                              DataArray = dataArray,
                                              VectorArray = vectorArray,
                                              NestedData =
                                                  new TestNestedData {
                                                                         Vector =
                                                                             new TestVector {
                                                                                                X = i * .6f,
                                                                                                Y = i * .6f,
                                                                                                Z = i * .6f
                                                                                            },
                                                                         VectorArray = new TestVector[i]
                                                                     }
                                          }
                );
            }

            if (!_task.Task.Wait(10000))
                Assert.Fail();

            _server.OnClientMessageReceived -= OnMessageWithStructReceivedServerHandler;
            Assert.Pass();
        }

        [OneTimeTearDown]
        public void TearDown() {
            _client.Disconnect();
            _server.Stop();
        }
    }
}