using System;

namespace SOVND.Client.Settings
{
    public class SovndListeningSettings : IListeningSettings
    {
        public string Channel
        {
            get { return "ambient"; }
            set { throw new NotImplementedException(); }
        }
    }
}