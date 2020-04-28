// Copyright(C) 2020 Tetsuki Syu
// See Program.cs for the full notice.

using Discord;
using Discord.Commands;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using TaigaBotCS.Interfaces;
using TaigaBotCS.Utility;

namespace TaigaBotCS.Commands
{
    [Attributes.Command("cvt", "util", null, new[] { "convert" })]
    public class Convert : ModuleBase<SocketCommandContext>, IMemberConfigurable
    {
        private readonly string[] _lengths = new[]
        {
            "km", "m", "cm", "in", "ft", "mi", "au"
        };

        private readonly string[] _temps = new[]
        {
            "c", "f", "k"
        };

        private readonly string[] _validUnits;
        private readonly int _timeOut = 15 * 1000;
        private readonly Regex _cvtPattern = new Regex(@"(-?[0-9.]+)(\D{1,2})");
        private Dictionary<string, Dictionary<string, double>> _lookupLength
            = new Dictionary<string, Dictionary<string, double>>
            {
                { "km", new Dictionary<string, double>
                {
                    { "km", 1 }, { "m", 0.001 }, { "cm", 1e-5 }, { "in", 2.54e-5 },
                    { "ft", 0.0003048 }, { "mi", 1.60934 }, { "au", 1.496e+8 }
                } },

                { "m", new Dictionary<string, double>
                {
                    { "km", 1000 }, { "m", 1 }, { "cm", 0.01 }, { "in", 0.0254 },
                    { "ft", 0.3048 }, { "mi", 1609.34 }, { "au", 1.496e+11 }
                } },

                { "cm", new Dictionary<string, double>
                {
                    { "km", 100000 }, { "m", 100 }, { "cm", 1 }, { "in", 2.54 },
                    { "ft", 30.48 }, { "mi", 160934 }, { "au", 1.496e+13 }
                } },

                { "in", new Dictionary<string, double>
                {
                    { "km", 39370.1 }, { "m", 39.3701 }, { "cm", 0.393701 }, { "in", 1 },
                    { "ft", 12 }, { "mi", 63360 }, { "au", 5.89e+12 }
                } },

                { "ft", new Dictionary<string, double>
                {
                    { "km", 3280.84 }, { "m", 3.28084 }, { "cm", 0.0328084 }, { "in", 0.0833333 },
                    { "ft", 1 }, { "mi", 5280 }, { "au", 4.908e+11 }
                } },

                { "mi", new Dictionary<string, double>
                {
                    { "km", 0.621371 }, { "m", 0.000621371 }, { "cm", 6.21371e-6 }, { "in", 1.57828e-5 },
                    { "ft", 0.000189394 }, { "mi", 1 }, { "au", 9.296e+7 }
                } },

                { "au", new Dictionary<string, double>
                {
                    { "km", 6.68459e-9 }, { "m", 6.68459e-12 }, { "cm", 6.68459e-14 }, { "in", 1.69789e-13 },
                    { "ft", 2.03746e-12 }, { "mi", 1.07578e-8 }, { "au", 1 }
                } },
            };

        private Dictionary<string, Dictionary<string, double>> _lookupTemperature
            = new Dictionary<string, Dictionary<string, double>>
            {
                { "c", new Dictionary<string, double>
                {
                    { "c", 1 }, { "f", 5.0 / 9.0 }, { "k", 1 }
                } },

                { "f", new Dictionary<string, double>
                {
                    { "c", 9.0 / 5.0 }, { "f", 1 }, { "k", 9.0 / 5.0 }
                } },

                { "k", new Dictionary<string, double>
                {
                    { "c", 1 }, { "f", 5.0 / 9.0 }, { "k", 1 }
                } }
            };

        private Dictionary<ulong, Dictionary<string, object>> _cvtCommandTexts
            = new Dictionary<ulong, Dictionary<string, object>>();

        private enum ConversionError
        {
            LengthTooShort, InvalidUnit, WrongPattern
        }

        public Convert() : base()
        {
            var cvtText = Helper.GetLocalization("en").texts.cvt;
            var usage = cvtText["usage"].ToString()
                .Replace("{temps}", string.Join(", ", _temps));
            usage = usage.Replace("{heights}", string.Join(", ", _lengths));

            _validUnits = _lengths.Concat(_temps).ToArray();

            TypeDescriptor.AddAttributes(typeof(Convert),
                new Attributes.CommandAttribute("cvt", "util", usage, new[] { "convert" }));
        }

#pragma warning disable CS1998
        [Command("cvt")]
        [Alias("convert")]
        public async Task ConvertAsync()
            => _ = HandleErrorAsync(ConversionError.LengthTooShort, null);

        [Command("cvt")]
        [Alias("convert")]
        public async Task ConvertAsync(string targetUnit)
            => _ = HandleErrorAsync(ConversionError.LengthTooShort, null);

