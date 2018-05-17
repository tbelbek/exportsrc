#region usings

using System;
using System.Diagnostics;
using System.Windows.Forms;

#endregion

namespace ExportSrc
{
    public class LabelTraceListener : TraceListener
    {
        private readonly Label _label;

        public LabelTraceListener(Label label)
        {
            this._label = label;
        }

        public override void Write(string message)
        {
            this._label.BeginInvoke((Action)(() => { this._label.Text = message; }));
        }

        public override void WriteLine(string message)
        {
            this._label.BeginInvoke((Action)(() => { this._label.Text = message; }));
        }
    }
}