namespace Dfe.Spi.LocalPreparer.Common.Utils
{
    public static class ExceptionExtensions
    {
        public static List<string> messages { get; set; } = new();

        public static List<string> GetMessages(this Exception exception)
        {
            messages.Add(exception.Message);
            return exception.InnerException != null ? GetMessages(exception.InnerException) : messages;
        }
    }
}
