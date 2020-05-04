using System;
using System.Collections.Generic;
using System.IO;
using Utf8Json;

namespace TaigaBotCS.Services
{
    public class ReminderService
    {
        private const string _reminderPath = "./storage/reminders.json";

        private Dictionary<ulong, Tuple<DateTime, string>> _reminders
            = new Dictionary<ulong, Tuple<DateTime, string>>();
        private DateTime _lastTime;

        public ReminderService(IServiceProvider services)
        {
            _lastTime = DateTime.Now;
            
            if (File.Exists(_reminderPath))
            {
                var rawJson = File.ReadAllText(_reminderPath);
                _reminders = JsonSerializer
                    .Deserialize<Dictionary<ulong, Tuple<DateTime, string>>>(rawJson);
                Console.WriteLine("Reminders successfully read.");
            }
        }

        public DateTime SetReminder(ulong userId, DateTime dueTime, string msg)
        {
            _reminders[userId] = new Tuple<DateTime, string>(dueTime, msg);
            return dueTime;
        }

        public IEnumerable<Tuple<ulong, string>> RemindUser()
        {
            foreach (var reminder in _reminders)
            {
                if (DateTime.Now > reminder.Value.Item1)
                {
                    var item = new Tuple<ulong, string>(reminder.Key, reminder.Value.Item2);
                    _reminders.Remove(reminder.Key);
                    yield return item;
                }
            }

            if ((DateTime.Now - _lastTime).TotalMinutes >= 10)
            {
                Console.WriteLine("Writing reminders...");
                WriteReminders();
                _lastTime = DateTime.Now;
            }
        }

        private void WriteReminders()
        {
            var rawJson = JsonSerializer.ToJsonString(_reminders);
            File.WriteAllText(_reminderPath, rawJson);
        }
    }
}
