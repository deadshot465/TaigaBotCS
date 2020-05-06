using System.Collections.Generic;

namespace TaigaBotCS.Services
{
    public class PersistenceService
    {
        private Dictionary<string, object> _savedData
            = new Dictionary<string, object>();

        public bool SaveData<T>(string key, T value)
        {
            _savedData[key] = value;
            return true;
        }

        public T GetSavedData<T>(string key)
        {
            return (T)_savedData[key];
        }
    }
}
