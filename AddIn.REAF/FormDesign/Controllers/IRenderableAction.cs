using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Drawing;
using Keystone.AddIn.FormDesigner.Elements;

namespace Keystone.AddIn.FormDesigner.Controllers
{
    interface IRenderableAction
    {
        void DrawAction(Graphics g, GraphViewer graphViewer);

        int ActionRenderIndex{get;}
    }
}
