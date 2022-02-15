namespace Sqlbi.Bravo.Infrastructure.Contracts.PBICloud
{
    public enum CloudWorkspaceType
    {
        Unknown = 0,

        /// <summary>
        /// PersonalWorkspaceType
        /// </summary>
        User,

        /// <summary>
        /// GroupWorkspaceType
        /// </summary>
        Group,

        /// <summary>
        /// FolderWorkspaceType
        /// </summary>
        Folder,
    }
}
