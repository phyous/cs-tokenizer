using System.Text.RegularExpressions;

namespace CsTokenizer.Interfaces
{
    public interface IRegexPatternGenerator
    {
        /// <summary>
        /// Gets an existing or creates a new compiled regex pattern for the given token
        /// </summary>
        /// <param name="token">The token to create a pattern for</param>
        /// <returns>A compiled regex pattern</returns>
        Regex GetOrCreatePattern(string token);

        /// <summary>
        /// Clears the pattern cache
        /// </summary>
        void ClearCache();
    }
} 