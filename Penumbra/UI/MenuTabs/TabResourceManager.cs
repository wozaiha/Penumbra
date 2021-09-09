using System.Numerics;
using Dalamud.Interface;
using FFXIVClientStructs.FFXIV.Client.System.Resource;
using FFXIVClientStructs.FFXIV.Client.System.Resource.Handle;
using FFXIVClientStructs.STD;
using ImGuiNET;
using Penumbra.UI.Custom;

namespace Penumbra.UI
{
    public partial class SettingsInterface
    {
        private static string GetNodeLabel( string label, uint type, ulong count )
        {
            var byte1 = type >> 24;
            var byte2 = ( type >> 16 ) & 0xFF;
            var byte3 = ( type >> 8 )  & 0xFF;
            var byte4 = type           & 0xFF;
            return byte1 == 0
                ? $"{( char )byte2}{( char )byte3}{( char )byte4} - {count}###{label}{type}Debug"
                : $"{( char )byte1}{( char )byte2}{( char )byte3}{( char )byte4} - {count}###{label}{type}Debug";
        }

        private unsafe void DrawResourceMap( string label, StdMap< uint, Pointer< ResourceHandle > >* typeMap )
        {
            if( typeMap == null || !ImGui.TreeNodeEx( label ) )
            {
                return;
            }

            using var raii = ImGuiRaii.DeferredEnd( ImGui.TreePop );

            if( typeMap->Count == 0 || !ImGui.BeginTable( $"##{label}_table", 4, ImGuiTableFlags.SizingFixedFit | ImGuiTableFlags.RowBg ) )
            {
                return;
            }

            raii.Push( ImGui.EndTable );

            ImGui.TableSetupColumn( "Hash", ImGuiTableColumnFlags.WidthFixed, 100 * ImGuiHelpers.GlobalScale );
            ImGui.TableSetupColumn( "Ptr", ImGuiTableColumnFlags.WidthFixed, 100  * ImGuiHelpers.GlobalScale );
            ImGui.TableSetupColumn( "Path", ImGuiTableColumnFlags.WidthFixed, ImGui.GetWindowContentRegionWidth() - 300 * ImGuiHelpers.GlobalScale );
            ImGui.TableSetupColumn( "Refs", ImGuiTableColumnFlags.WidthFixed, 30 * ImGuiHelpers.GlobalScale );
            ImGui.TableHeadersRow();

            var node = typeMap->SmallestValue;
            while( !node->IsNil )
            {
                ImGui.TableNextRow();
                ImGui.TableNextColumn();
                ImGui.Text( node->KeyValuePair.Item1.ToString() );
                ImGui.TableNextColumn();
                var address = $"0x{( ulong )node->KeyValuePair.Item2.Value:X}";
                ImGui.Text( address );
                if( ImGui.IsItemClicked() )
                {
                    ImGui.SetClipboardText( address );
                }

                ImGui.TableNextColumn();
                ImGui.Text( node->KeyValuePair.Item2.Value->FileName.ToString() );
                ImGui.TableNextColumn();
                ImGui.Text( node->KeyValuePair.Item2.Value->RefCount.ToString() );
                node = node->Next();
            }
        }

        private unsafe void DrawCategoryContainer( string label, ResourceGraph.CategoryContainer container )
        {
            var map = container.MainMap;
            if( map == null || !ImGui.TreeNodeEx( $"{label} - {map->Count}###{label}Debug" ) )
            {
                return;
            }

            using var raii = ImGuiRaii.DeferredEnd( ImGui.TreePop );

            var node = map->SmallestValue;
            while( !node->IsNil )
            {
                DrawResourceMap( GetNodeLabel( label, node->KeyValuePair.Item1, node->KeyValuePair.Item2.Value->Count ),
                    node->KeyValuePair.Item2.Value );
                node = node->Next();
            }
        }

        private unsafe void DrawResourceManagerTab()
        {
            if( !ImGui.BeginTabItem( "Resource Manager Tab" ) )
            {
                return;
            }

            using var raii = ImGuiRaii.DeferredEnd( ImGui.EndTabItem );

            var resourceHandler = *( ResourceManager** )( Dalamud.SigScanner.Module.BaseAddress + 0x1D93AC0 );

            if( resourceHandler == null )
            {
                return;
            }

            raii.Push( ImGui.EndChild );
            if( !ImGui.BeginChild( "##ResourceManagerChild", -Vector2.One, true ) )
            {
                return;
            }

            DrawCategoryContainer( "Common", resourceHandler->ResourceGraph->CommonContainer );
            DrawCategoryContainer( "BgCommon", resourceHandler->ResourceGraph->BgCommonContainer );
            DrawCategoryContainer( "Bg", resourceHandler->ResourceGraph->BgContainer );
            DrawCategoryContainer( "Cut", resourceHandler->ResourceGraph->CutContainer );
            DrawCategoryContainer( "Chara", resourceHandler->ResourceGraph->CharaContainer );
            DrawCategoryContainer( "Shader", resourceHandler->ResourceGraph->ShaderContainer );
            DrawCategoryContainer( "Ui", resourceHandler->ResourceGraph->UiContainer );
            DrawCategoryContainer( "Sound", resourceHandler->ResourceGraph->SoundContainer );
            DrawCategoryContainer( "Vfx", resourceHandler->ResourceGraph->VfxContainer );
            DrawCategoryContainer( "UiScript", resourceHandler->ResourceGraph->UiScriptContainer );
            DrawCategoryContainer( "Exd", resourceHandler->ResourceGraph->ExdContainer );
            DrawCategoryContainer( "GameScript", resourceHandler->ResourceGraph->GameScriptContainer );
            DrawCategoryContainer( "Music", resourceHandler->ResourceGraph->MusicContainer );
            DrawCategoryContainer( "SqpackTest", resourceHandler->ResourceGraph->SqpackTestContainer );
            DrawCategoryContainer( "Debug", resourceHandler->ResourceGraph->DebugContainer );
        }
    }
}