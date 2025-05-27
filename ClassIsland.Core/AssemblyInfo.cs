using System.Runtime.CompilerServices;
using System.Windows;
using Avalonia.Metadata;

[assembly: ThemeInfo(
    ResourceDictionaryLocation.None,            //where theme specific resource dictionaries are located
                                                //(used if a resource is not found in the page,
                                                // or application resource dictionaries)
    ResourceDictionaryLocation.SourceAssembly   //where the generic resource dictionary is located
                                                //(used if a resource is not found in the page,
                                                // app, or any theme specific resource dictionaries)
)]
[assembly: InternalsVisibleTo("ClassIsland")]

[assembly: XmlnsPrefix("http://classisland.tech/schemas/xaml/core", "ci")]
[assembly: XmlnsDefinition("http://classisland.tech/schemas/xaml/core", "ClassIsland.Core")]
[assembly: XmlnsDefinition("http://classisland.tech/schemas/xaml/core", "ClassIsland.Core.Converters")]
[assembly: XmlnsDefinition("http://classisland.tech/schemas/xaml/core", "ClassIsland.Core.Controls")]
[assembly: XmlnsDefinition("http://classisland.tech/schemas/xaml/core", "ClassIsland.Core.Controls.CommonDialog")]
[assembly: XmlnsDefinition("http://classisland.tech/schemas/xaml/core", "ClassIsland.Core.Controls.LessonsControls")]
[assembly: XmlnsDefinition("http://classisland.tech/schemas/xaml/core", "ClassIsland.Core.Controls.IconControl")]
[assembly: XmlnsDefinition("http://classisland.tech/schemas/xaml/core", "ClassIsland.Core.Controls.NavHyperlink")]
[assembly: XmlnsDefinition("http://classisland.tech/schemas/xaml/core", "ClassIsland.Core.Controls.Ruleset")]
[assembly: XmlnsDefinition("http://classisland.tech/schemas/xaml/core", "ClassIsland.Core.Commands")]
[assembly: XmlnsDefinition("http://classisland.tech/schemas/xaml/core", "ClassIsland.Core.Abstractions.Controls")]
[assembly: XmlnsDefinition("http://classisland.tech/schemas/xaml/core", "ClassIsland.Core.Controls.StickerControl")]
[assembly: XmlnsDefinition("http://classisland.tech/schemas/xaml/core", "ClassIsland.Core.Abstractions.Views")]