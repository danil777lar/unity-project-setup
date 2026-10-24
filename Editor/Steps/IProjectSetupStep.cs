namespace Larje.ProjectSetup.Editor.Steps
{
    public interface IProjectSetupStep
    {
        string Name { get; }

        void Run(ProjectSetupContext context);
    }
}
