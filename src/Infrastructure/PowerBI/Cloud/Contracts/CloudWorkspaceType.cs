namespace Sqlbi.Bravo.Infrastructure.PowerBI.Cloud.Contracts
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
