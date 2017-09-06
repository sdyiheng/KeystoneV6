using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MessageEngine.SuperMCMCore;
using System.Windows.Forms;
using Keystone.AddIn.FormDesigner.Messages;
using Keystone.AddIn.FormDesigner.Elements;
using System.ComponentModel;
using System.Collections;
using Keystone.AddIn.FormDesigner.Elements.Property;
using System.Diagnostics;
using MessageEngine;

namespace Keystone.AddIn.FormDesigner.Controllers.ToolbarControllers
{
    class Toolbar_Recycle_Controller : mouseActionToolbarActionController
    {
        private ListView listView;
        private SplitContainer splitContainner;
        public Toolbar_Recycle_Controller(System.Windows.Forms.ToolStripButton _btn, ListView _list, SplitContainer _splitContainner)
            : base(_btn)
        {
            this.listView = _list;
            this.splitContainner = _splitContainner;
            this.listView.Dock = DockStyle.Fill;
            this.button.Enabled = true;
        }


        //[MessageSubscriber(MessageEngine.SuperMCMCore.MMMODE.PASIUI)]
        //protected virtual void on(ToolbarCommandClickedMsg msg)
        //{
        //    if (this.button == msg.Button)
        //    {
        //        this.button.Checked = !this.button.Checked;// msg.Button.Equals(this.button);
        //        if (this.button.Checked)
        //            this.listView.BringToFront();
        //        this.splitContainner.Panel2Collapsed = !this.button.Checked;
        //    }
        //}

       

        [MessageSubscriber(MessageEngine.SuperMCMCore.MMMODE.PASIUI)]
        protected override void on(ToolbarCommandClickedMsg msg)
        {
            if (actionButtonItems != null && Array.IndexOf(actionButtonItems, msg.Button) != -1)
            {
                if (button.Checked && msg.Button.Equals(this.button))
                {
                    this.button.Checked = false;
                    this.splitContainner.Panel2Collapsed = true;
                }
                else
                {
                    this.button.Checked = msg.Button.Equals(this.button);
                    if (this.button.Checked)
                    {
                        this.listView.BringToFront();
                        if (this.splitContainner.Panel2Collapsed)
                            this.splitContainner.Panel2Collapsed = false;
                    }
                }
            }
        }

    }
}
