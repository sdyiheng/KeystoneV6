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
using System.Drawing;
using Keystone.Graph.Core.Messages;

namespace Keystone.AddIn.FormDesigner.Controllers.ToolbarControllers
{
    class Toolbar_Outline_Controller : mouseActionToolbarActionController
    {
        private readonly ImageList treeImgList = new ImageList();
        private readonly XTreeWithCustomDoubleClick tree;
        private readonly SplitContainer splitContainner;
        private readonly Dictionary<string, int> imgIndexDic = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase );
        private readonly Dictionary<Guid, TreeNode> nodeDic = new Dictionary<Guid, TreeNode>();
        
        public Toolbar_Outline_Controller(System.Windows.Forms.ToolStripButton _btn, XTreeWithCustomDoubleClick _list, SplitContainer _splitContainner)
            : base(_btn)
        {
            this.tree = _list;
            this.splitContainner = _splitContainner;
            this.tree.Dock = DockStyle.Fill;
            this.button.Enabled = true;

            tree.NodeMouseClick += new TreeNodeMouseClickEventHandler(listView_NodeMouseClick);
            tree.HideSelection = false;

            treeImgList.Images.Add(new Bitmap(16, 16));
            this.tree.ImageList = treeImgList;
        }

        void listView_NodeMouseClick(object sender, TreeNodeMouseClickEventArgs e)
        {
            Guid elementID = (Guid)e.Node.Tag;
            SuperMCMService.PostMessage(new RequestSelectElementMsg(elementID), ViewDesignerMainController.MESSAGECHANNEL);
        }

        [MessageSubscriber(MessageEngine.SuperMCMCore.MMMODE.PASIUI)]
        protected override void on(CurrentViewDefinitionMsg msg)
        {
            base.on(msg);
            this.refreshOutline();
        }

        [MessageSubscriber(MessageEngine.SuperMCMCore.MMMODE.PASIUI)]
        private void on(RefreshOutlineMsg msg)
        {
            this.refreshOutline();
        }

        private readonly object lockee = new object();
        private void refreshOutline()
        {
            lock (lockee)
            {
                this.tree.SuspendLayout();
                this.tree.BeginUpdate();

                this.nodeDic.Clear();

                this.tree.Nodes.Clear();
                if (this.form != null)
                {
                    this.addSubNodes(this.tree.Nodes, this.form);
                }

                this.tree.EndUpdate();
                this.tree.ResumeLayout();
            }
        }
        private void addSubNodes(TreeNodeCollection nodes, IViewElementContainer container)
        {
            if (container.HasChildren)
            {
                string typeName = string.Empty;
                foreach (IViewElement e in container.Children)
                {
                    Debug.Assert(e.ParentElement == container);
                    TreeNode node = new TreeNode(e.ElementLabel);
                    node.Tag = e.ElementID;
                    nodes.Add(node);

                    int index = 0;
                    typeName = e.GetType().ToString();
                    if (imgIndexDic.ContainsKey(typeName))
                        index = imgIndexDic[typeName];
                    else
                    {
                        Image img = e.GetIconImage();
                        if (img == null)
                            imgIndexDic.Add(typeName, 0);
                        else
                        {
                            treeImgList.Images.Add(img);
                            index = treeImgList.Images.Count - 1;
                            imgIndexDic.Add(typeName, index);
                        }
                    }
                    node.ImageIndex = index;
                    node.SelectedImageIndex = index;
                    this.nodeDic.Add(e.ElementID, node);

                    if (e is IViewElementContainer)
                        this.addSubNodes(node.Nodes, e as IViewElementContainer);
                }
            }
        }

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
                        this.tree.BringToFront();
                        if (this.splitContainner.Panel2Collapsed)
                            this.splitContainner.Panel2Collapsed = false;
                    }
                }
            }
        }

        [MessageSubscriber(MessageEngine.SuperMCMCore.MMMODE.PASIUI)]
        private void on(SelectionChangedMsg msg)
        {
            if (msg.Elements == null || msg.Elements.Length == 0 || msg.Elements[0] == null)
                this.tree.SelectedNode = null;
            else
            {
                Guid eid = msg.Elements[0].ElementID;
                if (nodeDic.ContainsKey(eid))
                {
                    this.tree.SelectedNode = nodeDic[eid];
                }
            }
        }

        [MessageSubscriber(MessageEngine.SuperMCMCore.MMMODE.PASIUI)]
        private void on(ViewElementPropertyChangedMsg msg)
        {
            if (msg.PropertyName == "ElementName")
            {
                this.refreshOutline();
            }
        }
    }
}
