// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace ITSMSkill.TeamsChannels.Invoke
{
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.Bot.Builder;
    using Microsoft.Bot.Schema;
    using Microsoft.Bot.Schema.Teams;

    public interface ITeamsTaskModuleHandler<T> : ITeamsFetchActivityHandler<T>, ITeamsSubmitActivityHandler<T>
    {
    }

    [FetchHandler]
    public interface ITeamsFetchActivityHandler<T>
    {
        Task<TaskModuleResponse> OnTeamsTaskModuleFetchAsync(ITurnContext turnContext, CancellationToken cancellationToken);
    }

    public interface ITeamsSubmitActivityHandler<T>
    {
        Task<T> OnTeamsTaskModuleSubmitAsync(ITurnContext context, CancellationToken cancellationToken);
    }
}
