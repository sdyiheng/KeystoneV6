using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;
using System.Windows.Forms;
using MessageEngine;
using Keystone.AddIn.FormDesigner.Messages;
using MessageEngine.SuperMCMCore;
using System.Drawing;
using Keystone.AddIn.FormDesigner.Elements;
using Keystone.Common.Utility;

namespace Keystone.AddIn.FormDesigner.Controllers.CustomContextMenuItem
{
    class FormViewExpansionController : baseCustomContextMenuItemController
    {
        protected override void doReg()
        {
            Debug.Assert(menuItem == null);

            menuItem = new ToolStripMenuItem();
            menuItem.Text = "扩展设计视图";
            menuItem.Click += new EventHandler(menuItem_Click);
            menuItem.Visible = false;
            menuItem.Enabled = false;
            reged = true;

            SuperMCMService.PostMessage(new RegisterContextMenuItemMsg(menuItem,999), ViewDesignerMainController.MESSAGECHANNEL);
        }

        void menuItem_Click(object sender, EventArgs e)
        {
            if( this.form == null )
                return;

            if (!this.form.HasChildren)
                return;

            foreach (IViewElement element in this.form.Children)
            {
                element.X += 24; ;
                element.Y += 24;
            }

            this.form.Width += 48;
            this.form.Height += 48;

            SuperMCMService.PostMessage(new resizeFormDesignerMsg(), ViewDesignerMainController.MESSAGECHANNEL);
            SuperMCMService.PostMessage(new ViewPaintRequestMsg(), ViewDesignerMainController.MESSAGECHANNEL);
        }

        [MessageSubscriber(MessageEngine.SuperMCMCore.MMMODE.PASIUI)]
        protected override void onContextMenuActionElementsMsg(ContextMenuActionElementsMsg msg)
        {
            base.onContextMenuActionElementsMsg(msg);
            if (this.menuItem != null)
            {
                this.menuItem.Enabled = contextElementMsg != null && contextElementMsg.FocusOnElement != null;//is ViewDefinition;
                this.menuItem.Visible = this.menuItem.Enabled;
            }
        }

    }
}
