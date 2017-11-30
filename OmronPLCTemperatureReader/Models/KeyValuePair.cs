using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OmronPLCTemperatureReader.Models
{
    [Serializable]
    public class KeyValuePair<TKey, TValue>
    {
        private TKey _key;
        private TValue _value;

        public KeyValuePair()
        {
            _key = default(TKey);
            _value = default(TValue);
        }

        public KeyValuePair(TKey key, TValue value)
        {
            _key = key;
            _value = value;
        }

        public TKey Key
        {
            get { return _key; }
            set { _key = value; }
        }

        public TValue Value
        {
            get { return _value; }
            set { _value = value; }
        }
    }
}
