using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AutoStreamDeck.Actions
{
    [AttributeUsage(AttributeTargets.Class)]
    public class ActionMetaAttribute : Attribute
    {

        public string ActionName { get; }

#if NET8_0
        public string? Description { get; set; }
#else
        public string Description { get; set; }
#endif

        public ActionMetaAttribute(string actionName)
        {
            ActionName = actionName;
        }
    }
}
