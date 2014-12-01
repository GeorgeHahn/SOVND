namespace SOVND.Lib.Models
{
    public class ChatMessage
    {
        public string Message { get; private set; }

        public ChatMessage(string message)
        {
            Message = message;
        }
    }
}