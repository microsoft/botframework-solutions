using Microsoft.Bot.Builder.Skills.Contextual.Models;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.Bot.Builder.Skills.Contextual
{
    public interface IContextResolver
    {
        Task<IList<string>> GetResolvedContactAsync(RelatedEntityInfo relatedEntityInfo);
    }
}
