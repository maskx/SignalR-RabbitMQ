using Microsoft.Extensions.Logging.Testing;
using System;
using System.Collections.Generic;
using System.Text;

namespace maskx.AspNetCore.SignalR.RabbitMQ.Tests
{
    public class VerifiableLoggedTest : LoggedTest
    {
        public virtual IDisposable StartVerifiableLog(Func<WriteContext, bool> expectedErrorsFilter = null)
        {
            return CreateScope(expectedErrorsFilter);
        }

        private VerifyNoErrorsScope CreateScope(Func<WriteContext, bool> expectedErrorsFilter = null)
        {
            return new VerifyNoErrorsScope(LoggerFactory, wrappedDisposable: null, expectedErrorsFilter);
        }
    }
}
