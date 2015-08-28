using System;
using System.IO;

namespace ValidationTool
{
    public class Logging
    {
        private readonly object _syncObject = new Object();
        private readonly TextWriter _tw;
        public Logging(string fileName)
        {
            _tw = new StreamWriter(fileName);
        }
        public void Log(string message, params object[] arg)
        {
            lock (_syncObject)
            {
                var m = string.Format(message, arg);
                _tw.WriteLine("{0}", m);
                _tw.Flush();
            }
        }
    }
}
