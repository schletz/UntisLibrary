using System;
using System.Collections.Generic;
using System.Text;

namespace UntisLibrary.Api
{
    public class UntisException : Exception
    {
        public int ErrorCode { get; set; }
        public string Method { get; set; }

        public UntisException() : base() { }
        public UntisException(string message) : base(message) { }
        public UntisException(string message, Exception innerException) : base(message, innerException) { }
    }
}
