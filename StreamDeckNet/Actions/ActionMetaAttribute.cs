using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace StreamDeckNet.Actions
{
    [AttributeUsage(AttributeTargets.Class)]
    public class ActionMetaAttribute : Attribute
    {

        public string ActionName { get; }

        public string? Description { get; set; }

        public ActionMetaAttribute(string actionName)
        {
            ActionName = actionName;
        }
    }
}
