using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CallCenter.Helpers;
using CallCenter.Hubs;
using CallCenter.Models;
using Microsoft.AspNetCore.SignalR;



namespace CallCenter.Services
{
    public class CallCenter : ICallCenter
    {
        private static IHubContext<CallCenterHub> _hubContext;

        public CallCenter(IHubContext<CallCenterHub> context)
        {
            _hubContext = context;
        }

        private readonly Random _random = new Random();
        private bool _isRunning = false;
        private bool _isPaused = false;
        private SimulationOptions _options;
        private readonly List<Operator> _operators = new List<Operator>();

        //private List<Call> _calls = new List<Call>();
        //private List<Call> _awaitingCalls = new List<Call>();

        private ConcurrentBag<Call> _safeCalls = new ConcurrentBag<Call>();
        private ConcurrentBag<Call> _safeAwaitingCalls = new ConcurrentBag<Call>();

        //public SynchronizedCollection<Call> _safeCalls = new SynchronizedCollection<Call>();
        //private SynchronizedCollection<Call> _safeAwaitingCalls = new SynchronizedCollection<Call>();


        public void Start(SimulationOptions options)
        {
            CallCenterHubAppendLine("Simulation starting");

            if (_isRunning) throw new Exception("Already started");

            _options = options;
            var id = 1;
            var callId = 1;

            for (var i = 0; i < _options.OperatorCount; i++)
            {
                var @operator = new Operator(id++, OperatorTitle.Operator);
                _operators.Add(@operator);
            }

            for (var i = 0; i < _options.ManagerCount; i++)
            {
                var @operator = new Operator(id++, OperatorTitle.Manager);
                _operators.Add(@operator);
            }

            for (var i = 0; i < _options.SeniorManagerCount; i++)
            {
                var @operator = new Operator(id++, OperatorTitle.SeniorManager);
                _operators.Add(@operator);
            }

            //for (var i = 0; i < _options.CallsAmount; i++)
            //{
            //    var duration = _random.Next(_options.MinSecAnswer, _options.MaxSecAnswer);
            //    var call = new Call { Id = callId++, Duration = duration, IsActive = true };
            //    _calls.Add(call);
            //}

            for (var i = 0; i < _options.CallsAmount; i++)
            {
                var duration = _random.Next(_options.MinSecAnswer, _options.MaxSecAnswer);
                var call = new Call { Id = callId++, Duration = duration, IsActive = true };
                _safeCalls.Add(call);
            }

            foreach (var @operator in _operators)
            {
                @operator.StatusChanged += operator_StatusChanged;
                var thread = new Thread(() => @operator.Start());
                thread.Start();
            }

            var callThread = new Thread(ActivateCallsSending);
            callThread.Start();

            var t = Task.Run(() => ActivateCallsSending());
            _isRunning = true;
            t.Wait();

            
            CallCenterHubAppendLine("Simulation started");

            CallCenterHubAppendLine("Operators starts answer the calls.");



            WaitForEmployeeEndsCalls();
            Stop();
        }

        public void ActivateCallsSending()
        {
            while (_safeCalls.Any() || _safeAwaitingCalls.Any())
            {
                Task task = null;

                if (_safeCalls.Any() || _safeAwaitingCalls.Any())
                {
                    var call = _safeCalls.FirstOrDefault();

                    if (call == null)
                    {
                        call = _safeAwaitingCalls.Where(x => x.IsActive == true).FirstOrDefault();
                        if (call == null)
                        {
                            CallCenterHubAppendLine("Calls are ended");
                            _safeAwaitingCalls.Clear();
                        }
                    }

                    if (call != null)
                    {
                        Thread.Sleep(call.Duration * 1000 / 15);
                        CallCenterHubAppendLine("Sending a call");
                        task = Task.Run(() => SendingCallsAsync());
                        task.Wait();
                    }
                }
            }
        }

        public void WaitForEmployeeEndsCalls()
        {
            var allEmployess = _operators.Where(x => x.Id > 0).Count();
            var freeEmployeesAmount = _operators.Where(x => x.IsBusy == false).Count();

            while (allEmployess != freeEmployeesAmount)
            {
                Thread.Sleep(5000);
                Task.Delay(250);
                freeEmployeesAmount = _operators.Where(x => x.IsBusy == false).Count();
            } 
        }

        private void operator_StatusChanged(object sender, StatusChangedEventArgs e)
        {
            CallCenterHubAppendLine(e.Message);
        }

        public void Pause()
        {
            if (!_isRunning) throw new Exception("Simulation not started yet");
            _isPaused = !_isPaused;
            CallCenterHubAppendLine("Simulation restarting");
        }

        public void Stop()
        {
            CallCenterHubAppendLine("Simulation stopping");

            if (!_isRunning) throw new Exception("Simulation not started yet");
            _isRunning = false;

            foreach (var thread in _operators) thread.Kill();
            _operators.Clear();
            _safeCalls.Clear();
            _safeAwaitingCalls.Clear();

            CallCenterHubAppendLine("Simulation stopped");
        }

        public bool IsRunning => _isRunning;

        public void SendCall()
        {
            CallCenterHubAppendLine("Sending a call");

            var @operator = _operators.OrderBy(_ => _.Title).FirstOrDefault(_ => !_.IsBusy);
            if (@operator == null)
            {
                CallCenterHubAppendLine("Sorry! All operators are busy. Try again later.");
            }
            else
            {
                var duration = _random.Next(_options.MinSecAnswer, _options.MaxSecAnswer);
                @operator.Answer(duration);
            }

            CallCenterHubAppendLine("Call sent");
        }

        public void SendingCallsAsync()
        {
            CallCenterHubAppendLine("Call sent");

            Call call = null;

            if (_safeCalls.Any())
            {
                call = _safeCalls.FirstOrDefault();
                call.IsActive = false;
            }
            else if (_safeAwaitingCalls.Any())
            {
                call = _safeAwaitingCalls.FirstOrDefault();
            }

            var @operator = _operators.OrderBy(_ => _.Title).FirstOrDefault(_ => !_.IsBusy);
            if (@operator == null)
            {      
                var waitingCall = _safeAwaitingCalls.Where(x => x.Id == call.Id).FirstOrDefault();
                if(waitingCall == null)
                {
                    _safeAwaitingCalls.Add(call);
                } 
                CallCenterHubAppendLine("Sorry! All operators are busy. Try again later.");
            }
            else
            {
                CallCenterHubAppendLine(@operator.Title + " " + @operator.Id + "" + " took a call " + call.Id);
                @operator.Answer(call.Duration);

                var callExists = _safeCalls.Where(x => x.Id == call.Id).FirstOrDefault();
                if (callExists != null)
                {
                    ConcurrentBag.Remove(_safeCalls, callExists);
                }
                else
                {
                    ConcurrentBag.Remove(_safeAwaitingCalls, callExists);
                }
            }
        }

        private void CallCenterHubAppendLine(string message)
        {
            _hubContext.Clients.All.SendAsync("appendLine", PrepareResponse(message));
        }

        private CallCenterHubResponse PrepareResponse(string message)
        {
            return new CallCenterHubResponse
            {
                Message = message,
                FreeOperators = _operators.Count(_ => !_.IsBusy && _.Title == OperatorTitle.Operator),
                FreeManagers = _operators.Count(_ => !_.IsBusy && _.Title == OperatorTitle.Manager),
                FreeSeniorManagers = _operators.Count(_ => !_.IsBusy && _.Title == OperatorTitle.SeniorManager)
            };
        }

    }
}
