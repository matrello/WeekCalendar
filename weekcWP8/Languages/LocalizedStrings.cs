using weekc.Resources;

namespace weekc.Languages
{
    /// <summary>
    /// Provides access to string resources.
    /// </summary>
    public class LocalizedStrings
    {
        private static AppResources _localizedResources = new AppResources();
        public AppResources LocalizedResources { get { return _localizedResources; } }

        private static readonly Strings _strings = new Strings();
        public Strings Strings { get { return _strings; } } 
    }
}