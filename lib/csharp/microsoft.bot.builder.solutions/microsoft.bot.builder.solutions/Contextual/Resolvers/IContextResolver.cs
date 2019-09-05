using Microsoft.Bot.Builder.Solutions.Contextual.Models;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.Bot.Builder.Solutions.Contextual
{
    public interface IContextResolver
    {
        Task<IList<string>> GetResolvedContactAsync(RelatedEntityInfo relatedEntityInfo);
    }
}
