using System;
namespace BatchExecute
{
    public class ArgumentFormatterException : Exception
    {
        internal ArgumentFormatterException(string message)
            : base(message)
        {
            
        }

        internal ArgumentFormatterException(string message, Exception innerException)
            : base(message, innerException)
        {
            
        }
    }
}
