using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MessageEngine.SuperMCMCore;
using Keystone.AddIn.FormDesigner.Messages;
using System.Drawing;
using Keystone.Common.Utility;
using System.Drawing.Drawing2D;
using Keystone.AddIn.FormDesigner.Elements;

namespace Keystone.AddIn.FormDesigner.Controllers
{
    abstract  class baseFormController : BaseMessageListener
    {
        protected ViewDefinition form = null;

        [MessageSubscriber]
        protected virtual void on(CurrentViewDefinitionMsg msg)
        {
            this.form = msg.ViewDefinition;
        }

    }

     class baseFixGridFormController : baseFormController
     {
         private static Pen assistPen = new Pen(Color.Red, 1f);

         static baseFixGridFormController()
         {
             assistPen.DashStyle = DashStyle.Custom;
             assistPen.DashPattern = new float[] { 4, 2 };
         }

         protected int[] xs;
         protected int[] ys;

         protected void buildAssistLine(IViewElement e, IViewElement[] skipElements)
         {
             if (e == null)
             {
                 xs = null;
                 ys = null;
                 return;
             }

             IViewElement[] es = e.GetAllElements();
             if (es == null)
             {
                 xs = null;
                 ys = null;
                 return;
             }

             List<int> xlist = new List<int>();
             List<int> ylist = new List<int>();

             Rectangle rect = Rectangle.Empty;
             foreach (IViewElement element in es)
             {
                 if (skipElements != null && Array.IndexOf(skipElements, element) != -1)
                     continue;

                 rect = element.GetAbsRectangle();
                 if (rect == Rectangle.Empty)
                     continue;

                 if (!xlist.Contains(rect.Left))
                     xlist.Add(rect.Left);
                 if (!xlist.Contains(rect.Right))
                     xlist.Add(rect.Right);
                 if (!ylist.Contains(rect.Top))
                     ylist.Add(rect.Top);
                 if (!ylist.Contains(rect.Bottom))
                     ylist.Add(rect.Bottom);
             }

             xs = xlist.ToArray();
             ys = ylist.ToArray();

             Array.Sort(xs);
             Array.Sort(ys);
         }
         protected void clearAssistLine()
         {
             xs = null;
             ys = null;
         }

        [Flags]
         public enum AssistLineMode
         {
             None,
             Left = 1,
             Top = 2,
             Right = 4,
             Bottom = 8,
             All=15,
         }

         protected virtual void drawAssistLine(Graphics g, Rectangle rect,AssistLineMode mode)
         {
             if ((mode & AssistLineMode.Top) == AssistLineMode.Top)
                 drawAssistLine_y(g, rect.Top );

             if ((mode & AssistLineMode.Bottom) == AssistLineMode.Bottom)
                 drawAssistLine_y(g, rect.Bottom);

             if ((mode & AssistLineMode.Left) == AssistLineMode.Left)
                 this.drawAssistLine_x(g, rect.Left);

             if ((mode & AssistLineMode.Right) == AssistLineMode.Right)
                 this.drawAssistLine_x(g, rect.Right);
         }
         protected virtual void drawAssistLine_x(Graphics g, int x)
         {
             if( xs != null&& xs.Contains(x ))
                 g.DrawLine(assistPen, new Point(x, 0), new Point(x, this.form.Height));
         }
         protected virtual void drawAssistLine_y(Graphics g, int y)
         {
             if (ys != null && ys.Contains(y))
                 g.DrawLine(assistPen, new Point(0, y), new Point(this.form.Width, y));
         }


         protected int fixValue_X(int v)
         {
             return Math.Max(0, Math.Min(this.form.Width, BaseViewElement.FixGridValue(v)));
         }

         protected int fixValue(int v)
         {
             return BaseViewElement.FixGridValue(v);
         }

         protected int fixValue_Y(int v)
         {
             return Math.Max(0, Math.Min(this.form.Height, BaseViewElement.FixGridValue(v)));
         }

         protected Point fixValue(Point p)
         {
             return new Point(this.fixValue_X(p.X), this.fixValue_Y(p.Y));
         }

         protected Rectangle fixPointToRect(Point p)
         {
             return GeoHelper.GetOutterRectangle(BaseViewElement.FixGridValue_Down(p), BaseViewElement.FixGridValue_Up(p));
         }
         protected Rectangle fixValue(Rectangle rect)
         {
             return GeoHelper.GetOutterRectangle(
                 this.fixValue(rect.Location), 
                 this.fixValue(new Point(rect.Right, rect.Bottom))
                 );
         }
     }
}
