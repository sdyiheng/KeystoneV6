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
    class SubFormSelectController : baseCustomContextMenuItemController
    {
        protected override void doReg()
        {
            Debug.Assert(menuItem == null);

            menuItem = new ToolStripMenuItem();
            menuItem.Text = "选择子视图";
            menuItem.Click += new EventHandler(menuItem_Click);
            menuItem.Visible = false;
            menuItem.Enabled = false;
            reged = true;

            SuperMCMService.PostMessage(new RegisterContextMenuItemMsg(menuItem), ViewDesignerMainController.MESSAGECHANNEL);
        }

        FormViewInvokeSourceMsg formsMsg = null;
        [MessageSubscriber]
        private void on(FormViewInvokeSourceMsg _msg)
        {
            formsMsg = _msg;
        }

        void menuItem_Click(object sender, EventArgs e)
        {
            if (this.contextElementMsg != null && this.contextElementMsg != null)
            {
                SubFormElement subFormElement = this.contextElementMsg.FocusOnElement as SubFormElement;
                if (subFormElement != null && formsMsg != null)
                {
                    RequestASelectionInputMsg selectMsg = new RequestASelectionInputMsg(formsMsg.FormIDs, formsMsg.FormNames, false, new object[] { subFormElement.SubFormID }, "");
                    SuperMCMService.PostMessage( new QueueMsg( new object[]{
                        selectMsg,
                        new AsyncInvokeMessage<ObjectPackageMsg>((ObjectPackageMsg msg)=>{
                            object selectedObj = (msg[0] as RequestASelectionInputMsg).FirstSelected;
                            if( selectedObj != null )
                            {
                                (msg[1] as SubFormElement).SubFormID = (Guid)selectedObj;
                                (msg[1] as SubFormElement).SubFormName = (msg[2] as FormViewInvokeSourceMsg).GetFormName((Guid)selectedObj);
                                //if ((msg[1] as SubFormElement).SubFormName == (msg[1] as SubFormElement).SubFormID.ToString())
                                //    (msg[1] as SubFormElement).SubFormName = string.Empty;
                            }
                        }, new ObjectPackageMsg( selectMsg , subFormElement,formsMsg )),
                        new RefreshPropertyValueMsg()
                    }));
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
                    formsMsg != null && formsMsg.FormIDs != null && formsMsg.FormIDs.Length > 0;
                this.menuItem.Visible = this.menuItem.Enabled;
            }
        }

        

    }
}
