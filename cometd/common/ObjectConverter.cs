using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Cometd.Bayeux;

namespace Cometd.Common
{
    class ObjectConverter
    {
        public static String ToString(Object obj, String defaultValue)
        {
            if (obj == null) return defaultValue;

            try { return obj.ToString(); }
            catch (Exception) { }

            return defaultValue;
        }

        public static Int64 ToInt64(Object obj, Int64 defaultValue)
        {
            if (obj == null) return defaultValue;

            try { return Convert.ToInt64(obj); }
            catch (Exception) { }

            try { return Int64.Parse(obj.ToString()); }
            catch (Exception) { }

            return defaultValue;
        }

        public static Int32 ToInt32(Object obj, Int32 defaultValue)
        {
            if (obj == null) return defaultValue;

            try { return Convert.ToInt32(obj); }
            catch (Exception) { }

            try { return Int32.Parse(obj.ToString()); }
            catch (Exception) { }

            return defaultValue;
        }

        public static Boolean ToBoolean(Object obj, Boolean defaultValue)
        {
            if (obj == null) return defaultValue;

            try { return Convert.ToBoolean(obj); }
            catch (Exception) { }

            try { return Boolean.Parse(obj.ToString()); }
            catch (Exception) { }

            return defaultValue;
        }

        public static IList<IMessage> ToListOfIMessage(IList<IMutableMessage> M)
        {
            List<IMessage> R = new List<IMessage>();
            foreach (IMutableMessage m in M)
            {
                R.Add((IMessage)m);
            }
            return R;
        }

        public static IList<IDictionary<String, Object>> ToListOfDictionary(IList<IMutableMessage> M)
        {
            IList<IDictionary<String, Object>> R = new List<IDictionary<String, Object>>();

            foreach (IMutableMessage m in M)
            {
                R.Add((IDictionary<String, Object>)m);
            }
            return R;
        }
    }
}
