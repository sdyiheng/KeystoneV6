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
    class FormViewContractionController : baseCustomContextMenuItemController
    {
        protected override void doReg()
        {
            Debug.Assert(menuItem == null);

            menuItem = new ToolStripMenuItem();
            menuItem.Text = "收缩设计视图";
            menuItem.Click += new EventHandler(menuItem_Click);
            menuItem.Visible = false;
            menuItem.Enabled = false;
            reged = true;

            SuperMCMService.PostMessage(new RegisterContextMenuItemMsg(menuItem,1000), ViewDesignerMainController.MESSAGECHANNEL);
        }

        void menuItem_Click(object sender, EventArgs e)
        {
            if( this.form == null )
                return;

            if (!this.form.HasChildren)
                return;

            List<Rectangle> list = new List<Rectangle>();
            foreach (IViewElement element in this.form.Children)
            {
                list.Add(element.GetAbsRectangle());
            }

            Rectangle outterRect = GeoHelper.GetOutterRectangle(list.ToArray());
            Rectangle formRect = this.form.GetAbsRectangle();
            if (outterRect == formRect)
                return;

            if (formRect.Width == 36 && formRect.Height == 36)
                return;

            foreach (IViewElement element in this.form.Children)
            {
                element.X -= outterRect.X;
                element.Y -= outterRect.Y;
            }

            this.form.Width = outterRect.Width;
            this.form.Height = outterRect.Height;

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
