using System.ComponentModel;

namespace GitPull
{
    internal class GeneralOptions : BaseOptionModel<GeneralOptions>
    {
        [DisplayName("Pull on Solution Open")]
        [Description("Automatically calls \"git pull\" when a solution is opened")]
        [DefaultValue(false)]
        public bool PullOnSolutionOpen { get; set; }
    }
}
