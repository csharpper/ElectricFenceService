using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Fence.Util
{
    class Parser
    {
        string _message = "";
        public string[] Parse(string message)
        {
            lock (_message)
            {
                _message += message;
                return getAll();
            }
        }

        private string[] getAll()
        {
            var datas = _message.Split(new char[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
            if (_message.LastOrDefault() == '\r' || _message.LastOrDefault() == '\n')
            {
                _message = "";
                return datas;
            }
            else
            {
                _message = datas.LastOrDefault();
                return datas.Take(datas.Length - 1).ToArray();
            }
        }
    }
}
