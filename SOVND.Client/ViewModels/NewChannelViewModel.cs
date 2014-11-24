using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SOVND.Client.ViewModels
{
    public class NewChannelViewModel
    {
        private NewChannelModel model = new NewChannelModel();

        // TODO Don't technically need INPC on these properties because this will probably never be used in a 2 way binding, but probably good to add them regardless
        public string Name
        {
            get { return model.Name; }
            set { model.Name = value; }
        }

        public string Description
        {
            get { return model.Description; }
            set { model.Description = value; }
        }

        public string Image
        {
            get { return ""; }
        }

        public bool Register()
        {
            return App.client.RegisterChannel(Name, Description, Image);
        }
    }

    public class NewChannelModel
    {
        public string Name { get; set; }
        public string Description { get; set; }
        //public string Image { get; set; }
        //public string Moderators { get; set; }
    }
}
