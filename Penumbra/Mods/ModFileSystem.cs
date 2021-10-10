using System;
using System.Linq;
using Penumbra.Mod;
using Penumbra.Util;

namespace Penumbra.Mods
{
    public delegate void OnModFileSystemChange();

    public static partial class ModFileSystem
    {
        // The root folder that should be used as the base for all structured mods.
        public static ModFolder Root = ModFolder.CreateRoot();

        // Gets invoked every time the file system changes.
        public static event OnModFileSystemChange? ModFileSystemChanged;

        internal static void InvokeChange()
            => ModFileSystemChanged?.Invoke();

        // Find a specific mod folder by its path from Root.
        // Returns true if the folder was found, and false if not.
        // The out parameter will contain the furthest existing folder.
        public static bool Find( string path, out ModFolder folder )
        {
            var split = path.Split( new[] { '/' }, StringSplitOptions.RemoveEmptyEntries );
            folder = Root;
            foreach( var part in split )
            {
                if( !folder.FindSubFolder( part, out folder ) )
                {
                    return false;
                }
            }

            return true;
        }

        // Rename the SortOrderName of a single mod. Slashes are replaced by Backslashes.
        // Saves and returns true if anything changed.
        public static bool Rename( this ModData mod, string newName )
        {
            if( RenameNoSave( mod, newName ) )
            {
                SaveMod( mod );
                return true;
            }

            return false;
        }

        // Rename the target folder, merging it and its subfolders if the new name already exists.
        // Saves all mods manipulated thus, and returns true if anything changed.
        public static bool Rename( this ModFolder target, string newName )
        {
            if( RenameNoSave( target, newName ) )
            {
                SaveModChildren( target );
                return true;
            }

            return false;
        }

        // Move a single mod to the target folder.
        // Returns true and saves if anything changed.
        public static bool Move( this ModData mod, ModFolder target )
        {
            if( MoveNoSave( mod, target ) )
            {
                SaveMod( mod );
                return true;
            }

            return false;
        }

        // Move a mod to the filesystem location specified by sortOrder and rename its SortOrderName.
        // Creates all necessary Subfolders.
        public static void Move( this ModData mod, string sortOrder )
        {
            var split  = sortOrder.Split( new[] { '/' }, StringSplitOptions.RemoveEmptyEntries );
            var folder = Root;
            for( var i = 0; i < split.Length - 1; ++i )
            {
                folder = folder.FindOrCreateSubFolder( split[ i ] ).Item1;
            }

            if( MoveNoSave( mod, folder ) | RenameNoSave( mod, split.Last() ) )
            {
                SaveMod( mod );
            }
        }

        // Moves folder to target.
        // If an identically named subfolder of target already exists, merges instead.
        // Root is not movable.
        public static bool Move( this ModFolder folder, ModFolder target )
        {
            if( MoveNoSave( folder, target ) )
            {
                SaveModChildren( target );
                return true;
            }

            return false;
        }

        // Merge source with target, moving all direct mod children of source to target,
        // and moving all subfolders of source to target, or merging them with targets subfolders if they exist.
        // Returns true and saves if anything changed.
        public static bool Merge( this ModFolder source, ModFolder target )
        {
            if( MergeNoSave( source, target ) )
            {
                SaveModChildren( target );
                return true;
            }

            return false;
        }
    }

    // Internal stuff.
    public static partial class ModFileSystem
    {
        // Reset all sort orders for all descendants of the given folder.
        // Assumes that it is not called on Root, and thus does not remove unnecessary SortOrder entries.
        private static void SaveModChildren( ModFolder target )
        {
            foreach( var mod in target.AllMods( true ) )
            {
                Penumbra.Config.ModSortOrder[ mod.BasePath.Name ] = mod.SortOrder.FullName;
            }

            Penumbra.Config.Save();
            InvokeChange();
        }

        // Sets and saves the sort order of a single mod, removing the entry if it is unnecessary.
        private static void SaveMod( ModData mod )
        {
            if( ReferenceEquals( mod.SortOrder.ParentFolder, Root )
             && string.Equals( mod.SortOrder.SortOrderName, mod.Meta.Name.Replace( '/', '\\' ), StringComparison.InvariantCultureIgnoreCase ) )
            {
                Penumbra.Config.ModSortOrder.Remove( mod.BasePath.Name );
            }
            else
            {
                Penumbra.Config.ModSortOrder[ mod.BasePath.Name ] = mod.SortOrder.FullName;
            }

            Penumbra.Config.Save();
            InvokeChange();
        }

        private static bool RenameNoSave( this ModFolder target, string newName )
        {
            if( ReferenceEquals( target, Root ) )
            {
                throw new InvalidOperationException( "Can not rename root." );
            }

            newName = newName.Replace( '/', '\\' );
            if( target.Name == newName )
            {
                return false;
            }

            if( target.Parent!.FindSubFolder( newName, out var preExisting ) )
            {
                MergeNoSave( target, preExisting );
            }
            else
            {
                var parent = target.Parent;
                parent.RemoveFolderIgnoreEmpty( target );
                target.Name = newName;
                parent.FindOrAddSubFolder( target );
            }

            return true;
        }

        private static bool RenameNoSave( ModData mod, string newName )
        {
            newName = newName.Replace( '/', '\\' );
            if( mod.SortOrder.SortOrderName == newName )
            {
                return false;
            }

            mod.SortOrder.ParentFolder.RemoveModIgnoreEmpty( mod );
            mod.SortOrder = new SortOrder( mod.SortOrder.ParentFolder, newName );
            mod.SortOrder.ParentFolder.AddMod( mod );
            return true;
        }

        private static bool MoveNoSave( ModData mod, ModFolder target )
        {
            var oldParent = mod.SortOrder.ParentFolder;
            if( ReferenceEquals( target, oldParent ) )
            {
                return false;
            }

            oldParent.RemoveMod( mod );
            mod.SortOrder = new SortOrder( target, mod.SortOrder.SortOrderName );
            target.AddMod( mod );
            return true;
        }

        private static bool MergeNoSave( ModFolder source, ModFolder target )
        {
            if( ReferenceEquals( source, target ) )
            {
                return false;
            }

            var any = false;
            while( source.SubFolders.Count > 0 )
            {
                any |= MoveNoSave( source.SubFolders.First(), target );
            }

            while( source.Mods.Count > 0 )
            {
                any |= MoveNoSave( source.Mods.First(), target );
            }

            source.Parent?.RemoveSubFolder( source );

            return any || source.Parent != null;
        }

        private static bool MoveNoSave( ModFolder folder, ModFolder target )
        {
            // Moving a folder into itself is not permitted.
            if( ReferenceEquals( folder, target ) )
            {
                return false;
            }

            if( ReferenceEquals( target, folder.Parent! ) )
            {
                return false;
            }

            folder.Parent!.RemoveSubFolder( folder );
            var subFolderIdx = target.FindOrAddSubFolder( folder );
            if( subFolderIdx > 0 )
            {
                var main = target.SubFolders[ subFolderIdx ];
                MergeNoSave( folder, main );
            }

            return true;
        }
    }
}