namespace Sqlbi.Bravo.Infrastructure.Contracts.PBICloud
{
    public enum CloudSharedModelWorkspaceType
    {
        /// <summary>
        /// ???
        /// </summary>
        Personal = 0,

        /// <summary>
        /// A modern workspace (the 'new' decoupled Workspace experience)
        /// </summary>
        Workspace = 1,

        /// <summary>
        /// A legacy workspace based on Office 365 groups (the 'old' Workspace experience where Workspaces are tightly coupled with O365 groups)
        /// </summary>
        Group = 2,

        /// <summary>
        /// Personal workspace of a Power BI user, a.k.a. "My Workspace"
        /// </summary>
        PersonalGroup = 3
    }
}