        [Command("cvt")]
        [Alias("convert")]
        public async Task ConvertAsync(string targetUnit, string sourceUnit)
        {
            var requestOptions = new RequestOptions();
            requestOptions.Timeout = _timeOut;

            targetUnit = targetUnit.ToLower();
            sourceUnit = sourceUnit.ToLower();

            SetMemberConfig(Context.User.Id);

            if (!_validUnits.Contains(targetUnit))
            {
                _ = HandleErrorAsync(ConversionError.InvalidUnit, null);
                return;
            }
            else if (!_cvtPattern.IsMatch(sourceUnit))
            {
                _ = HandleErrorAsync(ConversionError.WrongPattern, sourceUnit);
                return;
            }

            _ = SendMessageAndDeleteAfterTimeout(Context, ExecuteConversion(targetUnit, sourceUnit), _timeOut);
        }

        [Command("cvt")]
        [Alias("convert")]
        public async Task ConvertAsync(string targetUnit, string sourceUnit, params string[] rest)
            => _ = ConvertAsync(targetUnit, sourceUnit);
#pragma warning restore CS1998

        private async Task HandleErrorAsync(ConversionError error, string sourceUnit)
        {
            SetMemberConfig(Context.User.Id);
            var cvtCmd = _cvtCommandTexts[Context.User.Id];
            var cvtErrors = cvtCmd["errors"] as Dictionary<string, object>;

            var lengthTooShort = cvtErrors["length_too_short"].ToString()
                .Replace("{temps}", string.Join(", ", _temps))
                .Replace("{heights}", string.Join(", ", _lengths))
                .Replace("{prefix}", DotNetEnv.Env.GetString("PREFIX"));

            var invalidUnitMsg = cvtErrors["invalid_unit"].ToString()
                .Replace("{units}", string.Join(", ", _validUnits));

            var wrongPatterns = cvtErrors["wrong_pattern"].ToString()
                .Replace("{input}", sourceUnit);

            string msg = error switch
            {
                ConversionError.LengthTooShort => lengthTooShort,
                ConversionError.InvalidUnit => invalidUnitMsg,
                ConversionError.WrongPattern => wrongPatterns,
                _ => string.Empty
            };

            await SendMessageAndDeleteAfterTimeout(Context, msg, _timeOut);
        }

        private async Task SendMessageAndDeleteAfterTimeout(ICommandContext context,
            string message, int timeOut)
        {
            var requestOptions = new RequestOptions
            {
                Timeout = _timeOut
            };

            var task = await context.Channel.SendMessageAsync(message);
            await Task.Delay(timeOut);
            await task.DeleteAsync(requestOptions);
        }

        public void SetMemberConfig(ulong userId)
        {
            if (_cvtCommandTexts.ContainsKey(userId)) return;

            var responseText = Helper
                .GetLocalization(Helper.GetMemberConfig(userId)?.Language);
            _cvtCommandTexts[userId] = responseText.texts.cvt;
        }

        private bool AreCompatible(string targetUnit, string sourceUnit)
            => (_temps.Contains(targetUnit) && _temps.Contains(sourceUnit)) ||
            (_lengths.Contains(targetUnit) && _lengths.Contains(sourceUnit));

        private string ExecuteConversion(string targetUnit, string sourceUnit)
        {
            var groups = _cvtPattern.Match(sourceUnit).Groups;
            var value = groups.Values.ToArray()[1].Value;
            var unit = groups.Values.ToArray()[2].Value;
            var cvtCmd = _cvtCommandTexts[Context.User.Id];
            var cvtErrors = cvtCmd["errors"] as Dictionary<string, object>;

            if (!AreCompatible(targetUnit, unit))
                return cvtErrors["operation_not_possible"].ToString();

            var tables = new[] { _lookupLength, _lookupTemperature };

            var numberToConvert = double.Parse(value);
            if (double.IsNaN(numberToConvert))
                return cvtErrors["is_nan"].ToString();

            foreach (var type in tables)
            {
                if (!type.ContainsKey(targetUnit)) continue;
                if (!type[targetUnit].ContainsKey(unit)) continue;

                var result = 0.0;

                switch (targetUnit)
                {
                    case "c":
                        if (unit == "f")
                            numberToConvert -= 32;
                        else if (unit == "k")
                            numberToConvert -= 273.15;
                        result = type[targetUnit][unit] * numberToConvert;
                        break;
                    case "f":
                        result = type[targetUnit][unit] * numberToConvert;
                        if (unit == "c")
                            result += 32;
                        else if (unit == "k")
                            result -= 459.67;
                        break;
                    case "k":
                        if (unit == "c")
                            numberToConvert += 273.15;
                        else if (unit == "f")
                            numberToConvert += 459.67;
                        result = type[targetUnit][unit] * numberToConvert;
                        break;
                    default:
                        result = type[targetUnit][unit] * numberToConvert;
                        break;
                }

                return string.Format(cvtCmd["result"].ToString(),
                    value + UnitToDisplay(unit),
                    Math.Round(result, 5),
                    UnitToDisplay(targetUnit));
            }

            return cvtErrors["generic"].ToString();
        }

        private string UnitToDisplay(string unit)
            => unit switch
            {
                "c" => "\u2103",
                "f" => "\u00B0\u0046",
                "k" => "K",
                _ => unit
            };
    }
}
