using Semver;

namespace PALC.Updater.ViewModels;

public partial class ExistingVersionVM : ViewModelBase
{
    public required SemVersion? ReleaseVersion { get; set; }
    public required string Path { get; set; }
}
