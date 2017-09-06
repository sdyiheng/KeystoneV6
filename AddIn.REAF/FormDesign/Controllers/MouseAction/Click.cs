using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;
using System.Windows.Forms;
using Keystone.Common.Utility;
using System.Drawing;

namespace Keystone.AddIn.FormDesigner.Controllers.MouseAction
{
     class OnceClick
    {
        //public const int ShortClickThresholdInMS = 500;
        public const int LongClickThresholdInMS = 600;

        public readonly MouseButtons MouseButton;
        public readonly Point MouseDown = Point.Empty;
        public readonly long MouseDownTicket = 0;
        public readonly Keys KeyModifier = Keys.None;

        private Point mouseUp = Point.Empty;
        private long mouseUpTicket = 0;

        public Point MouseUp
        {
            get
            {
                return this.mouseUp;
            }
        }
        public long MouseUpTicket
        {
            get
            {
                return this.mouseUpTicket;
            }
        }

        public OnceClick(Point mouseDown, long mouseDownTicket, MouseButtons mouseButton, Keys key)
        {
            this.MouseDown = mouseDown;
            this.MouseDownTicket = mouseDownTicket;
            this.MouseButton = mouseButton;
            this.KeyModifier = key;
        }


        public void CommitClick(Point mouseUp, long mouseUpTicket)
        {
            this.mouseUp = mouseUp;
            this.mouseUpTicket = mouseUpTicket;
            Debug.Assert(mouseUpTicket > MouseDownTicket);
#if DEBUG
            Console.WriteLine("ComitClick:" + this.getTimeSpanInMS());
#endif
        }

        public bool IsValidClick
        {
            get
            {
                if (mouseUpTicket == 0)
                    return false;
                if (mouseUp == Point.Empty)
                    return false;

                return !GeoHelper.IsMouseMoved(this.MouseDown, this.mouseUp);
            }
        }
        public bool IsValidClick_Short
        {
            get
            {
                if (!IsValidClick)
                    return false;

                return getTimeSpanInMS() < LongClickThresholdInMS;
            }
        }

        private double getTimeSpanInMS()
        {
            Debug.Assert(mouseUpTicket > MouseDownTicket);
            return (mouseUpTicket - MouseDownTicket) * 1000.0 / (double)TimeTracker.Freq;
        }

        public bool IsControlKey
        {
            get
            {
                return (KeyModifier & Keys.Control) == Keys.Control;
            }
        }
        public bool IsShiftKey
        {
            get
            {
                return (KeyModifier & Keys.Shift) == Keys.Shift;
            }
        }
        public bool IsAltKey
        {
            get
            {
                return (KeyModifier & Keys.Alt) == Keys.Alt;
            }
        }

    }

    class ClickMsgSendTask
    {
        public readonly object Msg;
        public bool Invalid;

        public ClickMsgSendTask(object msg)
        {
            this.Msg = msg;
        }
    }

}
