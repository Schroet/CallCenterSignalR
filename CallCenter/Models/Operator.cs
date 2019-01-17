using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

namespace CallCenter.Models
{
    public class Operator
    {
        public int Id { get; }
        public OperatorTitle Title { get; }
        public bool IsBusy { get; private set; }

        private bool _isRunning = true;
        private bool _changedStatus = false;
        private DateTime _stop = DateTime.MinValue;

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
                        Message = $"Operator {Id} of type {Title} is now busy till {_stop} seconds, thread id: {Thread.CurrentThread.ManagedThreadId}"
                    });
                    _changedStatus = false;
                }

                if (IsBusy && DateTime.Now >= _stop)
                {
                    IsBusy = false;

                    StatusChanged?.Invoke(this, new StatusChangedEventArgs
                    {
                        Message = $"Employee {Title} {Id} ended a call, thread id: {Thread.CurrentThread.ManagedThreadId}" 
                    });

                    StatusChanged?.Invoke(this, new StatusChangedEventArgs
                    {
                        Message = $"Hello! I'm {Title} {Id}, thread id: {Thread.CurrentThread.ManagedThreadId}"
                        //Message = $"Operator {Id} of type {Title} is free now, thread id: {Thread.CurrentThread.ManagedThreadId}"
                    });    
                }

                Thread.Sleep(100);
            }
        }

        public void Answer(int duration)
        {
            if (IsBusy) throw new Exception("Operator is busy!");
            _stop = DateTime.Now.AddSeconds(duration);
            IsBusy = true;
            _changedStatus = true;

            //StatusChanged?.Invoke(this, new StatusChangedEventArgs
            //{
            //    Message = $"Hello! I'm {Title} {Id}, thread id: {Thread.CurrentThread.ManagedThreadId}"
            //});

        }

        public void Kill()
        {
            _isRunning = false;
        }
    }
}
