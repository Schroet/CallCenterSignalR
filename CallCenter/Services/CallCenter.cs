using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
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
        private List<Call> _calls = new List<Call>();
        private List<Call> _awaitingCalls = new List<Call>();


        public void Start(SimulationOptions options)
        {
            CallCenterHubAppendLine("Simulation starting");

            //options.CallsAmount = _options.CallsAmount;

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

            for (var i = 0; i < _options.CallsAmount; i++)
            {
                var duration = _random.Next(_options.MinSecAnswer, _options.MaxSecAnswer);
                var call = new Call { Id = callId++, Duration = duration, IsActive = true };
                _calls.Add(call);
            }

            foreach (var @operator in _operators)
            {
                @operator.StatusChanged += operator_StatusChanged;
                var thread = new Thread(() => @operator.Start());
                thread.Start();
            }

            _isRunning = true;
            CallCenterHubAppendLine("Simulation started");

            //Section for handling with generated calls list.
            CallCenterHubAppendLine("Operators starts answer the calls.");

            while (_calls.Any() || _awaitingCalls.Any())
            {
                Task task = null;

                if (_calls.Any() || _awaitingCalls.Any())
                {
                    var call = _calls.FirstOrDefault();

                    if (call == null)
                    {
                        call = _awaitingCalls.Where(x=>x.IsActive == true).FirstOrDefault();
                        if(call == null)
                        {
                            CallCenterHubAppendLine("Calls are ended");
                            _awaitingCalls.Clear();
                        }
                    }

                    if (call != null)
                    {
                        Thread.Sleep(call.Duration * 1000 / 5);
                        CallCenterHubAppendLine("Sending a call");
                        task = Task.Run(() => SendingCallsAsync());
                        task.Wait();
                    }
                }
            }

            WaitForEmployeeEndsCalls();
            Stop();
        }

        public void AnsweringProcess()
        {
            for (var idx = 0; idx < 100; ++idx)
            {
                var @freeOperator = _operators.OrderBy(_ => _.Title).FirstOrDefault(_ => !_.IsBusy);
                var activeCall = _calls.Where(x => x.IsActive == true).FirstOrDefault();
                if (@freeOperator == null)
                {
                    _awaitingCalls.Add(activeCall);
                    CallCenterHubAppendLine("Sorry! All operators are busy. Try again later.");
                }
                else
                {
                    CallCenterHubAppendLine(freeOperator.Title + " " + freeOperator.Id + "" + " took a call " + activeCall.Id);
                    @freeOperator.Answer(activeCall.Duration);
                    CallCenterHubAppendLine(freeOperator.Title + " " + freeOperator.Id + "" + " ended a call " + activeCall.Id);
                    activeCall.IsActive = false;
                    _calls.Remove(activeCall);
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
            _calls.Clear();
            _awaitingCalls.Clear();

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

        public async Task SendingCallsAsync()
        {
            CallCenterHubAppendLine("Call sent");

            Call call = null;

            if (_calls.Any())
            {
                call = _calls.FirstOrDefault();
                call.IsActive = false;
            }
            else if (_awaitingCalls.Any())
            {
                call = _awaitingCalls.FirstOrDefault();
            }

            var @operator = _operators.OrderBy(_ => _.Title).FirstOrDefault(_ => !_.IsBusy);
            if (@operator == null)
            {      
                var waitingCall = _awaitingCalls.Where(x => x.Id == call.Id).FirstOrDefault();
                if(waitingCall == null)
                {
                    _awaitingCalls.Add(call);
                } 
                CallCenterHubAppendLine("Sorry! All operators are busy. Try again later.");
            }
            else
            {
                CallCenterHubAppendLine(@operator.Title + " " + @operator.Id + "" + " took a call " + call.Id);
                await @operator.Answer(call.Duration);

                var callExists = _calls.Where(x => x.Id == call.Id).FirstOrDefault();
                if (callExists != null)
                {
                    _calls.Remove(call);
                }
                else
                {
                    _awaitingCalls.Remove(call);
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
