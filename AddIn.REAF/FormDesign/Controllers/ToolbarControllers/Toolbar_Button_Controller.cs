﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MessageEngine.SuperMCMCore;
using MessageEngine;
using Keystone.AddIn.FormDesigner.Messages;
using Keystone.AddIn.FormDesigner.Elements;

namespace Keystone.AddIn.FormDesigner.Controllers.ToolbarControllers
{
    class Toolbar_Button_Controller : mouseActionToolbarActionController
    {
        public Toolbar_Button_Controller(System.Windows.Forms.ToolStripButton _btn)
            : base(_btn)
        {
        }

        protected override void doMyClickAction(Keystone.AddIn.FormDesigner.Messages.ToolbarCommandClickedMsg msg)
        {
            if (this.button.Checked)
            {
                bool multiTimes = msg.ClickType == MouseClickType.DoubleClick;
                if (this.element != null && this.element is ButtonElement && !multiTimes)
                    return;


                SuperMCMService.PostMessage(new InsertElementTypeMsg(new ButtonElement(), msg.ClickType == MouseClickType.DoubleClick), ViewDesignerMainController.MESSAGECHANNEL);
            }
        }

        [MessageSubscriber(MessageEngine.SuperMCMCore.MMMODE.PASIUI)]
        private void on(InsertElementTypeMsg msg)
        {
            this.element = msg.Element;
            this.button.Checked = msg.Element is ButtonElement;
        }
    }
}
