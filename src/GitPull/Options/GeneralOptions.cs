using System.ComponentModel;

namespace GitPull
{
    internal class Options : BaseOptionModel<Options>
    {
        public class Page : BaseOptionPage<Options> { }

        [DisplayName("Pull on Solution Open")]
        [Description("Automatically calls \"git pull\" when a solution is opened")]
        [DefaultValue(false)]
        public bool PullOnSolutionOpen { get; set; }
    }
}
