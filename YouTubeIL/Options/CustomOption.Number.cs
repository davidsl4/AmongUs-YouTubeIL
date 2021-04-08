using UnityEngine;

namespace YouTubeIL.Options
{
    internal interface INumberOption
    {
        public void Increase();
        public void Decrease();
    }
    
    internal sealed class CustomNumberOption : CustomOption, INumberOption
    {
        /// <summary>
        /// The lowest permitted value.
        /// </summary>
        private readonly float _min;
        /// <summary>
        /// The highest permitted value.
        /// </summary>
        private readonly float _max;
        /// <summary>
        /// The increment or decrement steps when <see cref="Increase"/> or <see cref="Decrease"/> are called.
        /// </summary>
        private readonly float _increment;

        private readonly string _suffix;

        public CustomNumberOption(string id, string name, float value, float min = 0.25f, float max = 5f,
            float increment = 0.25f, string suffix = "", CustomOption parentOption = null) : base(id, name, CustomOptionType.Number, value,
            parentOption)
        {
            _min = Mathf.Min(value, min);
            _max = Mathf.Max(value, max);
            _increment = increment;
            _suffix = suffix;
            
            ValueStringFormat = (_, v) => v + suffix;
        }

        protected override bool GameOptionCreated(OptionBehaviour o)
        {
            base.GameOptionCreated(o);
            
            if (o is not NumberOption number) return false;
            number.TitleText.Text = GetFormattedName();
            number.ValidRange = new FloatRange(_min, _max);
            number.Increment = _increment;
            number.Value = number.oldValue = GetValue();
            number.ValueText.Text = GetFormattedValue();

            return true;
        }
        
        public void Increase()
        {
            SetValue(GetValue() + _increment);
        }

        public void Decrease()
        {
            SetValue(GetValue() - _increment);
        }

        private void SetValue(float value, bool raiseEvents = true)
        {
            value = Mathf.Clamp(value, _min, _max);

            base.SetValue(value, raiseEvents);
        }

        private float GetValue() => GetValue<float>();
    }

    internal abstract partial class CustomOption
    {
        /// <summary>
        /// Adds a number option.
        /// </summary>
        /// <param name="id">The ID of the option, used to transmit the value between players</param>
        /// <param name="name">The name/title of the option</param>
        /// <param name="value">The initial/default value</param>
        /// <param name="min">The lowest value permitted, may be overriden if <paramref name="value"/> is lower</param>
        /// <param name="max">The highest value permitted, may be overriden if <paramref name="value"/> is higher</param>
        /// <param name="increment">The increment or decrement steps when <see cref="CustomNumberOption.Increase"/> or <see cref="CustomNumberOption.Decrease"/> are called</param>
        /// <param name="suffix">The ending for the number in the views</param>
        /// <param name="parentOption">The parent option</param>
        public static CustomNumberOption AddNumber(string id, string name, float value, float min = 0.25F,
            float max = 5F, float increment = 0.25F, string suffix = "", CustomOption parentOption = null) =>
            new(id, name, value, min, max, increment, suffix, parentOption);
    }
}