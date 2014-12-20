using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Markup;

namespace SOVND.Client.Util
{
    public class Type2Extension : System.Windows.Markup.TypeExtension
    {
        public Type2Extension()
        {
        }

        public Type2Extension(string typeName)
        {
            base.TypeName = typeName;
        }

        public override object ProvideValue(IServiceProvider serviceProvider)
        {
            IXamlTypeResolver typeResolver = (IXamlTypeResolver)serviceProvider.GetService(typeof(IXamlTypeResolver));
            int sepindex = TypeName.IndexOf('+');
            if (sepindex < 0)
                return typeResolver.Resolve(TypeName);
            else
            {
                Type outerType = typeResolver.Resolve(TypeName.Substring(0, sepindex));
                return outerType.Assembly.GetType(outerType.FullName + "+" + TypeName.Substring(sepindex + 1));
            }
        }
    }
}
