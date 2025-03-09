using System.Runtime.CompilerServices;

namespace Backdash.Tests.TestUtils
{
    /// <summary>
    /// Attribute that is applied to a method to indicate that it is a fact that should be run
    /// by the test runner only when dynamic code is supported.
    /// </summary>
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
    class DynamicFactAttribute : FactAttribute
    {
        public DynamicFactAttribute(string? message = null)
        {
            if (!RuntimeFeature.IsDynamicCodeSupported)
            {
                Skip = "Skipped due to no dynamic code support";
                if (message != null)
                    Skip += ": " + message;
            }
        }
    }
}
