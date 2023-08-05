// ***********************************************************************
// Copyright (c) Charlie Poole and TestCentric contributors.
// Licensed under the MIT License. See LICENSE file in root directory.
// ***********************************************************************

using NUnit.Engine;
using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using TestCentric.Engine.Agents;
using TestCentric.Engine.Internal;
using TestCentric.Engine.Communication.Messages;
using TestCentric.Engine.Communication.Protocols;
using System.Runtime.Serialization;
using System.Xml;
using System.IO;

namespace TestCentric.Engine.Communication.Transports.Tcp
{
    public class TestAgentTcpTransport : ITestAgentTransport, ITestEventListener
    {
        private static readonly Logger log = InternalTrace.GetLogger(typeof(TestAgentTcpTransport));

        private string _agencyUrl;
        private Socket _clientSocket;
        private ITestEngineRunner _runner;

        public TestAgentTcpTransport(RemoteTestAgent agent, string serverUrl)
        {
            Guard.ArgumentNotNull(agent, nameof(agent));
            Agent = agent;

            Guard.ArgumentNotNullOrEmpty(serverUrl, nameof(serverUrl));
            _agencyUrl = serverUrl;

            var parts = serverUrl.Split(new char[] { ':' });
            Guard.ArgumentValid(parts.Length == 2, "Invalid server address specified. Must be a valid endpoint including the port number", nameof(serverUrl));
            ServerEndPoint = new IPEndPoint(IPAddress.Parse(parts[0]), int.Parse(parts[1]));
            log.Debug($"Using server EndPoint {ServerEndPoint}");
        }

        public TestAgent Agent { get; }

        public IPEndPoint ServerEndPoint { get; }

        public bool Start()
        {
            try
            {
                // Connect to the server
                log.Debug($"Connecting to TestAgency at {_agencyUrl}");
                _clientSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                _clientSocket.Connect(ServerEndPoint);
                log.Info($"Connected to TestAgency at {_clientSocket.RemoteEndPoint}");
            }
            catch(System.Exception ex)
            {
                log.Error(ex.ToString());
            }

            // Immediately upon connection send the agent Id as a raw byte array
            _clientSocket.Send(Agent.Id.ToByteArray());
            log.Info("Sent agent ID");

            // Start the loop that reads and executes commands
            Thread commandLoop = new Thread(CommandLoop);
            commandLoop.Start();

            return true;
        }

        public void Stop()
        {
            //Agent.StopSignal.Set();
        }

        public ITestEngineRunner CreateRunner(TestPackage package)
        {
            log.Debug("Create runner for test package");
            log.Debug($"  Id={package.ID}");
            log.Debug($"  Name={package.Name}");
            log.Debug($"  FullName={package.FullName}");
            log.Debug("  Settings:");
            foreach(var key in package.Settings.Keys)
                log.Debug($"    {key}={package.Settings[key]}");

            return Agent.CreateRunner(package);
        }

        private void CommandLoop()
        {
            bool keepRunning = true;
            var socketReader = new SocketReader(_clientSocket, new BinarySerializationProtocol());

            try
            {
                while (keepRunning)
                {
                    log.Debug("Waiting for a command");
                    var command = socketReader.GetNextMessage<CommandMessage>();
                    log.Debug($"Received {command.CommandName} command");
                    if (command.Argument == null)
                        log.Debug($"  No Argument provided");
                    else
                        log.Debug($"  Argument Type: {command.Argument.GetType()}");

                    switch (command.CommandName)
                    {
                        case "CreateRunner":
                            var package = new TestPackageSerializer().Deserialize((string)command.Argument);
                            //log.Debug($"CreateRunner for package {package.FullName}");
                            //var package = (TestPackage)command.Argument;
                            _runner = CreateRunner(package);
                            break;
                        case "Load":
                            SendResult(_runner.Load());
                            break;
                        case "Reload":
                            SendResult(_runner.Reload());
                            break;
                        case "Unload":
                            _runner.Unload();
                            break;
                        case "Explore":
                            var filter = GetFilterArgument(command);
                            SendResult(_runner.Explore(filter));
                            break;
                        case "CountTestCases":
                            filter = GetFilterArgument(command);
                            SendResult(_runner.CountTestCases(filter));
                            break;
                        case "Run":
                            filter = GetFilterArgument(command);
                            Thread runnerThread = new Thread(RunTests);
                            runnerThread.Start(filter);
                            break;

                        case "RunAsync":
                            filter = GetFilterArgument(command);
                            SendResult(_runner.RunAsync(this, filter));
                            break;

                        case "RequestStop":
                            _runner.RequestStop();
                            break;

                        case "ForcedStop":
                            _runner.ForcedStop();
                            break;

                        case "Stop":
                            keepRunning = false;
                            break;
                    }
                }
            }
            catch(Exception ex)
            {
                log.Error(ex.ToString());
                throw;
            }

            log.Info("Terminating command loop");
            Stop();

            TestFilter GetFilterArgument(CommandMessage command)
            {
                var filterText = (string)command.Argument;
                log.Debug($"  Filter = {filterText}");
                return new TestFilter(filterText);
            }
        }

        private void RunTests(object filter)
        {
            SendResult(_runner.Run(this, (TestFilter)filter));
        }

        private void SendResult(object result)
        {
            var resultMessage = new CommandReturnMessage(result);
            var bytes = new BinarySerializationProtocol().Encode(resultMessage);
            log.Debug($"Sending result {result.GetType().Name}, length={bytes.Length}");
            _clientSocket.Send(bytes);
        }

        public void OnTestEvent(string report)
        {
            var progressMessage = new ProgressMessage(report);
            var bytes = new BinarySerializationProtocol().Encode(progressMessage);
            _clientSocket.Send(bytes);
        }
    }
}
