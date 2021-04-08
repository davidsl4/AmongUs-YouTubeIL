using System;
using System.Collections.Generic;
using System.Linq;
using Hazel;
using YouTubeIL.Networking;
using YouTubeIL.Patches;
using static AmongUsClient;

namespace YouTubeIL.Options
{
    internal enum CustomOptionType : byte
    {
        /// <summary>
        /// A checkmark toggle option.
        /// </summary>
        Toggle,
        /// <summary>
        /// A float number option with increase/decrease buttons.
        /// </summary>
        Number,
        /// <summary>
        /// A string option (underlying int) with forward/back arrows.
        /// </summary>
        String
    }
    
    internal abstract partial class CustomOption
    {
        /// <summary>
        /// List of all the added custom options.
        /// </summary>
        private static readonly List<CustomOption> Options = new();
        
        protected readonly string ID;
        protected readonly string Name;
        protected readonly object DefaultValue;
        protected readonly CustomOptionType OptionType;
        protected object OldValue;
        protected object Value;
        protected readonly CustomOption ParentOption;
        protected readonly HashSet<CustomOption> ChildOptions = new();
        protected OptionBehaviour GameOption;

        protected Func<CustomOption, string, string> NameStringFormat { get; set; } = (_, name) => name;

        protected Func<CustomOption, object, string> ValueStringFormat { get; set; } =
            (_, value) => value.ToString();

        protected Func<CustomOption, string, object, string> HudStringFormat { get; set; } =
            (_, name, value) => name + ": " + value;

        protected Func<CustomOption, object, bool> ValueEnabledConverter { get; set; } =
            (_, value) => (value as bool?).GetValueOrDefault(true);
        
        /// <summary>
        /// An event raised before a value change occurs, can alter the final value or cancel the value change. Only raised for the lobby host.
        /// </summary>
        public event EventHandler<OptionOnValueChangedEventArgs> OnValueChanged;
        /// <summary>
        /// An event raised after the option value has changed.
        /// </summary>
        public event EventHandler<OptionValueChangedEventArgs> ValueChanged;


        internal CustomOption(string id, string name, CustomOptionType optionType, object value,
            CustomOption parentOption = null)
        {
            if (string.IsNullOrEmpty(id))
                throw new ArgumentNullException(nameof(id), "Option id cannot be null or empty.");
            if (string.IsNullOrEmpty(name))
                throw new ArgumentNullException(nameof(name), "Option name cannot be null or empty.");

            ID = GetSafeOptionId(id);
            Name = name;
            OptionType = optionType;
            ParentOption = parentOption;
            ParentOption?.ChildOptions.Add(this);
            DefaultValue = OldValue =
                Value = value ?? throw new ArgumentNullException(nameof(value), "Value cannot be null");
            Options.Add(this);
            RegisterUnregisterRpc(Options.Count - 1);
        }

        ~CustomOption()
        {
            Options.Remove(this);
            RegisterUnregisterRpc(Options.Count + 1);
        }

        private static string GetSafeOptionId(string defaultId)
        {
            var id = defaultId;
            var i = 0;
            while (Options.Any(option => option.ID.Equals(id, StringComparison.Ordinal)))
                id = $"{id}_{++i}";
            return id;
        }

        private static void RegisterUnregisterRpc(int oldCount)
        {
            switch (Options.Count)
            {
                case > 0 when oldCount == 0:
                    HandleRpcPatch.RegisterCustomRpc(CustomRpc.SyncCustomOption, SyncCustomOptionHandler);
                    break;
                case 0 when oldCount > 0:
                    HandleRpcPatch.UnregisterCustomRpc(CustomRpc.SyncCustomOption);
                    break;
            }
        }

        private static void SyncCustomOptionHandler(MessageReader messageReader)
        {
            // structure: count of options, option1id, option1value, option2id, option2value...
            
            var count = messageReader.ReadByte();
            for (byte i = 0; i < count; i++)
            {
                var optionId = messageReader.ReadString();
                var customOption = Options.FirstOrDefault(o => o.ID.Equals(optionId, StringComparison.Ordinal));
                if (customOption == null)
                {
                    YouTubeIL.Logger.LogError($"Received option that could not be found, id: {optionId}");
                    return;
                }

                object newValue = customOption.OptionType switch
                {
                    CustomOptionType.Toggle => messageReader.ReadBoolean(),
                    CustomOptionType.Number => messageReader.ReadSingle(),
                    CustomOptionType.String => messageReader.ReadInt32(),
                    _ => throw new ArgumentOutOfRangeException()
                };
                customOption.SetValue(newValue);
            }
        }

