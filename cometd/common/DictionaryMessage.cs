using System;
using System.IO;
using System.Collections.Generic;
using System.ServiceModel.Web;
using System.Runtime.Serialization.Json;
using System.Web.Script.Serialization;
using Cometd.Bayeux;

namespace Cometd.Common
{
    [Serializable]
    public class DictionaryMessage : Dictionary<String, Object>, IMutableMessage
    {
        private const long serialVersionUID = 4318697940670212190L;

        public DictionaryMessage()
        {
        }

        public DictionaryMessage(IDictionary<String, Object> message)
        {
            foreach (KeyValuePair<String, Object> kvp in message)
            {
                this.Add(kvp.Key, kvp.Value);
            }
        }

        public IDictionary<String, Object> Advice
        {
            get
            {
                Object advice;
                this.TryGetValue(Message_Fields.ADVICE_FIELD, out advice);
                if (advice is String)
                {
                    advice = jsonParser.Deserialize<IDictionary<String, Object>>(advice as String);
                    this[Message_Fields.ADVICE_FIELD] = advice;
                }
                return (IDictionary<String, Object>)advice;
            }
        }

        public String Channel
        {
            get
            {
                Object obj;
                this.TryGetValue(Message_Fields.CHANNEL_FIELD, out obj);
                return (String)obj;
            }
            set
            {
                this[Message_Fields.CHANNEL_FIELD] = value;
            }
        }

        public ChannelId ChannelId
        {
            get
            {
                return new ChannelId(Channel);
            }
        }

        public String ClientId
        {
            get
            {
                Object obj;
                this.TryGetValue(Message_Fields.CLIENT_ID_FIELD, out obj);
                return (String)obj;
            }
            set
            {
                this[Message_Fields.CLIENT_ID_FIELD] = value;
            }

        }

        public Object Data
        {
            get
            {
                Object obj;
                this.TryGetValue(Message_Fields.DATA_FIELD, out obj);
                return obj;
            }
            set
            {
                this[Message_Fields.DATA_FIELD] = value;
            }
        }

        public IDictionary<String, Object> DataAsDictionary
        {
            get
            {
                Object data;
                this.TryGetValue(Message_Fields.DATA_FIELD, out data);
                if (data is String)
                {
                    data = jsonParser.Deserialize<Dictionary<String, Object>>(data as String);
                    this[Message_Fields.DATA_FIELD] = data;
                }
                return (Dictionary<String, Object>)data;
            }
        }

        public IDictionary<String, Object> Ext
        {
            get
            {
                Object ext;
                this.TryGetValue(Message_Fields.EXT_FIELD, out ext);
                if (ext is String)
                {
                    ext = jsonParser.Deserialize<Dictionary<String, Object>>(ext as String);
                    this[Message_Fields.EXT_FIELD] = ext;
                }
                return (Dictionary<String, Object>)ext;
            }
        }

        public String Id
        {
            get
            {
                Object obj;
                this.TryGetValue(Message_Fields.ID_FIELD, out obj);
                return (String)obj;
            }
            set
            {
                this[Message_Fields.ID_FIELD] = value;
            }
        }

        public String JSON
        {
            get
            {
                return jsonParser.Serialize(this as IDictionary<String, Object>);
            }
        }

        public IDictionary<String, Object> getAdvice(bool create)
        {
            IDictionary<String, Object> advice = Advice;
            if (create && advice == null)
            {
                advice = new Dictionary<String, Object>();
                this[Message_Fields.ADVICE_FIELD] = advice;
            }
            return advice;
        }

        public IDictionary<String, Object> getDataAsDictionary(bool create)
        {
            IDictionary<String, Object> data = DataAsDictionary;
            if (create && data == null)
            {
                data = new Dictionary<String, Object>();
                this[Message_Fields.DATA_FIELD] = data;
            }
            return data;
        }

        public IDictionary<String, Object> getExt(bool create)
        {
            IDictionary<String, Object> ext = Ext;
            if (create && ext == null)
            {
                ext = new Dictionary<String, Object>();
                this[Message_Fields.EXT_FIELD] = ext;
            }
            return ext;
        }

        public bool Meta
        {
            get
            {
                return ChannelId.isMeta(Channel);
            }
        }

        public bool Successful
        {
            get
            {
                Object obj;
                this.TryGetValue(Message_Fields.SUCCESSFUL_FIELD, out obj);
                return ObjectConverter.ToBoolean(obj, false);
            }
            set
            {
                this[Message_Fields.SUCCESSFUL_FIELD] = value;
            }
        }

        public override String ToString()
        {
            return JSON;
        }

        public static IList<IMutableMessage> parseMessages(String content)
        {
            IList<IDictionary<String, Object>> dictionaryList = null;
            try
            {
                dictionaryList = jsonParser.Deserialize<IList<IDictionary<String, Object>>>(content);
            }
            catch (Exception e)
            {
                Console.WriteLine("Exception when parsing json {0}", e);
            }

            IList<IMutableMessage> messages = new List<IMutableMessage>();
            if (dictionaryList == null)
            {
                return messages;
            }

            foreach (IDictionary<String, Object> message in dictionaryList)
            {
                if (message != null)
                    messages.Add(new DictionaryMessage(message));
            }

            return messages;
        }

        protected static JavaScriptSerializer jsonParser = new JavaScriptSerializer();
    }
}
