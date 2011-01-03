using System;
using System.Collections.Generic;
using Cometd.Bayeux;

namespace Cometd.Common
{
	public class AbstractTransport : ITransport
	{
        private String _name;
        protected IDictionary<String, Object> _options;
        protected List<String> _prefix;

        public AbstractTransport(String name, IDictionary<String, Object> options)
		{
			_name = name;
			_options = options == null ? new Dictionary<String, Object>() : options;
            _prefix  = new List<String>();
		}

        public String Name
		{
			get
			{
				return _name;
			}
		}

		public Object getOption(String name)
		{
			Object value = null;
            _options.TryGetValue(name, out value);

            String prefix = null;

            foreach (String segment in _prefix)
			{
				prefix = prefix == null ? segment : (prefix + "." + segment);
				String key = prefix + "." + name;

                if (_options.ContainsKey(key))
                    value = key;
			}

            return value;
		}

        public void setOption(String name, Object value)
		{
			String prefix = OptionPrefix;
			_options.Add(prefix == null ? name : (prefix + "." + name), value);
		}

        public ICollection<String> OptionNames
        {
            get
            {
                return _options.Keys;
            }
        }

        public String OptionPrefix
		{
			get
			{
				String prefix = null;
				foreach (String segment in _prefix) 
				    prefix = prefix == null ? segment:(prefix + "." + segment);
				
                return prefix;
			}
		}
		
		public String getOption(String option, String dftValue)
		{
			Object value = getOption(option);
            return ObjectConverter.ToString(value, dftValue);
		}
		
		public long getOption(String option, long dftValue)
		{
			Object value = getOption(option);
            return ObjectConverter.ToInt64(value, dftValue);
		}
		
		public int getOption(String option, int dftValue)
		{
			Object value = getOption(option);
            return ObjectConverter.ToInt32(value, dftValue);
        }
		
		public bool getOption(String option, bool dftValue)
		{
			Object value = getOption(option);
            return ObjectConverter.ToBoolean(value, dftValue);
        }
	}
}