        private static void WriteCustomOptionsToPacket(MessageWriter messageWriter, params CustomOption[] options)
        {
            messageWriter.Write(options.Length);
            foreach (var customOption in options)
            {
                messageWriter.Write(customOption.ID);
                messageWriter.Write((byte)customOption.OptionType);
                switch (customOption.OptionType)
                {
                    case CustomOptionType.Toggle:
                        messageWriter.Write(customOption.GetValue<bool>());
                        break;
                    case CustomOptionType.Number:
                        messageWriter.Write(customOption.GetValue<float>());
                        break;
                    case CustomOptionType.String:
                        messageWriter.Write(customOption.GetValue<int>());
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }
        }

        private static void SendSyncCustomOptions(params CustomOption[] options)
        {
            var messageWriter = Instance.StartRpc(PlayerControl.LocalPlayer.NetId,
                (byte) Networking.RpcCalls.YouTubeIL, SendOption.Reliable);
            messageWriter.Write(YouTubeIL.Id);
            messageWriter.Write((byte)CustomRpc.SyncCustomOption);
            WriteCustomOptionsToPacket(messageWriter, options);
            messageWriter.EndMessage();
        }
        
        /// <returns><see cref="Name"/> passed through <see cref="NameStringFormat"/>.</returns>
        protected string GetFormattedName()
        {
            return NameStringFormat.Invoke(this, Name);
        }

        /// <returns><see cref="Value"/> passed through <see cref="ValueStringFormat"/>.</returns>
        protected string GetFormattedValue()
        {
            return ValueStringFormat.Invoke(this, Value);
        }

        /// <returns><see cref="object.ToString()"/> or the return value of <see cref="ValueStringFormat"/> when provided.</returns>
        public override string ToString()
        {
            return HudStringFormat.Invoke(this, GetFormattedName(), GetFormattedValue());
        }

        protected virtual bool GameOptionCreated(OptionBehaviour o)
        {
            GameOption = o;
            if (ParentOption != null)
            {
                GameOption.enabled = ParentOption.IsValueEnabled();
            }
            return true;
        }

        /// <summary>
        /// Returns event args of type <see cref="OptionOnValueChangedEventArgs"/> or a derivative.
        /// </summary>
        /// <param name="newValue">The new value</param>
        protected virtual OptionOnValueChangedEventArgs CreateOnValueChangedEventArgs(object newValue) =>
            new (newValue, Value);

        /// <summary>
        /// Returns event args of type <see cref="OptionValueChangedEventArgs"/> or a derivative.
        /// </summary>
        /// <param name="value">The new value</param>
        private OptionValueChangedEventArgs ValueChangedEventArgs(object value) =>
            new(value, Value);
        
        /// <summary>
        /// Sets the option's value.
        /// </summary>
        /// <remarks>
        /// Does nothing when the value type differs or when the value matches the current value.
        /// </remarks>
        /// <param name="value">The new value</param>
        /// <param name="raiseEvents">Whether or not to raise events</param>
        protected void SetValue(object value, bool raiseEvents = true)
        {
            if (value?.GetType() != Value?.GetType() || Value == value) return; // Refuse value updates that don't match the option type.

            if (raiseEvents && OnValueChanged != null && Instance is {AmHost: true} && PlayerControl.LocalPlayer)
            {
                var lastValue = value;

                var args = CreateOnValueChangedEventArgs(value);
                if (args != null)
                {
                    foreach (var @delegate in OnValueChanged!.GetInvocationList())
                    {
                        var handler = (EventHandler<OptionOnValueChangedEventArgs>) @delegate;
                        handler(this, args);

                        if (args.Value?.GetType() != value?.GetType())
                        {
                            args.Value = lastValue;
                            args.Cancel = false;

                            YouTubeIL.Logger.LogWarning(
                                $"A handler for option \"{Name}\" attempted to change value type, ignored.");
                        }

                        lastValue = args.Value;

                        if (args.Cancel) return; // Handler cancelled value change.
                    }
                    
                    value = args.Value;
                }
            }

            if (!Equals(OldValue, Value)) OldValue = Value;

            Value = value;

            if (Instance.AmHost)
                SendSyncCustomOptions(this);

            UpdateGameOption();

            if (raiseEvents) ValueChanged?.Invoke(this, ValueChangedEventArgs(value));

            try
            {
                var enabledState = IsValueEnabled();
                foreach (var customOption in ChildOptions.Where(customOption => customOption.GameOption != null))
                {
                    customOption.GameOption.enabled = enabledState;
                }
            }
            catch
            {
                // ignored
            }
        }
        
        /// <summary>
        /// Called when the option value changes, used to reflect the change visually with the <see cref="GameOption"/> object.
        /// </summary>
        private void UpdateGameOption()
        {
            try
            {
                switch (GameOption)
                {
                    case ToggleOption toggle:
                    {
                        if (Value is not bool newValue) return;

                        toggle.oldValue = newValue;
                        if (toggle.CheckMark != null) toggle.CheckMark.enabled = newValue;
                        break;
                    }
                    case NumberOption number:
                    {
                        if (Value is float newValue) number.Value = newValue;
                        if (number.ValueText != null) number.ValueText.Text = GetFormattedValue();
                        break;
                    }
                    case StringOption str:
                    {
                        if (Value is int newValue) str.Value = str.oldValue = newValue;

                        if (str.ValueText != null) str.ValueText.Text = GetFormattedValue();
                        break;
                    }
                    case KeyValueOption kv:
                    {
                        if (Value is int newValue) kv.Selected = kv.oldValue = newValue;

                        if (kv.ValueText != null) kv.ValueText.Text = GetFormattedValue();
                        break;
                    }
                }
            }
            catch (Exception e)
            {
                YouTubeIL.Logger.LogWarning($"Failed to update game setting value for option \"{Name}\": {e}");
            }
        }
        
        /// <summary>
        /// Gets the option value casted to <typeparamref name="T"/>
        /// </summary>
        /// <typeparam name="T">The type to cast the value to</typeparam>
        /// <returns>The casted value.</returns>
        protected T GetValue<T>()
        {
            return (T)Value;
        }

        /// <summary>
        /// Gets the default option value casted to <typeparamref name="T"/>
        /// </summary>
        /// <typeparam name="T">The type to cast the value to</typeparam>
        /// <returns>The casted default value.</returns>
        protected T GetDefaultValue<T>()
        {
            return (T)DefaultValue;
        }

        /// <summary>
        /// Gets the old option value casted to <typeparamref name="T"/>
        /// </summary>
        /// <typeparam name="T">The type to cast the value to</typeparam>
        /// <returns>The casted old value.</returns>
        protected T GetOldValue<T>()
        {
            return (T)OldValue;
        }

        private bool IsValueEnabled() => ValueEnabledConverter.Invoke(this, Value);
    }
}