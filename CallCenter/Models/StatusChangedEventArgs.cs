using System;

namespace CallCenter.Models
{
    public class StatusChangedEventArgs : EventArgs
    {
        public string Message { get; set; }
    }
}
