using System;
using System.Diagnostics;
using System.Threading;

namespace CallCenter.Models
{
    public class Operator
    {
        public int Id { get; }
        public OperatorTitle Title { get; }
        public bool IsBusy { get; private set; }

        private bool _isRunning = true;
        private bool _changedStatus = false;
        //private DateTime _stop = DateTime.MinValue;
        private int _leftDuration;
        private const int period = 100;
        private Call _currentCall;


        public event EventHandler<StatusChangedEventArgs> StatusChanged;

        public Operator(int id, OperatorTitle title)
        {
            Id = id;
            Title = title;
            Debug.WriteLine($"Operator {id} of type {Title} created");
        }

        public void Start()
        {
            StatusChanged?.Invoke(this, new StatusChangedEventArgs
            {
                Message = $"Operator {Id} of type {Title} started, thread id: {Thread.CurrentThread.ManagedThreadId}"
            });

            while (_isRunning)
            {
                if (_changedStatus)
                {
                    StatusChanged?.Invoke(this, new StatusChangedEventArgs
                    {
                        Message = $"Operator {Id} of type {Title} is now busy for {_leftDuration / 1000} secs, thread id: {Thread.CurrentThread.ManagedThreadId}"
                    });
                    _changedStatus = false;
                }

                if (IsBusy && _leftDuration <= 0 && _currentCall != null)
                {
                    IsBusy = false;
                    _currentCall.IsActive = false;
                    _currentCall = null;

                    StatusChanged?.Invoke(this, new StatusChangedEventArgs
                    {
                        Message = $"{Title} {Id} ended a call, thread id: {Thread.CurrentThread.ManagedThreadId}"
                    });

                    StatusChanged?.Invoke(this, new StatusChangedEventArgs
                    {
                        Message = $"Hello! I'm {Title} {Id}, thread id: {Thread.CurrentThread.ManagedThreadId}"
                    });
                }

                Thread.Sleep(period);
                if (IsBusy)
                {
                    _leftDuration -= period;
                }
            }
        }

        public void Answer(Call call)
        {
            if (IsBusy) throw new Exception("Operator is busy!");
            _currentCall = call;
            _leftDuration = _currentCall.Duration * 1000;
            IsBusy = true;
            _changedStatus = true;
        }

        public void Kill()
        {
            _isRunning = false;
        }
    }
}
