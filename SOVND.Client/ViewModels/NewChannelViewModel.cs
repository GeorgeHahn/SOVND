using SOVND.Client.Modules;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SOVND.Client.Models;

namespace SOVND.Client.ViewModels
{
    public class NewChannelViewModel
    {
        private NewChannelModel _model;
        private readonly SovndClient _client;

        public NewChannelViewModel(SovndClient client)
            : this(new NewChannelModel(), client)
        { }

        public NewChannelViewModel(NewChannelModel model, SovndClient client)
        {
            _model = model;
            _client = client;
        }

        // TODO Don't technically need INPC on these properties because this will probably never be used in a 2 way binding, but probably good to add them regardless
        public string Name
        {
            get { return _model.Name; }
            set { _model.Name = value; }
        }

        public string Description
        {
            get { return _model.Description; }
            set { _model.Description = value; }
        }

        public string Image
        {
            get { return ""; }
        }

        public bool Register()
        {
            return _client.RegisterChannel(Name, Description, Image);
        }
    }
}
