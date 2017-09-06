using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Keystone.AddIn.Entity
{
     class CopyEntityNodeCodeMsg
    {
        public readonly EntityNode Node;

        public CopyEntityNodeCodeMsg(EntityNode node)
        {
            this.Node = node;
        }
    }
}
