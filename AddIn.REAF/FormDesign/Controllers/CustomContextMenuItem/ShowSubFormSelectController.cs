using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MessageEngine.SuperMCMCore;
using System.Windows.Forms;
using MessageEngine;
using Keystone.AddIn.FormDesigner.Messages;
using Keystone.AddIn.FormDesigner.Elements;
using Keystone.Common.Utility;
using System.Drawing;
using System.Diagnostics;
using Keystone.Common.Messages;
using MessageEngine.Messages;

namespace Keystone.AddIn.FormDesigner.Controllers.CustomContextMenuItem
{
    class ShowSubFormSelectController : baseCustomContextMenuItemController
    {
        protected override void doReg()
        {
            Debug.Assert(menuItem == null);

            menuItem = new ToolStripMenuItem();
            menuItem.Text = "打开子视图";
            menuItem.Click += new EventHandler(menuItem_Click);
            menuItem.Visible = false;
            menuItem.Enabled = false;
            reged = true;

            SuperMCMService.PostMessage(new RegisterContextMenuItemMsg(menuItem), ViewDesignerMainController.MESSAGECHANNEL);
        }

        void menuItem_Click(object sender, EventArgs e)
        {
            if (this.contextElementMsg != null && this.contextElementMsg != null)
            {
                SubFormElement subFormElement = this.contextElementMsg.FocusOnElement as SubFormElement;
                if (subFormElement != null && subFormElement.SubFormID != Guid.Empty)
                {
                    if( MessageAlertHelper.ShowConfirmMsg("需要保存当前设计视图的内容吗？"))
                        SuperMCMService.PostMessage(new RequestSaveFormViewMsg(), ViewDesignerMainController.MESSAGECHANNEL);

                    SuperMCMService.PostMessage(new RequestShowFormViewMsg(subFormElement.SubFormID));
                    return;
                }
            }
        }

        [MessageSubscriber(MessageEngine.SuperMCMCore.MMMODE.PASIUI)]
        protected override void onContextMenuActionElementsMsg(ContextMenuActionElementsMsg msg)
        {
            base.onContextMenuActionElementsMsg(msg);
            if (this.menuItem != null)
            {
                this.menuItem.Enabled = contextElementMsg != null && contextElementMsg.FocusOnElement is SubFormElement &&
                    (contextElementMsg.FocusOnElement as SubFormElement ).SubFormID != Guid.Empty ;
                this.menuItem.Visible = this.menuItem.Enabled;
            }
        }

        

    }
}
