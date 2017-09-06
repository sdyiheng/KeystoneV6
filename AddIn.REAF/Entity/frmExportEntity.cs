using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using Keystone.Graph.Controls.View;
using MessageEngine;
using Keystone.Common.Messages;
using System.IO;
using System.Diagnostics;
using Keystone.Common.Utility;

namespace Keystone.Graph.Controls
{
    public partial class frmExportEntity : Form
    {
        public frmExportEntity()
        {
            InitializeComponent();
        }

        readonly Dictionary<byte, int> imgIndex = new Dictionary<byte, int>();
        private void btnCancel_Click(object sender, EventArgs e)
        {
            this.Close();
        }

        //public TreeView Tree
        //{
        //    get
        //    {
        //        return this.treeView1;
        //    }
        //}

        MapNodeDescription bindedRoot = null;
        public MapNodeDescription BindedRoot
        {
            get
            {
                return bindedRoot;
            }
        }
        private void btnOK_Click(object sender, EventArgs e)
        {
            if (this.bindedRoot == null)
                return;

            if (!this.bindedRoot.HasSelected)
            {
                SuperMCMService.PostMessage(new MessageBoxMsg("错误", "未选中任何项", MessageBoxIcon.Error));
                return;
            }
            if (string.IsNullOrEmpty(this.textBox1.Text) || !Directory.Exists(this.textBox1.Text))
            {
                SuperMCMService.PostMessage(new MessageBoxMsg("错误", "设置导出路径", MessageBoxIcon.Error));
                return;
            }

            if (this.chkOverwrite.Checked && !MessageAlertHelper.ShowConfirmMsg("您确定要覆盖已有文件吗？"))
            {
                this.chkOverwrite.Checked = false;
                return;
            }
            this.DialogResult = System.Windows.Forms.DialogResult.OK;
            this.Close();
        }


        protected override void OnClosed(EventArgs e)
        {
            base.OnClosed(e);
            this.imageList1.Images.Clear();
        }

        private void btnSelectAll_Click(object sender, EventArgs e)
        {
            this.doSelectAll(this.treeView1.Nodes);
        }
        private void doSelectAll(TreeNodeCollection nodes)
        {
            foreach (TreeNode node in nodes)
            {
                node.Checked = true;
                if (node.Nodes.Count > 0)
                    this.doSelectAll(node.Nodes);
            }
        }

        public void BindEntity(MapNodeDescription root)
        {
            bindedRoot = new MapNodeDescription();

            this.treeView1.SuspendLayout();
            if (root.Nodes != null && root.Nodes.Count > 0)
            {
                var sortedList = from n in root.Nodes orderby n.NodeTitle  select n;
                foreach (MapNodeDescription subNode in sortedList)//root.Nodes.ToArray()
                    this.doBindEntity(this.treeView1.Nodes, subNode, bindedRoot);
            }
            this.treeView1.ResumeLayout();
            this.treeView1.ExpandAll();

#if DEBUG
            this.validate(this.treeView1.Nodes, bindedRoot );
#endif
        }

 #if DEBUG
        private void validate(TreeNodeCollection nodes, MapNodeDescription parent)
        {
            Debug.Assert(nodes.Count == 0 || nodes.Count == parent.Nodes.Count);
            foreach (TreeNode n in nodes)
            {
                Debug.Assert(parent.Nodes.Contains(n.Tag as MapNodeDescription));
                this.validate(n.Nodes, n.Tag as MapNodeDescription);
            }
        }
#endif

