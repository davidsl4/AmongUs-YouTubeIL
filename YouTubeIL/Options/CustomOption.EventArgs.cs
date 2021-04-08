using System;
using System.ComponentModel;

namespace YouTubeIL.Options
{
    public class OptionOnValueChangedEventArgs : CancelEventArgs
    {
        public object Value { get; set; }

        public object OldValue { get; }

        public OptionOnValueChangedEventArgs(object value, object oldValue)
        {
            Value = value;
            OldValue = oldValue;
        }
    }
    
    public class OptionValueChangedEventArgs : EventArgs
    {
        public readonly object OldValue;
        public readonly object Value;

        public OptionValueChangedEventArgs(object value, object oldValue)
        {
            Value = value;
            OldValue = oldValue;
        }
    }
}