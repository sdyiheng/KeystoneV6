using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MessageEngine.SuperMCMCore;
using Keystone.Common.Messages;
using System.Drawing;
using Keystone.Common.Utility;
using System.Diagnostics;
using System.Windows.Forms;
using MessageEngine;
using System.Threading;
using Keystone.AddIn.FormDesigner.Messages;

namespace Keystone.AddIn.FormDesigner.Controllers.MouseAction
{
    class MapMouseClickController : BaseMessageListener
    {
        Guid mainFormID = Guid.Empty;
        [MessageSubscriber]
        private void OnMainFormLoadedMsg(MainFormLoadedMsg msg)
        {
            mainFormID = msg.MainFormID;
        }

        private MessageEngine.SuperMCMCore.Timer msgTimer = null;
        public MapMouseClickController()
        {
            msgTimer = new MessageEngine.SuperMCMCore.Timer();
            msgTimer.Mode = TimerMode.Periodic;
            msgTimer.Period = 5;//
            msgTimer.Resolution = 1;
            msgTimer.Tick += new EventHandler(msgTimer_Tick);
            msgTimer.Start();
        }

        private long timeTicket = 0;
        public long TimeTicket
        {
            get
            {
                return timeTicket;
            }
        }
        void msgTimer_Tick(object sender, EventArgs e)
        {
            long currentTicket = TimeTracker.GetCurrentTicket();
            
            try
            {
                if (click != null && this.clicks.Count == 0 )
                {
                    double span = (currentTicket - click.MouseDownTicket) * 1000.0 / (double)TimeTracker.Freq;
                    if (span > OnceClick.LongClickThresholdInMS && span < OnceClick.LongClickThresholdInMS + 10)
                    {
                        this.timeTicket = DateTime.Now.Ticks;
                    }
                }

                if (msgList.Count == 0)
                    return;
            }
            catch (Exception ex)
            {
                Keystone.Common.Utility.LogHelper.Error(ex);
            }

            bool enter = Monitor.TryEnter(msgList);
            try
            {
                if (enter)
                {
                    while (msgList.Count > 0)
                    {
                        currentTicket = TimeTracker.GetCurrentTicket();

                        ClickMsgSendTask task = msgList[0];
                        MouseClickMsg msg = msgList[0].Msg as MouseClickMsg;
                        Debug.Assert(msg != null);
                        if (task.Invalid)
                        {
                            //无效的消息
                            msgList.RemoveAt(0);
                            continue;
                        }

                        double timeSpanInMS = (currentTicket - msg.TimeTicket) * 1000.0 / (double)TimeTracker.Freq;
                        if (timeSpanInMS > SystemInformation.DoubleClickTime * 2 / 3)
                        {
                            msgList.RemoveAt(0);
                            //超过等待时间
                            SuperMCMService.PostMesage(msg);
#if DEBUG
                            Console.WriteLine("Post A ClickMsg:" + msg.ClickType.ToString());
#endif
                        }
                        else
                        {
                            return;
                        }
                    }
                }
            }
            finally 
            {
                if( enter )
                    Monitor.Exit(msgList);

            }
        }

        //等待发送的消息
        readonly List<ClickMsgSendTask> msgList = new List<ClickMsgSendTask>();
        List<OnceClick> clicks = new List<OnceClick>(3);
        private OnceClick click = null;

        [MessageSubscriber(MessageEngine.SuperMCMCore.UIThreadSynchronizationMode.ASynchronizationInSequence)]
        public void OnMouseDownMoveUpMsg(MouseDownMoveUpMsg msg)
        {
            lock (msgList)
            {
                if (msg.FormID != mainFormID)
                    return;

                if (click == null)
                {
                    if (msg.Type == MouseDownMoveUpMsg.MsgType.MouseDown)//只关注鼠标按下事件
                    {
                        click = new OnceClick(msg.MousePosInControl, msg.TimeTicket, msg.MouseButton, msg.KeyModifier);
                        if (clicks.Count > 0)
                        {
                            //之前存在
                            //判断与上次点击的时间间隔
                            Debug.Assert(clicks.Count < 3);
                            double spanInMS = (msg.TimeTicket - clicks[clicks.Count - 1].MouseUpTicket) * 1000 / TimeTracker.Freq;
                            if (spanInMS < SystemInformation.DoubleClickTime * 2 / 3)
                            {
                                //未超过时间界限
                                //取消等待发送的消息
                                this.cancelAllTasks();
                            }
                            else
                            {
                                clicks.Clear();
                            }
                        }
                    }
                }
                else
                {
                    //正在等待一次Click完成
                    if (msg.Type == MouseDownMoveUpMsg.MsgType.MouseMove)
                    {
                        if (GeoHelper.IsMouseMoved(msg.MousePosInControl, click.MouseDown))
                        {
                            click = null;
                            clicks.Clear();
                            this.timeTicket = 0;
                        }
                    }
                    else if (msg.Type == MouseDownMoveUpMsg.MsgType.MouseUp)
                    {
                        if (GeoHelper.IsMouseMoved(msg.MousePosInControl, click.MouseDown))
                        {
                            click = null;
                            clicks.Clear();
                        }
                        else
                        {
                            click.CommitClick(msg.MousePosInControl, msg.TimeTicket);
                            //完成一次点击
                            clicks.Add(click);
                            if (clicks.Count == 3)
                            {
                                this.cancelAllTasks();
                                msgList.Add(new ClickMsgSendTask(new MouseClickMsg(click.MouseDown, click.MouseButton.ToString(), MouseClickType.TriClick, click.MouseDownTicket, this.mainFormID, click.IsControlKey, click.IsShiftKey)));
                                clicks.Clear();
                            }
                            else if (clicks.Count == 2)
                            {
                                this.cancelAllTasks();
                                msgList.Add(new ClickMsgSendTask(new MouseClickMsg(click.MouseDown, click.MouseButton.ToString(), MouseClickType.DoubleClick, click.MouseDownTicket, this.mainFormID, click.IsControlKey, click.IsShiftKey)));
                            }
                            else
                            {
                                msgList.Add(new ClickMsgSendTask(new MouseClickMsg(click.MouseDown, click.MouseButton.ToString(), MouseClickType.ShortClick, click.MouseDownTicket, this.mainFormID, click.IsControlKey, click.IsShiftKey)));
                            } 
                            click = null;
                        }
                        this.timeTicket = 0;
                    }
                }
            }
        }

        private void cancelAllTasks()
        {
            lock (msgList)
            {
                foreach (ClickMsgSendTask task in msgList.ToArray())
                {
                    task.Invalid = true;
                }
            }
        }


        public bool MouseCaptured
        {
            get { return true; }
        }
    }



}
