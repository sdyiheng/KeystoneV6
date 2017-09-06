using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;
using System.Windows.Forms;
using MessageEngine;
using Keystone.AddIn.FormDesigner.Messages;
using MessageEngine.SuperMCMCore;
using Keystone.AddIn.FormDesigner.Elements;

namespace Keystone.AddIn.FormDesigner.Controllers.CustomContextMenuItem
{
    class SelectParentController : baseCustomContextMenuItemController
    {
        protected override void doReg()
        {
            Debug.Assert(menuItem == null);

            menuItem = new ToolStripMenuItem();
            menuItem.Text = "选中元素";
            menuItem.DropDownItemClicked += new ToolStripItemClickedEventHandler(menuItem_DropDownItemClicked);
            menuItem.Visible = false;
            menuItem.Enabled = false;
            reged = true;

            SuperMCMService.PostMessage(new RegisterContextMenuItemMsg(menuItem), ViewDesignerMainController.MESSAGECHANNEL);
        }

        void menuItem_DropDownItemClicked(object sender, ToolStripItemClickedEventArgs e)
        {
            Guid eid = (Guid)e.ClickedItem.Tag;
            SuperMCMService.PostMessage(new RequestSelectElementMsg(eid), ViewDesignerMainController.MESSAGECHANNEL);
        }



        IViewElement element;
        [MessageSubscriber()]
        private void on(InsertElementTypeMsg msg)
        {
            this.element = msg.Element;
        }

        Elements.IViewElement[] selections = null;
        [MessageSubscriber]
        private void on(SelectionChangedMsg msg)
        {
            if (this.form == null)
                return;

            selections = msg.Elements;
        }
        private bool isElementSelected(IViewElement e)
        {
            return this.selections != null && Array.IndexOf(this.selections, e) != -1;
        }


        [MessageSubscriber(MessageEngine.SuperMCMCore.MMMODE.PASIUI)]
        protected override void onContextMenuActionElementsMsg(ContextMenuActionElementsMsg msg)
        {
            base.onContextMenuActionElementsMsg(msg);
            if (this.element != null)
            {
                this.menuItem.Enabled = false;
                this.menuItem.Visible = false;
            }
            else
            {
                IViewElement[] elements = this.form.HitTest(msg.PointInControl, 0);
                IViewElementContainer topMostContainer = BaseViewElement.TopMostElementContainer(elements);
                IViewElement topMostElement = BaseViewElement.TopMostElement(elements);
                if (topMostElement != null && topMostElement.ParentElement != null)//&& isElementSelected(topMostElement) 
                {
                    foreach (ToolStripMenuItem item in this.menuItem.DropDownItems)
                    {
                        item.Text = string.Empty;
                        item.Visible = false;
                        item.Tag = null;
                    }
                    this.setSelectionParent(!isElementSelected(topMostElement) ? topMostElement : topMostElement.ParentElement);
                    this.menuItem.Enabled = true;
                    this.menuItem.Visible = true;
                }
                else if (topMostContainer != null && topMostContainer != this.form && topMostElement.ParentElement != null)//&& isElementSelected(topMostContainer)
                {
                    foreach (ToolStripMenuItem item in this.menuItem.DropDownItems)
                    {
                        item.Text = string.Empty;
                        item.Visible = false;
                        item.Tag = null;
                    }
                    this.setSelectionParent(!isElementSelected(topMostContainer) ? topMostContainer : topMostContainer.ParentElement);
                    this.menuItem.Enabled = true;
                    this.menuItem.Visible = true;
                }
                else
                {
                    this.menuItem.Enabled = false;
                    this.menuItem.Visible = false;
                }
            }
        }

        private void setSelectionParent(IViewElement e)
        {
            if (e == null)
                return;

            ToolStripMenuItem target = null;
            foreach (ToolStripMenuItem item in this.menuItem.DropDownItems)
            {
                if (item.Tag == null)
                {
                    target = item;
                    break;
                }
            }

            if (target != null)
            {
                target.Text = e.GetElementTypeName();
                if (!string.IsNullOrEmpty(e.ElementName))
                    target.Text += ":" + e.ElementName;
                target.Tag = e.ElementID;
                target.Visible = true;
            }
            else
            {
                target = new ToolStripMenuItem();
                target.Text = e.GetElementTypeName();
                if (!string.IsNullOrEmpty(e.ElementName))
                    target.Text += ":" + e.ElementName;
                target.Tag = e.ElementID;
                this.menuItem.DropDownItems.Add(target);
            }

            if (e.ParentElement != null)
                this.setSelectionParent(e.ParentElement);
        }

        


    }
}
