using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Cometd.Bayeux;

namespace Cometd.Common
{
    class Print
    {
        public static String List(IList<String> L)
        {
            String s = "";
            foreach (String e in L) s += " '" + e + "'";
            return s;
        }

        public static String Dictionary(IDictionary<String, Object> D)
        {
            if (D == null) return " (null)";
            if (!(D is IDictionary<String, Object>)) return " (invalid)";
            String s = "";
            foreach (KeyValuePair<String, Object> kvp in D)
            {
                s += " '" + kvp.Key + ":";
                if (kvp.Value is IDictionary<String, Object>)
                    s += Dictionary(kvp.Value as IDictionary<String, Object>);
                else
                    s += kvp.Value.ToString();
                s += "'";
            }
            return s;
        }

        public static String Messages(IList<IMessage> M)
        {
            if (M == null) return " (null)";
            if (!(M is IList<IMessage>)) return " (invalid)";
            String s = "[";
            foreach (IMessage m in M)
            {
                s += " " + m;
            }
            s += " ]";
            return s;
        }

        public static String Messages(IList<IMutableMessage> M)
        {
            if (M == null) return " (null)";
            if (!(M is IList<IMutableMessage>)) return " (invalid)";
            String s = "[";
            foreach (IMutableMessage m in M)
            {
                s += " " + m;
            }
            s += " ]";
            return s;
        }
    }
}
