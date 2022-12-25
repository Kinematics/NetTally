using System.Collections.Generic;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NetTally.Extensions;
using NetTally.Global;
using NetTally.Types.Enums;

namespace NetTally.ViewModels
{
    public partial class GlobalOptionsViewModel : ObservableObject
    {
        private readonly ILogger<GlobalOptionsViewModel> logger;

        public GlobalOptionsViewModel(
            IOptions<GlobalSettings> options,
            ILogger<GlobalOptionsViewModel> logger
            )
        {
            GlobalSettings = options.Value;
            this.logger = logger;
        }

        public GlobalSettings GlobalSettings { get; }
        public List<string> RankVoteCountingModes { get; } = EnumExtensions.EnumDescriptionsList<RankVoteCounterMethod>().ToList();

    }
}
