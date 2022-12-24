using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;

namespace NetTally.Output
{
    public class TextResultsProviderFactory
    {
        private readonly IServiceProvider serviceProvider;

        public TextResultsProviderFactory(IServiceProvider serviceProvider)
        {
            this.serviceProvider = serviceProvider;
        }

        public ITextResultsProvider Create(Quest quest)
        {
            return ActivatorUtilities.CreateInstance<ITextResultsProvider>(serviceProvider, quest);
        }
    }
}
