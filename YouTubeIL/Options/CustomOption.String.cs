using System;
using System.Collections.Generic;
using UnityEngine;

namespace YouTubeIL.Options
{
    internal interface IStringOption
    {
        public void Increase();
        public void Decrease();
        public string GetText();
    }
    
    internal sealed class CustomStringOption : CustomOption, IStringOption
    {
        private readonly string[] _values;
        
        /// <summary>
        /// The text values the option can present.
        /// </summary>
        public IReadOnlyCollection<string> Values => Array.AsReadOnly(_values);

        public CustomStringOption(string id, string name, string[] values, uint value,
            CustomOption parentOption = null) : base(id, name, CustomOptionType.String, (int)value, parentOption)
        {
            _values = values;
            ValueStringFormat = (_, v) => _values[(int) v];
        }

        protected override bool GameOptionCreated(OptionBehaviour o)
        {
            base.GameOptionCreated(o);
            
            if (o is not StringOption str) return false;

            str.TitleText.Text = GetFormattedName();

            str.Value = str.oldValue = GetValue();

            str.ValueText.Text = GetFormattedValue();
            
            return true;
        }
        
        public void Increase()
        {
            var newValue = Mathf.Clamp(GetValue() + 1, 0, _values.Length - 1);
            SetValue(newValue);
        }

        public void Decrease()
        {
            var newValue = Mathf.Clamp(GetValue() - 1, 0, _values.Length - 1);
            SetValue(newValue);
        }

        private void SetValue(int value, bool raiseEvents = true)
        {
            if (value < 0 || value >= _values.Length) value = GetDefaultValue();

            base.SetValue(value, raiseEvents);
        }
        
        /// <returns>The int-casted default value.</returns>
        private int GetDefaultValue()
        {
            return GetDefaultValue<int>();
        }

        /// <returns>The int-casted old value.</returns>
        public int GetOldValue()
        {
            return GetOldValue<int>();
        }

        /// <returns>The int-casted current value.</returns>
        private int GetValue()
        {
            return GetValue<int>();
        }

        /// <returns>The text at index <paramref name="value"/>.</returns>
        private string GetText(int value)
        {
            return _values[value];
        }

        /// <returns>The current text.</returns>
        public string GetText()
        {
            return GetText(GetValue());
        }
    }

    internal abstract partial class CustomOption
    {
        public static CustomStringOption AddString(string id, string name, string[] values, uint defaultValue = 0,
            CustomOption parentOption = null) => new(id, name, values, defaultValue, parentOption);
    }
}