        private void doBindEntity(TreeNodeCollection nodes, MapNodeDescription node, MapNodeDescription bindedParent)
        {
            if (node.NodeType == 8 )
            {
                if (node.Nodes != null && node.Nodes.Count > 0)
                {
                    MapNodeDescription _node = node.CloneWithoutSubNodes();
                    TreeNode treeNode = new TreeNode("模块：" + _node.NodeTitle);
                    treeNode.Tag = _node;
                    if (imgIndex.ContainsKey(_node.NodeType))
                    {
                        treeNode.ImageIndex = imgIndex[_node.NodeType];
                        treeNode.SelectedImageIndex = imgIndex[_node.NodeType];
                    }
                    else if (SmallIconProvider.Instance.HasSmallIcon(_node.NodeType))
                    {
                        Image img = SmallIconProvider.Instance.GetSmallIcon(_node.NodeType);
                        this.imageList1.Images.Add(img);
                        this.imgIndex.Add(_node.NodeType, this.imageList1.Images.Count - 1);
                        treeNode.ImageIndex = imgIndex[_node.NodeType];
                        treeNode.SelectedImageIndex = imgIndex[_node.NodeType];
                    }
                    nodes.Add(treeNode);
                    if (bindedParent.Nodes == null)
                        bindedParent.Nodes = new List<MapNodeDescription>();
                    bindedParent.Nodes.Add(_node);

                    var sortedList = from n in node.Nodes orderby n.NodeTitle select n;
                    foreach (MapNodeDescription subNode in sortedList)
                        this.doBindEntity(treeNode.Nodes, subNode, _node);

                    if (treeNode.Nodes.Count == 0)
                    {
                        nodes.Remove(treeNode);
                        bindedParent.Nodes.Remove(_node);
                    }
                }
            }
            else if (node.NodeType == 9)
            {
                MapNodeDescription _node = node.CloneWithoutSubNodes();
                TreeNode treeNode = new TreeNode(_node.NodeTitle);
                treeNode.Tag = _node;
                if (imgIndex.ContainsKey(_node.NodeType))
                {
                    treeNode.ImageIndex = imgIndex[_node.NodeType];
                    treeNode.SelectedImageIndex = imgIndex[_node.NodeType];
                }
                else if (SmallIconProvider.Instance.HasSmallIcon(_node.NodeType))
                {
                    Image img = SmallIconProvider.Instance.GetSmallIcon(_node.NodeType);
                    this.imageList1.Images.Add(img);
                    this.imgIndex.Add(_node.NodeType, this.imageList1.Images.Count - 1);
                    treeNode.ImageIndex = imgIndex[_node.NodeType];
                    treeNode.SelectedImageIndex = imgIndex[_node.NodeType];
                }
                if (bindedParent.Nodes == null)
                    bindedParent.Nodes = new List<MapNodeDescription>();
                bindedParent.Nodes.Add(_node);
                nodes.Add(treeNode);

                if (node.Nodes != null && node.Nodes.Count > 0)
                {
                    var sortedList = from n in node.Nodes orderby n.NodeTitle select n;
                    foreach (MapNodeDescription subNode in sortedList)//node.Nodes.ToArray()
                    {
                        this.doBindEntity(nodes, subNode, bindedParent);
                    }
                }
            }
            else if( node.Nodes != null && node.Nodes.Count > 0)
            {
                var sortedList = from n in node.Nodes orderby n.NodeTitle select n;
                foreach (MapNodeDescription subNode in sortedList)
                    this.doBindEntity(nodes, subNode, bindedParent);
            }
        }

        private void treeView1_AfterCheck(object sender, TreeViewEventArgs e)
        {
            if (e.Node.Nodes.Count > 0)
            {
                (e.Node.Tag as MapNodeDescription).Selected = e.Node.Checked;
                if ((Control.ModifierKeys & Keys.Shift) != Keys.Shift)
                    return;
                foreach (TreeNode n in e.Node.Nodes)
                {
                    n.Checked = e.Node.Checked;
                    //(n.Tag as MapNodeDescription).Selected = n.Checked;
                }
            }
            else
            {
                (e.Node.Tag as MapNodeDescription).Selected = e.Node.Checked;
            }
        }

        private void btnPath_Click(object sender, EventArgs e)
        {
            FolderBrowserDialog dlg = new FolderBrowserDialog();
            dlg.ShowNewFolderButton = true;
            dlg.Description = "选定导出路径。";
            if (!string.IsNullOrEmpty(this.textBox1.Text) && Directory.Exists(this.textBox1.Text))
                dlg.SelectedPath = this.textBox1.Text;
            if (dlg.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                this.textBox1.Text = dlg.SelectedPath;
        }

        public string SelectedPath
        {
            get
            {
                return this.textBox1.Text;
            }
            set
            {
                this.textBox1.Text = value;
            }
        }

        public bool OverWrite
        {
            get
            {
                return this.chkOverwrite.Checked;
            }
            set
            {
                this.chkOverwrite.Checked = value;
            }
        }
    }
}
