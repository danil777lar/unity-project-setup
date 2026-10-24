namespace Larje.ProjectSetup.Editor
{
    public sealed class ProjectSetupContext
    {
        public const string ROOT = "Assets/_Project";
        public const string CONFIGS = ROOT + "/Configs";
        public const string IDS = CONFIGS + "/Ids";
        public const string INSTALLERS = CONFIGS + "/Installers";
        public const string SETTINGS = ROOT + "/Settings";
        public const string SCENES = ROOT + "/Scenes";
        public const string UI_PREFABS = ROOT + "/Prefabs/UI";
        public const string SCRIPTS = ROOT + "/Scripts";
        public const string RESOURCES = ROOT + "/Resources";

        public const string SPLASH_SCENE = "Splash";
        public const string MAIN_SCENE = "Main";

        public ProjectSetupOptions Options { get; }
        public ProjectSetupReport Report { get; }

        public ProjectSetupContext(ProjectSetupOptions options)
        {
            Options = options;
            Report = new ProjectSetupReport();
        }
    }
}
