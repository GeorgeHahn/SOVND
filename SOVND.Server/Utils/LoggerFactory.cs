namespace SOVND.Server.Utils
{
    public class LoggerFactory
    {
        public static HipchatLogger GetLogger<T>()
        {
            return new HipchatLogger();
        }
    }
}