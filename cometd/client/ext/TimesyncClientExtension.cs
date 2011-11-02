using System;
using System.Collections.Generic;
using Cometd.Bayeux;
using Cometd.Bayeux.Client;
using Cometd.Common;


namespace Cometd.Client.Ext
{
    public class TimesyncClientExtension : IExtension
    {
        public int Offset
        {
            get
            {
                return _offset;
            }
        }

        public int Lag
        {
            get
            {
                return _lag;
            }
        }

        public long ServerTime
        {
            get
            {
                return (DateTime.Now.Ticks - 621355968000000000) / 10000 + _offset;
            }
        }

        private volatile int _lag;
        private volatile int _offset;

        public bool rcv(IClientSession session, IMutableMessage message)
        {
            return true;
        }

        public bool rcvMeta(IClientSession session, IMutableMessage message)
        {
            Dictionary<String, Object> ext = (Dictionary<String, Object>)message.getExt(false);
            if (ext != null)
            {
                Dictionary<String, Object> sync = (Dictionary<String, Object>)ext["timesync"];
                if (sync != null)
                {
                    long now = (System.DateTime.Now.Ticks - 621355968000000000) / 10000;

                    long tc = ObjectConverter.ToInt64(sync["tc"], 0);
                    long ts = ObjectConverter.ToInt64(sync["ts"], 0);
                    int p = ObjectConverter.ToInt32(sync["p"], 0);
                    // final int a=((Number)sync.get("a")).intValue();

                    int l2 = (int)((now - tc - p) / 2);
                    int o2 = (int)(ts - tc - l2);

                    _lag = _lag == 0 ? l2 : (_lag + l2) / 2;
                    _offset = _offset == 0 ? o2 : (_offset + o2) / 2;
                }
            }

            return true;
        }

        public bool send(IClientSession session, IMutableMessage message)
        {
            return true;
        }

        public bool sendMeta(IClientSession session, IMutableMessage message)
        {
            Dictionary<String, Object> ext = (Dictionary<String, Object>)message.getExt(true);
            long now = (System.DateTime.Now.Ticks - 621355968000000000) / 10000;
            // Changed JSON.Literal to String
            String timesync = "{\"tc\":" + now + ",\"l\":" + _lag + ",\"o\":" + _offset + "}";
            ext["timesync"] = timesync;
            return true;
        }
    }
}
