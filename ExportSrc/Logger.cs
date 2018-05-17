#region usings

using System;
using System.Diagnostics;

#endregion

namespace ExportSrc
{
    public class Logger
    {
        private static Logger _current;

        public Logger()
        {
            Minimum = long.MinValue;
            Maximum = long.MaxValue;
        }

        public static Logger Current
        {
            get
            {
                if (_current == null) _current = new Logger();

                return _current;
            }
        }

        public long Maximum { get; set; }

        public long Minimum { get; set; }

        public void Log(Enum category, object value)
        {
            if (!MustLog(category))
                return;

            Trace.WriteLine(value, category.ToString());
        }

        public void Log(string category, object value)
        {
            Trace.WriteLine(value, category);
        }

        private bool MustLog(Enum level)
        {
            var n = Convert.ToInt64(level);

            return n >= Minimum && n <= Maximum;
        }
    }
}