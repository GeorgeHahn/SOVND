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

        public string Name
        {
            get { return model.Name; }
        }

        public string Description
        {
            get { return model.Description; }
        }

        public string Image
        {
            get { return model.Image; }
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
        public string Image { get; set; }
        //public string Moderators { get; set; }
    }
}
