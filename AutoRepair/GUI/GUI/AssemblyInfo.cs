using System.Windows; // need this to access ResourceDictionaryLocation and ThemeInfo

// this attribute tells WPF where to look for theme resource dictionaries
// basically its about how WPF resolves styles and templates at runtime
[assembly:ThemeInfo(
    ResourceDictionaryLocation.None,            // dont look for theme-specific dictionaries anywhere special
                                                // (if a resource isnt in the page or app dictionaries
                                                // it just wont be found from a theme dictionary)
    ResourceDictionaryLocation.SourceAssembly   // for the generic/fallback dictionary, look in this assembly
                                                // (so generic.xaml in the Themes folder would be used
                                                // if we had one, but we dont really need it here)
)]
