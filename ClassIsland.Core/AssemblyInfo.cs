using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Markup;

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
[assembly: XmlnsDefinition("http://classisland.tech/schemas/xaml/core", "ClassIsland.Core", AssemblyName = "ClassIsland.Core")]
[assembly: XmlnsDefinition("http://classisland.tech/schemas/xaml/core", "ClassIsland.Core.Converters", AssemblyName = "ClassIsland.Core")]
[assembly: XmlnsDefinition("http://classisland.tech/schemas/xaml/core", "ClassIsland.Core.Controls", AssemblyName = "ClassIsland.Core")]
[assembly: XmlnsDefinition("http://classisland.tech/schemas/xaml/core", "ClassIsland.Core.Controls.CommonDialog", AssemblyName = "ClassIsland.Core")]
[assembly: XmlnsDefinition("http://classisland.tech/schemas/xaml/core", "ClassIsland.Core.Controls.LessonsControls", AssemblyName = "ClassIsland.Core")]
[assembly: XmlnsDefinition("http://classisland.tech/schemas/xaml/core", "ClassIsland.Core.Controls.IconControl", AssemblyName = "ClassIsland.Core")]
[assembly: XmlnsDefinition("http://classisland.tech/schemas/xaml/core", "ClassIsland.Core.Controls.NavHyperlink", AssemblyName = "ClassIsland.Core")]
[assembly: XmlnsDefinition("http://classisland.tech/schemas/xaml/core", "ClassIsland.Core.Commands", AssemblyName = "ClassIsland.Core")]
[assembly: XmlnsDefinition("http://classisland.tech/schemas/xaml/core", "ClassIsland.Core.Abstractions.Controls", AssemblyName = "ClassIsland.Core")]
