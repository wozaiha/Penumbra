using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using Dalamud.Logging;
using Penumbra.GameData.Files;
using Penumbra.Mod;

namespace Penumbra.Util;

public static class ModelChanger
{
    public const string MaterialFormat = "/mt_c0201b0001_{0}.mtrl";

    public static bool ValidStrings( string from, string to )
        => from.Length                         != 0
         && to.Length                          != 0
         && from.Length                        < 16
         && to.Length                          < 16
         && from                               != to
         && Encoding.UTF8.GetByteCount( from ) == from.Length
         && Encoding.UTF8.GetByteCount( to )   == to.Length;


    [Conditional( "Debug" )]
    private static void WriteBackup( string name, byte[] text )
        => File.WriteAllBytes( name + ".bak", text );

    public static int ChangeMtrl( FullPath file, string from, string to )
    {
        if( !file.Exists )
        {
            return 0;
        }

        try
        {
            var data    = File.ReadAllBytes( file.FullName );
            var mdlFile = new MdlFile( data );
            from = string.Format( MaterialFormat, from );
            to   = string.Format( MaterialFormat, to );

            var replaced = 0;
            for( var i = 0; i < mdlFile.Materials.Length; ++i )
            {
                if( mdlFile.Materials[ i ] == @from )
                {
                    mdlFile.Materials[ i ] = to;
                    ++replaced;
                }
            }

            if( replaced > 0 )
            {
                WriteBackup( file.FullName, data );
                File.WriteAllBytes( file.FullName, mdlFile.Write() );
            }

            return replaced;
        }
        catch( Exception e )
        {
            PluginLog.Error( $"Could not write .mdl data for file {file.FullName}:\n{e}" );
            return -1;
        }
    }

    public static bool ChangeModMaterials( ModData mod, string from, string to )
    {
        if( ValidStrings( from, to ) )
        {
            return mod.Resources.ModFiles
               .Where( f => f.Extension.Equals( ".mdl", StringComparison.InvariantCultureIgnoreCase ) )
               .All( file => ChangeMtrl( file, from, to ) >= 0 );
        }

        PluginLog.Warning( $"{from} or {to} can not be valid material suffixes." );
        return false;
    }
}