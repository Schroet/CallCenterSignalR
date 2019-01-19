using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
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
        private SimulationOptions _options;
        private readonly List<Operator> _operators = new List<Operator>();
        private readonly ConcurrentQueue<Call> _callsToProcess = new ConcurrentQueue<Call>();

        public void Start(SimulationOptions options)
        {
            CallCenterHubAppendLine("Simulation starting");

            if (_isRunning) throw new Exception("Already started");

            _callsToProcess.Clear();

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
                _callsToProcess.Enqueue(call);
            }

            foreach (var @operator in _operators)
            {
                @operator.StatusChanged += operator_StatusChanged;
                var thread = new Thread(() => @operator.Start());
                thread.Start();
            }
            _isRunning = true;
            new Thread(ActivateCallProcessing).Start();   
            CallCenterHubAppendLine("Simulation started");
            CallCenterHubAppendLine("Operators start answer the calls.");
        }

        public void Restart()
        {
            Stop();
            Start(_options);
        }

        public void ActivateCallProcessing()
        {
            while (_callsToProcess.Any())
            {
                if (!_callsToProcess.TryDequeue(out Call call)) continue; // for a call to process

                var @operator = _operators.OrderBy(_ => _.Title).FirstOrDefault(_ => !_.IsBusy);

                while (@operator == null && _isRunning == true) // looking for a free guy
                {
                    // all operators are busy
                    CallCenterHubAppendLine("All operators are busy");
                    Thread.Sleep(500);
                    @operator = _operators.OrderBy(_ => _.Title).FirstOrDefault(_ => !_.IsBusy);
                }

                if( call != null && @operator != null)
                {
                    @operator.Answer(call);
                    Thread.Sleep(500);
                }    
            }

            while (_operators.Any(_ => _.IsBusy)) // waiting for all operators to process calls
            {
                Thread.Sleep(2000);
            }

            if(_isRunning != false)
            {
                Stop();
            }       
        }

        private void operator_StatusChanged(object sender, StatusChangedEventArgs e)
        {
            CallCenterHubAppendLine(e.Message);
        }

        public void Stop()
        {
            CallCenterHubAppendLine("Simulation stopping");

            //if (!_isRunning) throw new Exception("Simulation not started yet");
            _isRunning = false;

            foreach (var @operator in _operators) @operator.Kill();
            _operators.Clear();
            _callsToProcess.Clear();

            CallCenterHubAppendLine("Simulation stopped");
        }

        public bool IsRunning => _isRunning;


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
